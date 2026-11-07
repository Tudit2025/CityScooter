// Headless test of "Send to micro:bit" in tools/shell-dev: the page's Web Serial is bridged to fake boards
// (fake_repl_board.py: CPython runs each raw-REPL command, like a MicroPython micro:bit).   node send.test.js <repoRoot> [outDir]
const http = require("http");
const fs = require("fs");
const path = require("path");
const os = require("os");
const { spawn } = require("child_process");
const puppeteer = require("puppeteer-core");

const ROOT = path.resolve(process.argv[2]);
const OUT = path.resolve(process.argv[3] || __dirname);
const BOARD_DIR = fs.mkdtempSync(path.join(os.tmpdir(), "cs_send_"));
const CHROME = "C:/Program Files/Google/Chrome/Application/chrome.exe";
const TYPES = { ".html": "text/html", ".js": "application/javascript", ".css": "text/css", ".py": "text/plain", ".svg": "image/svg+xml",
  ".woff2": "font/woff2", ".png": "image/png", ".gif": "image/gif", ".cur": "image/x-icon" };
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
const check = (name, ok, extra) => { console.log((ok ? "PASS " : "FAIL ") + name + (extra !== undefined ? "  " + String(extra).slice(0, 300) : "")); if (!ok) failures++; };

// Page side: navigator.serial whose ports are bridged to Node (window.__toBoard / __boardOpen).
function installBridgeSerial() {
  const listeners = { connect: [], disconnect: [] };
  const ports = {};
  let n = 0;
  class BridgePort {
    constructor(id) { this.id = id; this.readable = null; this.writable = null; this.maxWrite = 0; }
    async open() {
      await window.__boardOpen(this.id);
      this.readable = new ReadableStream({ start: (c) => { this.ctl = c; } });
      this.writable = new WritableStream({ write: async (chunk) => {
        this.maxWrite = Math.max(this.maxWrite, chunk.length);
        await window.__toBoard(this.id, Array.from(chunk));
      } });
    }
    push(arr) { try { if (this.ctl) this.ctl.enqueue(new Uint8Array(arr)); } catch (e) { /* closed */ } }
    unplug() { try { this.ctl.error(new Error("device lost")); } catch (e) {} this.readable = null; this.writable = null; }
  }
  const serial = {
    async requestPort() { const p = new BridgePort("b" + n++); ports[p.id] = p; return p; },
    async getPorts() { return []; },
    addEventListener(type, fn) { (listeners[type] || []).push(fn); },
  };
  Object.defineProperty(Navigator.prototype, "serial", { get: () => serial, configurable: true });
  window.__bridge = { ports, fire(type, port) { for (const fn of listeners[type]) fn({ target: port }); } };
}

(async () => {
  await new Promise((r) => server.listen(8793, "127.0.0.1", r));
  const browser = await puppeteer.launch({ executablePath: CHROME, headless: true });
  const page = await browser.newPage();
  await page.setViewport({ width: 1366, height: 768 });
  const errors = [];
  page.on("pageerror", (e) => errors.push("pageerror: " + e.message));
  page.on("console", (m) => { if (m.type() === "error") errors.push("console.error: " + m.text()); });

  const procs = {};
  let nextFlags = [];
  await page.exposeFunction("__boardOpen", (id) => {
    const p = spawn("python", ["-I", path.join(__dirname, "fake_repl_board.py"), BOARD_DIR, id, ...nextFlags]);
    nextFlags = [];
    p.stdout.on("data", (buf) => { page.evaluate((id, a) => window.__bridge.ports[id].push(a), id, Array.from(buf)).catch(() => {}); });
    p.stderr.on("data", (b) => process.stderr.write("[" + id + "] " + b));
    procs[id] = p;
  });
  await page.exposeFunction("__toBoard", (id, arr) => { const p = procs[id]; if (p && !p.killed) p.stdin.write(Buffer.from(arr)); });
  await page.evaluateOnNewDocument(installBridgeSerial);
  await page.evaluateOnNewDocument(() => { try { localStorage.clear(); localStorage.setItem("cityscooter.mode", "usb"); } catch (e) {} });

  await page.goto("http://127.0.0.1:8793/tools/shell-dev/", { waitUntil: "load" });
  await page.waitForFunction(() => window.__fakeUnity && window.__fakeUnity.calls.some((c) => c.includes("SetKeyboardEnabled")), { timeout: 5000 });
  const ev = (fn, ...a) => page.evaluate(fn, ...a);
  const msg = () => ev(() => document.getElementById("cs-usb-msg").textContent);
  const waitMsg = (re, ms) => page.waitForFunction((src) => new RegExp(src).test(document.getElementById("cs-usb-msg").textContent),
    { timeout: ms || 30000 }, re.source).then(() => true, () => false);
  const mainPy = (id) => { try { return fs.readFileSync(path.join(BOARD_DIR, id + "_main.py"), "utf8"); } catch (e) { return null; } };
  const boardLog = (id) => { try { return fs.readFileSync(path.join(BOARD_DIR, id + "_log.txt"), "utf8"); } catch (e) { return ""; } };

  // ---------------- no board yet
  await page.waitForFunction(() => !document.getElementById("cs-blockly-msg"), { timeout: 15000 });
  check("Send button shown in USB mode", await ev(() => getComputedStyle(document.getElementById("cs-send")).display !== "none"));
  await page.click("#cs-send");
  check("Send without a board asks to connect", /Connect your micro:bit first/.test(await msg()), await msg());

  // ---------------- connect board b0 and send the starter blocks
  await page.click("#cs-connect");
  const hello = await page.waitForFunction(() => (window.__fakeUnity.status.usb1 || "") === "usb1|usb|1|v2", { timeout: 8000 }).then(() => true, () => false);
  check("fake board connected and said hi:v2", hello, await ev(() => JSON.stringify(window.__fakeUnity.status)));
  const blocksCode = await ev(() => document.getElementById("cs-preview").textContent);
  const t0 = Date.now();
  await page.click("#cs-send");
  await sleep(300);
  check("button shows progress while sending", /Sending/.test(await ev(() => document.getElementById("cs-send").textContent)));
  check("board card shows progress", /Sending your code/.test(await ev(() => document.getElementById("cs-boards").textContent)));
  await ev(() => window.CityScooterShell.sendToDevices("*", "crash")); // the game keeps talking while we send
  const sent = await waitMsg(/^Sent!/);
  check("Sent! message", sent, await msg());
  console.log("     send took " + (Date.now() - t0) + " ms for " + blocksCode.length + " chars");
  check("main.py on the board is exactly the blocks' code", mainPy("b0") === blocksCode.replace(/\r\n?/g, "\n"),
    (mainPy("b0") || "").length + " vs " + blocksCode.length);
  check("no serial write over 32 bytes (V1 buffer)", (await ev(() => window.__bridge.ports.b0.maxWrite)) <= 32,
    await ev(() => window.__bridge.ports.b0.maxWrite));
  // A typed "crash\n" would show up raw in a command (the code itself only has crash inside escaped b'...' pieces).
  check("game's crash was not typed into the REPL", boardLog("b0").includes("w(b'") && !/crash\\n/.test(boardLog("b0")));
  const newProg = await page.waitForFunction(() => window.__fakeUnity.lines.includes("usb1|st:7"), { timeout: 5000 }).then(() => true, () => false);
  check("board restarted with the new program and the game gets its lines", newProg);
  check("button back to Send to micro:bit", (await ev(() => document.getElementById("cs-send").textContent)) === "Send to micro:bit");
  const logRows = await ev(() => [...document.querySelectorAll("#cs-log .cs-line")].map((r) => r.textContent).join("\n"));
  check("monitor says sending and sent", /Sending your code/.test(logRows) && /Sent!/.test(logRows));
  check("no REPL noise in the monitor", !/raw REPL|KeyboardInterrupt|>>>|\bOK\b/.test(logRows), logRows.slice(-400));
  check("no silent warning while sending", !/No data from this micro:bit/.test(logRows));
  check("the dropped crash is not shown as sent", !/gamecrash/.test(logRows), logRows);
  await page.screenshot({ path: path.join(OUT, "send_done.png") });

  // ---------------- Python view: quotes, backslashes, non-ASCII, CRLF
  const tricky = "from microbit import *\r\n# héllo 🛴 it's \"quoted\" \\ back\\slash\r\nprint('it\\'s \\\\ ok')\r\nx = b'\\x00'\r\n";
  await page.click('.cs-tabs button[data-view="python"]');
  await ev((t) => { const ta = document.getElementById("cs-code"); ta.value = t; ta.dispatchEvent(new Event("input")); }, tricky);
  await page.click("#cs-send");
  check("Python view sent", await waitMsg(/^Sent!/));
  check("tricky Python arrives byte for byte (CRLF -> LF)", mainPy("b0") === tricky.replace(/\r\n/g, "\n"), JSON.stringify(mainPy("b0")));
  await page.click('.cs-tabs button[data-view="blocks"]');

  // ---------------- a board without MicroPython (MakeCode)
  nextFlags = ["--no-repl"];
  await page.click("#cs-connect");
  await page.waitForFunction(() => (window.__fakeUnity.status.usb2 || "").startsWith("usb2|usb|1"), { timeout: 8000 });
  check("two boards: per-board Send buttons", (await ev(() => document.querySelectorAll(".cs-board-send").length)) === 2);
  await page.click("#cs-send");
  check("two boards: head button asks which one", /Send my code here/.test(await msg()), await msg());
  await page.$$eval(".cs-board-send", (bs) => bs[1].click());
  const longWay = await waitMsg(/long way/, 15000);
  check("no MicroPython: tells the long way", longWay, await msg());
  const before = await ev(() => window.__fakeUnity.lines.filter((l) => l === "usb2|st:0").length);
  await sleep(600);
  check("that board keeps playing after the failed send", (await ev(() => window.__fakeUnity.lines.filter((l) => l === "usb2|st:0").length)) > before);
  check("other board untouched", mainPy("b1") === null);

  // ---------------- full board
  await ev(() => { const p = window.__bridge.ports.b1; p.unplug(); window.__bridge.fire("disconnect", p); });
  procs.b1.kill();
  nextFlags = ["--full"];
  await page.click("#cs-connect"); // b2 takes the free usb2 slot
  await page.waitForFunction(() => (window.__fakeUnity.status.usb2 || "").startsWith("usb2|usb|1"), { timeout: 8000 });
  await page.$$eval(".cs-board-send", (bs) => bs[1].click());
  check("full board: no room message", await waitMsg(/no room/, 15000), await msg());

  // ---------------- unplug while sending
  await page.$$eval(".cs-board-send", (bs) => bs[0].click());
  await sleep(700);
  await ev(() => { const p = window.__bridge.ports.b0; p.unplug(); window.__bridge.fire("disconnect", p); });
  procs.b0.kill();
  check("unplugged mid-send: says so", await waitMsg(/unplugged/, 8000), await msg());
  check("unplugged board no longer sending", !(await ev(() => document.getElementById("cs-boards").textContent)).includes("Sending your code"));

  // ---------------- Simulator mode has no Send button
  await page.click('.cs-modes button[data-mode="sim"]');
  check("Send hidden in Simulator mode", await ev(() => getComputedStyle(document.getElementById("cs-send")).display === "none"));
  await page.click('.cs-modes button[data-mode="usb"]');

  check("no page errors", errors.length === 0, errors.join(" | "));
  for (const p of Object.values(procs)) p.kill();
  await browser.close();
  server.close();
  console.log(failures ? failures + " FAILED" : "ALL PASSED");
  process.exit(failures ? 1 : 0);
})().catch((e) => { console.error(e); process.exit(1); });
