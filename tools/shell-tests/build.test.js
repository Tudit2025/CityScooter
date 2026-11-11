// End-to-end test of the REAL Unity Web build with the new shell: USB (fake Web Serial) and Simulator (real simulator).
//   node realtest.js <buildDir | https://url> <labcheck.py path> [outDir]
const http = require("http");
const fs = require("fs");
const path = require("path");
const puppeteer = require("puppeteer-core");

const TARGET = process.argv[2];
const REMOTE = /^https?:/.test(TARGET);
const ROOT = REMOTE ? "" : path.resolve(TARGET);
const LABCHECK = fs.readFileSync(process.argv[3], "utf8");
const OUT = path.resolve(process.argv[4] || __dirname);
const CHROME = "C:/Program Files/Google/Chrome/Application/chrome.exe";
const server = http.createServer((req, res) => {
  const rel = decodeURIComponent(req.url.split("?")[0]);
  const file = path.join(ROOT, rel === "/" ? "index.html" : rel);
  if (!file.startsWith(ROOT) || !fs.existsSync(file)) { res.writeHead(404); return res.end(); }
  const h = { "Content-Type": { ".html": "text/html", ".js": "application/javascript", ".css": "text/css", ".py": "text/plain" }[path.extname(file)] || "application/octet-stream" };
  if (file.endsWith(".unityweb")) {
    h["Content-Encoding"] = "br";
    h["Content-Type"] = file.endsWith(".wasm.unityweb") ? "application/wasm" : file.endsWith(".js.unityweb") ? "application/javascript" : "application/octet-stream";
  }
  res.writeHead(200, h);
  fs.createReadStream(file).pipe(res);
});
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let failures = 0;
const check = (name, ok, extra) => { console.log((ok ? "PASS " : "FAIL ") + name + (extra !== undefined ? "  " + extra : "")); if (!ok) failures++; };

function installFakeSerial() {
  const enc = new TextEncoder(), dec = new TextDecoder();
  const listeners = { connect: [], disconnect: [] };
  const ports = [];
  class FakePort {
    constructor() { this.written = ""; }
    async open() {
      this.readable = new ReadableStream({ start: (c) => { this.ctl = c; } });
      this.writable = new WritableStream({ write: (chunk) => { this.written += dec.decode(chunk); } });
    }
    push(t) { this.ctl.enqueue(enc.encode(t)); }
    async close() { this.readable = null; this.writable = null; }
  }
  const serial = {
    async requestPort() { const p = new FakePort(); ports.push(p); return p; },
    async getPorts() { return []; },
    addEventListener(type, fn) { (listeners[type] || []).push(fn); },
  };
  Object.defineProperty(Navigator.prototype, "serial", { get: () => serial, configurable: true });
  window.__serial = { ports };
  // Record what Unity sends to devices and what the shell sends to Unity.
  window.__sent = []; window.__toUnity = [];
  let shell;
  Object.defineProperty(window, "CityScooterShell", {
    configurable: true, get: () => shell,
    set: (v) => {
      shell = v;
      const boot = v.boot, send = v.sendToDevices;
      v.boot = function (canvas, config, opts) {
        const orig = window.createUnityInstance;
        window.createUnityInstance = (c, cfg, p) => orig(c, cfg, p).then((inst) => {
          const sm = inst.SendMessage.bind(inst);
          inst.SendMessage = (o, m, a) => { window.__toUnity.push(m + "(" + a + ")"); return sm(o, m, a); };
          window.__unity = inst; return inst;
        });
        return boot.call(this, canvas, config, opts);
      };
      v.sendToDevices = function (t, l) { window.__sent.push(t + ": " + l); return send.call(this, t, l); };
    },
  });
}

(async () => {
  if (!REMOTE) await new Promise((r) => server.listen(8792, "127.0.0.1", r));
  const browser = await puppeteer.launch({ executablePath: CHROME, headless: true,
    args: ["--use-angle=d3d11", "--enable-gpu", "--ignore-gpu-blocklist", "--autoplay-policy=no-user-gesture-required"] });
  const page = await browser.newPage();
  await page.setViewport({ width: 1366, height: 768 });
  const errors = [];
  page.on("pageerror", (e) => errors.push("pageerror: " + e.message));
  page.on("console", (m) => { if (m.type() === "error" && !/favicon/.test(m.text())) errors.push("console.error: " + m.text()); });
  page.on("response", (r) => { if (r.status() >= 400) errors.push("HTTP " + r.status() + " " + r.url()); });
  await page.evaluateOnNewDocument(installFakeSerial);
  await page.evaluateOnNewDocument(() => { try { localStorage.clear(); } catch (e) {} });
  const t0 = Date.now();
  await page.goto(REMOTE ? TARGET : "http://127.0.0.1:8792/", { waitUntil: "load", timeout: 60000 });
  const ready = await page.waitForFunction(() => window.__unity && window.__toUnity.some((c) => c.startsWith("SetKeyboardEnabled")), { timeout: 120000 }).then(() => true, () => false);
  check("Unity loads and the shell talks to DeviceBridge", ready, (Date.now() - t0) + " ms");
  const ev = (fn, ...a) => page.evaluate(fn, ...a);
  await sleep(1500);

  // Keyboard mode: page text boxes keep their keys (WebGLInput.captureAllKeyboardInput = false)
  await ev(() => { const t = document.createElement("textarea"); t.id = "kbtest"; t.style.cssText = "position:fixed;left:0;top:0;z-index:99"; document.body.appendChild(t); t.focus(); });
  await page.keyboard.type("print(1) a b");
  check("text box gets keys while the game runs", (await ev(() => document.getElementById("kbtest").value)) === "print(1) a b");
  await ev(() => document.getElementById("kbtest").remove());

  // USB mode with a fake board: game starts on jp and sends go/spd back
  await page.click('.cs-modes button[data-mode="usb"]');
  await page.click("#cs-connect");
  await sleep(300);
  await ev(() => window.__serial.ports[0].push("hi:v2\n"));
  for (let i = 0; i < 10; i++) { await ev(() => window.__serial.ports[0].push("st:0\n")); await sleep(50); }
  // Lesson page (index.html boots with keyboard: false): no Keyboard mode, and Space on the game does not start a ride.
  check("micro:bit only: no Keyboard mode button", (await ev(() => document.querySelectorAll('.cs-modes button[data-mode="keyboard"]').length)) === 0);
  check("micro:bit only: Unity keyboard off", (await ev(() => window.__toUnity.filter((c) => c.startsWith("SetKeyboardEnabled")).every((c) => c.includes("(0)")))));
  await ev(() => document.getElementById("unity-canvas").focus());
  await page.keyboard.press("Space"); await sleep(1200);
  check("micro:bit only: Space does not start a ride", !/(^|\n)go\n/.test(await ev(() => window.__serial.ports[0].written)));
  // The board picks where rides start (block "start rides in [Kings Park]"): the start screen moves there.
  await ev(() => window.__serial.ports[0].push("start:kingspark\n")); await sleep(800);
  await ev(() => window.__serial.ports[0].push("jp\n"));
  for (let i = 0; i < 40; i++) { await ev((v) => window.__serial.ports[0].push("st:" + v + "\n"), i < 20 ? 40 : -40); await sleep(50); }
  const written = await ev(() => window.__serial.ports[0].written);
  check("USB: jp starts a ride, game sends go to the board", /(^|\n)go\n/.test(written), JSON.stringify(written.slice(0, 60)));
  check("USB: start:kingspark from the board, the ride starts in Kings Park", /(^|\n)go\nzone:kingspark\n/.test(written), JSON.stringify(written.slice(0, 60)));
  check("USB: game sends speed to the board", /spd:\d\n/.test(written));
  check("USB: Unity got the board's lines", (await ev(() => window.__toUnity.filter((c) => c.startsWith("OnDeviceLines") && c.includes("usb1|st:")).length)) > 10);
  await page.screenshot({ path: path.join(OUT, "real_usb.png") });

  // Simulator mode with labcheck: ▶, then A starts a ride; the game's go reaches the simulator (echo:go)
  await page.click('.cs-modes button[data-mode="sim"]');
  // The latest keyboard state Unity got is off (the lesson page never turns it on, so switching modes sends nothing new).
  check("sim: Unity keyboard off", await ev(() => { const k = window.__toUnity.filter((c) => c.startsWith("SetKeyboardEnabled")); return k.length > 0 && k[k.length - 1].includes("(0)"); }));
  await page.waitForFunction(() => window.MicrobitBlocks, { timeout: 20000 });
  await ev((code) => { document.querySelector('.cs-tabs button[data-view="python"]').click(); const t = document.getElementById("cs-code"); t.value = code; t.dispatchEvent(new Event("input")); }, LABCHECK);
  await page.waitForFunction(() => document.getElementById("cs-sim").src.includes("usermbit"), { timeout: 5000 });
  await sleep(3000);
  const simFrame = page.frames().find((f) => f.url().includes("python-simulator.usermbit.org"));
  await simFrame.waitForSelector(".play-button", { visible: true, timeout: 20000 });
  // The external simulator sometimes shows its play button a moment before it can take a click: try a few times.
  for (let tries = 0; ; tries++) {
    try { await simFrame.click(".play-button"); break; } catch (e) { if (tries >= 5) throw e; await sleep(1000); }
  }
  const running = await page.waitForFunction(() => window.__toUnity.some((c) => c.includes("sim|st:")), { timeout: 20000 }).then(() => true, () => false);
  check("sim: code runs, Unity gets sim lines", running);
  // wait for the ride from the USB test to end or not: crash is fine; press A to (re)start
  await page.click(".cs-monitor-head strong");
  const sentBefore = await ev(() => window.__sent.length);
  await page.keyboard.down("ArrowRight"); await sleep(600); await page.keyboard.up("ArrowRight");
  // Whatever state the ride is in (running, crashed, ready), pressing A keeps the game talking to the board.
  let gotMsg = false, echoed = false;
  for (const end = Date.now() + 30000; Date.now() < end && !(gotMsg && echoed);) {
    await page.keyboard.press("a");
    await sleep(1200);
    gotMsg = await ev((n) => window.__sent.slice(n).some((s) => /^\*: (go|spd:\d|crash|gift)$/.test(s)), sentBefore);
    echoed = await ev(() => /echo:(go|spd:\d|crash|gift)/.test(document.getElementById("cs-log").textContent));
  }
  const sent = await ev((n) => window.__sent.slice(n), sentBefore);
  const logTxt = await ev(() => document.getElementById("cs-log").textContent);
  check("sim: game messages go to the simulator", gotMsg, JSON.stringify(sent.slice(0, 6)));
  check("sim: simulator echoes them back (seen in the monitor)", echoed, logTxt.slice(-120));
  // After a crash, A starts a new ride: go must reach the simulator too.
  const nBefore = await ev(() => window.__sent.length);
  await page.waitForFunction((n) => window.__sent.slice(n).includes("*: crash") || window.__sent.slice(-1)[0] === "*: crash", { timeout: 30000 }, nBefore - 1).catch(() => {});
  await sleep(1500);
  await page.keyboard.press("a");
  const goEcho = await page.waitForFunction(() => /echo:go/.test(document.getElementById("cs-log").textContent), { timeout: 8000 }).then(() => true, () => false);
  check("sim: A restarts the ride and go is echoed", goEcho, JSON.stringify((await ev(() => window.__sent.slice(-4)))));
  check("sim: tilt keys steer the game", (await ev(() => window.__toUnity.filter((c) => /sim\|st:(9\d|100)/.test(c)).length)) > 0);
  await page.screenshot({ path: path.join(OUT, "real_sim.png") });

  // Every ride (11/10): a program whose "on start" set 5 wanted stars says open:stars:5 once a second. Every ride must
  // start with 5 stars, also the rides started with A after the police ended the one before (what the teacher saw
  // missing in class). With 5 stars a ride is over within seconds, so three rides fit in the test.
  {
    const EVERY_RIDE = ["from microbit import *", "n = 0", "while True:", "    if button_a.was_pressed():", '        print("jp")', "    n += 1", "    if uart.any():", "        uart.read()  # like every real program: read what the game sends",
      "    if n % 20 == 0:", '        print("hi:v2")', '        print("open:stars:5")', '    print("st:0")', "    sleep(50)", ""].join("\n");
    await ev((code) => { const t = document.getElementById("cs-code"); t.value = code; t.dispatchEvent(new Event("input")); }, EVERY_RIDE);
    await page.click("#cs-run");
    await page.waitForFunction(() => window.__toUnity.some((c) => c.includes("sim|open:stars:5")), { timeout: 15000 }).catch(() => {});
    await page.click(".cs-monitor-head strong");
    const from = await ev(() => window.__sent.length);
    let restarts = 0;
    const rides = () => ev((n) => {
      // one entry per ride: did wanted:5 come after its go and before the next go?
      const out = [];
      for (const s of window.__sent.slice(n)) {
        if (s === "*: go") out.push(false);
        else if (s === "*: wanted:5" && out.length) out[out.length - 1] = true;
      }
      return out;
    }, from);
    for (const end = Date.now() + 150000; Date.now() < end && (await rides()).filter(Boolean).length < 3;) {
      // The external simulator can hang when a program is replaced; the shell notices, gives it a new iframe and asks
      // for its play button again (bridge.md). Do what the student would: click it.
      if (await ev(() => { const h = document.getElementById("cs-sim-hint"); return !!h && !h.hidden && /Click/.test(h.textContent); })) {
        const f = page.frames().find((fr) => fr.url().includes("python-simulator.usermbit.org"));
        try { await f.waitForSelector(".play-button", { visible: true, timeout: 15000 }); await f.click(".play-button"); restarts++; await sleep(2500); } catch (e) { await sleep(1000); }
        await page.click(".cs-monitor-head strong");
        continue;
      }
      await page.keyboard.press("a"); // starts a ride from the start screen or after the ride ended; a jump during a ride
      await sleep(900);
    }
    const got = await rides();
    check("every ride: open:stars:5 from the board gives 5 stars at the start of 3 rides in a row", got.length >= 3 && got.slice(0, 3).every(Boolean), JSON.stringify(got) + (restarts ? " (simulator restarted " + restarts + "x)" : "")
      + " sent: " + JSON.stringify(await ev((n) => window.__sent.slice(n).slice(-14), from))
      + " toUnity: " + JSON.stringify(await ev(() => window.__toUnity.slice(-6).map((c) => c.slice(0, 90))))
      + " monitor: " + JSON.stringify(await ev(() => [...document.querySelectorAll("#cs-log .cs-line")].slice(-12).map((r) => r.className.replace("cs-line ", "") + " " + r.textContent.slice(0, 110)))));
    // back to a program without it: the stars stop
    await ev((code) => { const t = document.getElementById("cs-code"); t.value = code; t.dispatchEvent(new Event("input")); }, LABCHECK);
    await page.click("#cs-run");
    await sleep(4500);
  }

  // Radio race (v0.4) on the real build, through the USB board: race: -> lobby (pos:0:0 once a second), then a go:
  // relayed by the board as rx:go -> countdown -> the race starts in Kings Park and pos: comes 4 times a second.
  await page.click('.cs-modes button[data-mode="usb"]');
  await sleep(500);
  const port = 0;
  const pushRace = (t) => ev((i, t) => window.__serial.ports[i].push(t), port, t);
  const writtenAt = () => ev((i) => window.__serial.ports[i].written.length, port);
  const boardText = () => ev((i) => window.__serial.ports[i].written, port);
  const startLen = await writtenAt();
  for (let i = 0; i < 6; i++) { await pushRace("race:AMY:12\nrx:p:BEN:0:0\nst:0\n"); await sleep(500); }
  const lobby = (await boardText()).slice(startLen);
  check("race: the board's race: puts the game in the lobby (pos:0:0 to the board)", /(^|\n)pos:0:0\n/.test(lobby), JSON.stringify(lobby.slice(-80)));
  const goLen = await writtenAt();
  await pushRace("rx:go:4821:kingspark:300\n");
  for (let i = 0; i < 20; i++) { await pushRace("race:AMY:12\nrx:p:BEN:" + (i * 5) + ":0\nst:0\n"); await sleep(250); }
  const raced = (await boardText()).slice(goLen);
  check("race: rx:go starts the race in Kings Park after the countdown", /(^|\n)go\n[\s\S]*zone:kingspark\n/.test(raced), JSON.stringify(raced.slice(0, 120)));
  const posLines = raced.split("\n").filter((l) => /^pos:\d+:-?\d+$/.test(l) && l !== "pos:0:0");
  check("race: pos:DIST:LANE goes to the board during the race", posLines.length >= 4, posLines.slice(-4).join(" "));
  await page.screenshot({ path: path.join(OUT, "real_race.png") });

  check("no page errors", errors.length === 0, errors.slice(0, 6).join(" | "));
  console.log(failures ? failures + " FAILED" : "ALL PASSED");
  await browser.close();
  if (!REMOTE) server.close();
  process.exit(failures ? 1 : 0);
})().catch((e) => { console.error(e); process.exit(2); });
