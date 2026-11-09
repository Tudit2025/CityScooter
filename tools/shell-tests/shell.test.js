// Headless test of the CityScooter web shell on tools/shell-dev (fake Unity + fake Web Serial + the REAL simulator).
//   node shelltest.js <repoRoot> [outDir]
const http = require("http");
const fs = require("fs");
const path = require("path");
const puppeteer = require("puppeteer-core");

const ROOT = path.resolve(process.argv[2]);
const OUT = path.resolve(process.argv[3] || __dirname);
const CHROME = "C:/Program Files/Google/Chrome/Application/chrome.exe";
const TYPES = { ".html": "text/html", ".js": "application/javascript", ".css": "text/css", ".py": "text/plain" };
const server = http.createServer((req, res) => {
  let rel = decodeURIComponent(req.url.split("?")[0]);
  if (rel.endsWith("/")) rel += "index.html";
  const file = path.join(ROOT, rel);
  if (!file.startsWith(ROOT) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) { res.writeHead(404); return res.end(); }
  res.writeHead(200, { "Content-Type": TYPES[path.extname(file)] || "application/octet-stream" });
  fs.createReadStream(file).pipe(res);
});
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let failures = 0;
const check = (name, ok, extra) => { console.log((ok ? "PASS " : "FAIL ") + name + (extra !== undefined ? "  " + extra : "")); if (!ok) failures++; };

// Fake Web Serial installed before the page's scripts run.
function installFakeSerial() {
  const enc = new TextEncoder(), dec = new TextDecoder();
  const listeners = { connect: [], disconnect: [] };
  const ports = [];
  let nextRequest = null; // "busy" | "cancel" | null
  class FakePort {
    constructor(name) {
      this.name = name; this.written = ""; this.opened = false; this.busy = false;
      this.readable = null; this.writable = null;
    }
    async open(opts) {
      const fail = () => { const e = new Error("Failed to open serial port."); e.name = "NetworkError"; throw e; };
      if (this.busy) fail();
      this.opens = (this.opens || 0) + 1;
      if (this.failOpens > 0) { this.failOpens--; fail(); } // e.g. Windows still holds the port after a refresh / re-plug
      this.opts = opts;
      this.opened = true;
      this.readable = new ReadableStream({ start: (c) => { this.ctl = c; } });
      this.writable = new WritableStream({ write: (chunk) => { this.written += dec.decode(chunk); } });
    }
    async close() { this.opened = false; this.readable = null; this.writable = null; this.closes = (this.closes || 0) + 1; }
    push(text) { if (this.ctl) this.ctl.enqueue(enc.encode(text)); }
    unplug() { try { this.ctl.error(new Error("device lost")); } catch (e) {} this.readable = null; this.writable = null; this.opened = false; }
    // A non-fatal read error: Chrome errors the stream and gives the port a new readable.
    glitch() {
      const old = this.ctl;
      this.readable = new ReadableStream({ start: (c) => { this.ctl = c; } });
      const e = new Error("Buffer overrun"); e.name = "BufferOverrunError";
      try { old.error(e); } catch (x) {}
    }
    // A fatal stream error while the board stays plugged in (no disconnect event).
    die() { const old = this.ctl; this.readable = null; try { old.error(new Error("The device has been lost.")); } catch (x) {} }
  }
  const serial = {
    async requestPort() {
      const mode = nextRequest; nextRequest = null;
      if (mode === "cancel") { const e = new Error("No port selected by the user."); e.name = "NotFoundError"; throw e; }
      const p = new FakePort("p" + ports.length); if (mode === "busy") p.busy = true;
      ports.push(p); return p;
    },
    async getPorts() { return []; },
    addEventListener(type, fn) { (listeners[type] || []).push(fn); },
  };
  Object.defineProperty(Navigator.prototype, "serial", { get: () => serial, configurable: true });
  window.__serial = {
    ports, FakePort,
    next(mode) { nextRequest = mode; },
    fire(type, port) { for (const fn of listeners[type]) fn({ target: port }); },
  };
}

(async () => {
  await new Promise((r) => server.listen(8791, "127.0.0.1", r));
  const browser = await puppeteer.launch({ executablePath: CHROME, headless: true,
    args: ["--autoplay-policy=no-user-gesture-required", "--use-angle=swiftshader", "--enable-unsafe-swiftshader"] });
  const page = await browser.newPage();
  await page.setViewport({ width: 1366, height: 768 });
  const errors = [];
  page.on("pageerror", (e) => errors.push("pageerror: " + e.message));
  page.on("console", (m) => { if (m.type() === "error") errors.push("console.error: " + m.text()); });
  await page.evaluateOnNewDocument(installFakeSerial);
  await page.evaluateOnNewDocument(() => { try { localStorage.clear(); } catch (e) {} });

  const URL = "http://127.0.0.1:8791/tools/shell-dev/";
  await page.goto(URL, { waitUntil: "load" });
  await page.waitForFunction(() => window.__fakeUnity && window.__fakeUnity.calls.some((c) => c.includes("SetKeyboardEnabled")), { timeout: 5000 });
  const ev = (fn, ...a) => page.evaluate(fn, ...a);
  const calls = () => ev(() => window.__fakeUnity.calls.slice());
  const lines = () => ev(() => window.__fakeUnity.lines.slice());
  const logText = () => ev(() => [...document.querySelectorAll("#cs-log .cs-line")].map((r) => r.className + " :: " + r.textContent));

  check("default mode is keyboard", (await ev(() => document.body.dataset.mode)) === "keyboard");
  check("keyboard enabled sent to Unity after ready", (await calls()).some((c) => c.includes('SetKeyboardEnabled("1")')));
  await page.screenshot({ path: path.join(OUT, "shell_keyboard.png") });

  // ---------------- USB: cancel, busy, connect
  await page.click('.cs-modes button[data-mode="usb"]');
  await ev(() => window.__serial.next("cancel"));
  await page.click("#cs-connect");
  await sleep(100);
  check("cancel shows the empty-list / cable hint", /another cable/.test(await ev(() => document.getElementById("cs-usb-msg").textContent)));
  await ev(() => window.__serial.next("busy"));
  await page.click("#cs-connect");
  await sleep(100);
  check("busy port: tries again first (Connecting…)", /Connecting/.test(await ev(() => document.getElementById("cs-usb-msg").textContent)));
  const busyHint = await page.waitForFunction(() => /python\.microbit\.org/.test(document.getElementById("cs-usb-msg").textContent), { timeout: 9000, polling: 100 }).then(() => true, () => false);
  check("busy port shows the python.microbit.org hint after about 6 s of tries", busyHint);
  check("the open tries are in the USB log", (await ev(() => window.CityScooterShell.usbLog.filter((l) => /could not open the port \(NetworkError/.test(l)).length)) === 6,
    JSON.stringify(await ev(() => window.CityScooterShell.usbLog)));
  // The busy port (ports[0]) took the usb1 slot but never opened; the next port reuses that slot.
  await ev(() => window.__serial.next(null));
  await page.click("#cs-connect"); // ports[1] -> usb1
  await sleep(200);
  check("USB mode shown", (await ev(() => document.body.dataset.mode)) === "usb");
  const p = 1;
  const push = (i, t) => ev((i, t) => window.__serial.ports[i].push(t), i, t);
  await push(p, "hi:v2\nst:-3");
  await push(p, "5\njp\njp\n");
  await push(p, "jp\nbr\nhello from my code\n");
  await push(p, "Traceback (most recent call last):\n  File \"main.py\", line 4, in <module>\nNameError: name 'stear' isn't defined\n");
  await push(p, "st:abc\necho:crash\n");
  await push(p, "sur\n");
  await push(p, "trick\n"); // v0.3 ramps
  // The stats row redraws every 200 ms and the lamp stays on for 600 ms: wait for it rather than sampling once.
  const surLamp = await page.waitForFunction(() => { const r = document.querySelector(".cs-stat .cs-sur"); return !!r && r.classList.contains("cs-on"); },
    { timeout: 550, polling: 50 }).then(() => true, () => false);
  await sleep(300);
  const L = await lines();
  check("sur (surrender) reaches Unity", L.includes("usb1|sur"));
  check("sur lights the SUR lamp", surLamp);
  check("trick reaches Unity (v0.3 ramps)", L.includes("usb1|trick"));
  check("trick lights the TRICK lamp", await ev(() => !!document.querySelector(".cs-stat .cs-trick")));
  check("st split across chunks reaches Unity whole", L.includes("usb1|st:-35"), JSON.stringify(L.slice(0, 8)));
  check("jp lines reach Unity", L.filter((x) => x === "usb1|jp").length === 3);
  check("status usb1 v2 connected", (await ev(() => window.__fakeUnity.status.usb1)) === "usb1|usb|1|v2");
  const log = await logText();
  check("student print shown raw", log.some((x) => /cs-l-print/.test(x) && x.includes("hello from my code")));
  check("NameError red with spelling hint", log.some((x) => /cs-l-error/.test(x) && /NameError/.test(x) && /spelling/.test(x)));
  check("Traceback line red", log.some((x) => /cs-l-error/.test(x) && /Traceback/.test(x)));
  check("malformed st yellow with hint", log.some((x) => /cs-l-warn/.test(x) && x.includes("st:abc") && /needs a number/.test(x)));
  check("st/jp/br/hi not logged line by line", !log.some((x) => !/cs-l-warn/.test(x) && / :: usb1(st:|jp$|br$|hi:)/.test(x)));
  check("sur is a command, not a student print", !log.some((x) => / :: usb1sur$/.test(x)), log.filter((x) => /sur/.test(x)).join(" | "));
  const stat = await ev(() => { const r = document.querySelector(".cs-stat"); return r && { id: r.querySelector(".cs-sid").textContent, steer: r.querySelector(".cs-steer-val").textContent, jp: r.querySelector(".cs-jp-big").textContent }; });
  check("stats row: id, steer, jp in last second", stat && stat.id === "usb1 V2" && stat.steer === "-35" && stat.jp === "3", JSON.stringify(stat));

  // game -> board
  await page.evaluate(() => [...document.querySelectorAll("#dev-tools button")].find((b) => b.textContent === "crash").click());
  await sleep(100);
  check("crash written to the board with newline", (await ev((i) => window.__serial.ports[i].written, p)) === "crash\n");
  check("game message shown in game colour", (await logText()).some((x) => /cs-l-game/.test(x) && x.includes("crash")));
  // districts (v0.3): zone:<id> from the game names the district; a board printing zone: is told it is a game message
  await ev(() => window.CityScooterShell.sendToDevices("*", "zone:kingspark"));
  await push(p, "zone:uwa\n");
  await sleep(400);
  const zlog = await logText();
  check("zone:kingspark from the game shown with the district name", zlog.some((x) => /cs-l-game/.test(x) && x.includes("zone:kingspark") && x.includes("entering Kings Park")),
    zlog.filter((x) => /zone/.test(x)).join(" | "));
  check("zone: from a board is a student print with a hint", zlog.some((x) => /cs-l-print/.test(x) && x.includes("zone:uwa") && /FROM the game/.test(x)),
    zlog.filter((x) => /zone/.test(x)).join(" | "));
  // start:<id> picks where rides start; programs send it every second, so the monitor notes only a change
  await push(p, "start:Kings Park\nstart:kingspark\nstart:kingspark\nstart:perth\n");
  await sleep(400);
  const slog = await logText(), sl = await lines();
  check("start:Kings Park reaches Unity (spaces and capitals forgiven there too)", sl.includes("usb1|start:Kings Park") && sl.filter((x) => x === "usb1|start:kingspark").length === 2);
  check("start: noted once with the district name", slog.filter((x) => x.includes("rides start in Kings Park")).length === 1, slog.filter((x) => /start/.test(x)).join(" | "));
  check("start:perth yellow with the list of districts", slog.some((x) => /cs-l-warn/.test(x) && x.includes("start:perth") && /kingspark/.test(x)), slog.filter((x) => /start/.test(x)).join(" | "));
  await page.screenshot({ path: path.join(OUT, "shell_usb.png") });

  // USB robustness (lesson 1, 11/9: boards dropped out until the page was refreshed)
  check("port opened with a big read buffer", (await ev((i) => window.__serial.ports[i].opts && window.__serial.ports[i].opts.bufferSize, p)) >= 4096);
  await ev((i) => window.__serial.ports[i].glitch(), p);
  await sleep(100);
  await push(p, "st:12\n");
  await sleep(300);
  check("a read glitch (buffer overrun) keeps the board connected", (await lines()).includes("usb1|st:12") && (await ev(() => window.__fakeUnity.status.usb1)).startsWith("usb1|usb|1"));
  check("the glitch is noted in the monitor", (await logText()).some((x) => /cs-l-warn/.test(x) && /read glitch \(BufferOverrunError/.test(x)));
  await ev((i) => { window.__serial.ports[i].failOpens = 1; window.__serial.ports[i].die(); }, p); // dies, and the first reopen fails too
  const back = await page.waitForFunction((i) => window.__serial.ports[i].opened && window.__fakeUnity.status.usb1.startsWith("usb1|usb|1"), { timeout: 4000, polling: 50 }, p).then(() => true, () => false);
  await push(p, "st:13\n");
  await sleep(300);
  check("a connection that breaks without an unplug is closed and opened again", back && (await ev((i) => window.__serial.ports[i].closes, p)) >= 1 && (await lines()).includes("usb1|st:13"),
    JSON.stringify(await ev(() => window.CityScooterShell.usbLog)));
  check("the monitor says it reconnected", (await logText()).some((x) => /broke without an unplug/.test(x)) && (await logText()).some((x) => /connected again/.test(x)));

  // silent board: second board sends nothing
  await page.click("#cs-connect");
  for (let i = 0; i < 19; i++) { await push(p, "st:-35\n"); await sleep(200); } // usb1 keeps sending like a real program
  const cards = await ev(() => [...document.querySelectorAll(".cs-board")].map((c) => c.textContent));
  check("second board is usb2", cards.some((c) => c.startsWith("usb2")), JSON.stringify(cards));
  check("silent warning on usb2 only", cards.some((c) => c.startsWith("usb2") && /not sending anything/.test(c)) && !cards.some((c) => c.startsWith("usb1") && /not sending/.test(c)));
  await push(2, "st:10\n");
  await push(p, "st:-35\n");
  await sleep(700);
  check("silent warning clears when data arrives", !(await ev(() => document.getElementById("cs-boards").textContent)).includes("not sending"));
  // boards do not interfere
  await push(2, "st:50\n");
  await push(p, "st:-20\n");
  await sleep(300);
  const L2 = await lines();
  check("two boards keep their own ids", L2.includes("usb2|st:50") && L2.includes("usb1|st:-20"));

  // unplug + replug keeps the id
  await ev((i) => { window.__serial.ports[i].unplug(); window.__serial.fire("disconnect", window.__serial.ports[i]); }, p);
  await sleep(200);
  check("unplug sends status 0", (await ev(() => window.__fakeUnity.status.usb1)).startsWith("usb1|usb|0"));
  // the re-plugged board (a V1 after a flash) is not ready at first: the first two opens fail
  await ev(() => { const np = new window.__serial.FakePort("replug"); np.failOpens = 2; window.__serial.ports.push(np); window.__serial.fire("connect", np); });
  await sleep(200);
  check("replug not ready yet: still trying", (await ev(() => window.__fakeUnity.status.usb1)).startsWith("usb1|usb|0"));
  const replugged = await page.waitForFunction(() => window.__fakeUnity.status.usb1.startsWith("usb1|usb|1"), { timeout: 3000, polling: 50 }).then(() => true, () => false);
  check("replug comes back as usb1 after the open tries", replugged, await ev(() => window.__fakeUnity.status.usb1));

  // ---------------- Simulator mode (real simulator)
  await page.click('.cs-modes button[data-mode="sim"]');
  await sleep(200);
  check("sim mode disables Unity keyboard", (await calls()).slice(-8).some((c) => c.includes('SetKeyboardEnabled("0")')));
  check("usb boards are switched off for the game in sim mode", (await ev(() => window.__fakeUnity.status.usb2)).startsWith("usb2|usb|0"));
  const frame = await page.waitForFunction(() => document.getElementById("cs-sim").src.includes("usermbit"), { timeout: 3000 }).then(() => true, () => false);
  check("simulator iframe loads", frame);
  // Run before ▶ must not flash
  await sleep(4000);
  await page.click("#cs-run");
  await sleep(300);
  check("Run before ▶ only shows the hint", /First click/.test(await ev(() => document.getElementById("cs-sim-hint").textContent)));
  await page.waitForFunction(() => window.MicrobitBlocks && document.getElementById("cs-preview").textContent.length > 100, { timeout: 15000 });
  await ev(() => document.querySelector('.cs-tabs button[data-view="python"]').click());
  check("Python view starts with the code made by the starter blocks", /# Made with blocks/.test(await ev(() => document.getElementById("cs-code").value)));
  // Use labcheck (it echoes every game message) so we can see messages arrive in the simulator.
  await ev(async () => { const t = document.getElementById("cs-code"); t.value = await (await fetch("/tools/labcheck/labcheck.py")).text(); });
  const simFrame = page.frames().find((f) => f.url().includes("python-simulator.usermbit.org"));
  await simFrame.waitForSelector(".play-button", { timeout: 15000 });
  await simFrame.click(".play-button");
  const gotSim = await page.waitForFunction(() => window.__fakeUnity.lines.some((l) => l.startsWith("sim|st:")), { timeout: 20000 }).then(() => true, () => false);
  check("after ▶ the code runs and sim lines reach Unity", gotSim);
  check("status sim connected", ((await ev(() => window.__fakeUnity.status.sim)) || "").startsWith("sim|sim|1"));
  // keyboard -> virtual micro:bit (focus outside the editor)
  await page.click(".cs-monitor-head strong");
  await page.keyboard.down("ArrowLeft");
  await sleep(900);
  const st = await ev(() => { const s = window.__fakeUnity.lines.filter((l) => l.startsWith("sim|st:")); return s.slice(-1)[0]; });
  await page.keyboard.up("ArrowLeft");
  check("ArrowLeft tilts the simulator left", !!st && parseFloat(st.split(":")[1]) <= -100, st);
  const before = (await lines()).filter((l) => l === "sim|jp").length;
  await page.keyboard.press("a");
  await sleep(500);
  check("A key presses button A (jp)", (await lines()).filter((l) => l === "sim|jp").length > before);
  await page.evaluate(() => [...document.querySelectorAll("#dev-tools button")].find((b) => b.textContent === "gift").click());
  const echo = await page.waitForFunction(() => [...document.querySelectorAll("#cs-log .cs-line")].some((r) => r.textContent.includes("echo:gift")), { timeout: 5000 }).then(() => true, () => false);
  check("game message reaches the simulator (echo:gift)", echo);
  check("gift not written to the unused USB board", !(await ev(() => window.__serial.ports.map((x) => x.written).join(""))).includes("gift"));
  await page.screenshot({ path: path.join(OUT, "shell_sim.png") });

  // editor: Tab, draft saved, Run re-flashes after ▶
  await page.click("#cs-code");
  await ev(() => { const t = document.getElementById("cs-code"); t.value = "x = 1\n"; t.setSelectionRange(6, 6); });
  await page.keyboard.press("Tab");
  check("Tab inserts 4 spaces", (await ev(() => document.getElementById("cs-code").value)) === "x = 1\n    ");
  await ev(() => { const t = document.getElementById("cs-code"); t.value = 'from microbit import *\nwhile True:\n    print("st:42")\n    sleep(50)\n'; t.dispatchEvent(new Event("input")); });
  await page.click("#cs-run");
  const reflashed = await page.waitForFunction(() => window.__fakeUnity.lines.slice(-5).includes("sim|st:42"), { timeout: 8000 }).then(() => true, () => false);
  check("Run re-flashes edited code after the first ▶", reflashed);
  await sleep(700);

  // leave sim mode
  await page.click('.cs-modes button[data-mode="keyboard"]');
  await sleep(300);
  check("back to keyboard re-enables Unity keys", (await calls()).slice(-8).some((c) => c.includes('SetKeyboardEnabled("1")')));
  check("sim reported off", (await ev(() => window.__fakeUnity.status.sim)).startsWith("sim|sim|0"));
  const n1 = (await lines()).length; await sleep(800); const n2 = (await lines()).length;
  check("no sim lines reach Unity after leaving", n1 === n2, n1 + " -> " + n2);

  // draft persists across reload (localStorage); mode remembered
  await page.evaluate(() => {}); // keep page
  const page2 = await browser.newPage();
  await page2.setViewport({ width: 1920, height: 1080 });
  page2.on("pageerror", (e) => errors.push("page2 pageerror: " + e.message));
  await page2.evaluateOnNewDocument(installFakeSerial);
  await page2.goto(URL, { waitUntil: "load" });
  await sleep(1500);
  check("draft restored from localStorage", (await page2.evaluate(() => document.getElementById("cs-code").value)).includes("st:42"));
  check("last chosen mode remembered", (await page2.evaluate(() => document.body.dataset.mode)) === "keyboard");
  await page2.click("#cs-help-btn");
  await sleep(200);
  await page2.screenshot({ path: path.join(OUT, "shell_help_1080.png") });
  await page2.keyboard.press("Escape");
  check("Escape closes help", await page2.evaluate(() => document.getElementById("cs-help").hidden));

  // micro:bit-only page (keyboard: false, as deployed for lessons): no Keyboard mode, Unity never gets keys,
  // a remembered "keyboard" mode becomes USB.
  const page3 = await browser.newPage();
  await page3.setViewport({ width: 1366, height: 768 });
  page3.on("pageerror", (e) => errors.push("page3 pageerror: " + e.message));
  await page3.evaluateOnNewDocument(installFakeSerial);
  await page3.evaluateOnNewDocument(() => { try { localStorage.setItem("cityscooter.mode", "keyboard"); } catch (e) {} });
  await page3.goto(URL + "?keyboard=0", { waitUntil: "load" });
  await page3.waitForFunction(() => window.__fakeUnity && window.__fakeUnity.calls.some((c) => c.includes("SetKeyboardEnabled")), { timeout: 5000 });
  const calls3 = () => page3.evaluate(() => window.__fakeUnity.calls.filter((c) => c.includes("SetKeyboardEnabled")));
  check("no keyboard: no Keyboard mode button", (await page3.evaluate(() => document.querySelectorAll('.cs-modes button[data-mode="keyboard"]').length)) === 0);
  check("no keyboard: no keyboard pane", (await page3.evaluate(() => document.querySelectorAll('.cs-pane[data-pane="keyboard"]').length)) === 0);
  check("no keyboard: remembered keyboard mode becomes USB", (await page3.evaluate(() => document.body.dataset.mode)) === "usb");
  check("no keyboard: Unity keys off", (await calls3()).every((c) => c.includes('SetKeyboardEnabled("0")')), (await calls3()).join(" "));
  await page3.click('.cs-modes button[data-mode="sim"]');
  await sleep(300);
  await page3.click('.cs-modes button[data-mode="usb"]');
  await sleep(300);
  check("no keyboard: still off after switching modes", (await calls3()).every((c) => c.includes('SetKeyboardEnabled("0")')), (await calls3()).join(" "));
  await page3.screenshot({ path: path.join(OUT, "shell_no_keyboard.png") });

  check("no page errors", errors.length === 0, errors.slice(0, 5).join(" | "));
  console.log(failures ? failures + " FAILED" : "ALL PASSED");
  await browser.close();
  server.close();
  process.exit(failures ? 1 : 0);
})().catch((e) => { console.error(e); process.exit(2); });
