// Radio race (11/9): microbit/race.py and microbit/race_start.py compile, and run in the REAL simulator embedded in
// tools/shell-dev (V2 and forced V1), with the shell's robot racers. Also pins down the simulator's radio format.
//   node race.test.js <repoRoot> [outDir]
const http = require("http");
const fs = require("fs");
const path = require("path");
const { execFileSync } = require("child_process");
const puppeteer = require("puppeteer-core");

const ROOT = path.resolve(process.argv[2]);
const OUT = path.resolve(process.argv[3] || __dirname);
const CHROME = "C:/Program Files/Google/Chrome/Application/chrome.exe";
const SIM = "https://python-simulator.usermbit.org";
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

// CPython syntax check without running or writing anything (-I: ignore the environment and the script's folder).
function compiles(file) {
  try { execFileSync("python", ["-I", "-c", "import sys; compile(open(sys.argv[1], encoding='utf-8').read(), sys.argv[1], 'exec')", file], { stdio: "pipe" }); return ""; }
  catch (e) { return String(e.stderr || e.message); }
}

const RACE = fs.readFileSync(path.join(ROOT, "microbit/race.py"), "utf8");
const STARTER = fs.readFileSync(path.join(ROOT, "microbit/race_start.py"), "utf8");
const forceV1 = (py) => py.replace("from microbit import microphone", "from microbit import microphone_not_on_v1");
// What the simulator does with radio_input: a text packet needs MicroPython's header \x01\x00\x01, like radio.send() makes.
const PROBE = `from microbit import *
import radio
uart.init(baudrate=115200)
radio.on()
radio.config(group=12, queue=10)
print("probe")
while True:
    try:
        m = radio.receive()
        while m:
            print("got:" + m)
            m = radio.receive()
    except ValueError as e:
        print("err:" + str(e))
    sleep(50)
`;

(async () => {
  for (const f of ["microbit/race.py", "microbit/race_start.py"]) {
    const err = compiles(path.join(ROOT, f));
    check(f + " compiles (CPython syntax)", !err, err);
    check(f + ": no display.scroll (it stops the loop for seconds)", !/display\.scroll/.test(fs.readFileSync(path.join(ROOT, f), "utf8")));
  }

  await new Promise((r) => server.listen(8795, "127.0.0.1", r));
  const browser = await puppeteer.launch({ executablePath: CHROME, headless: true, args: ["--autoplay-policy=no-user-gesture-required"] });
  const page = await browser.newPage();
  await page.setViewport({ width: 1366, height: 768 });
  const errors = [];
  page.on("pageerror", (e) => errors.push("pageerror: " + e.message));
  page.on("console", (m) => { if (m.type() === "error") errors.push("console.error: " + m.text()); });
  page.on("dialog", (d) => d.accept());
  await page.evaluateOnNewDocument(() => {
    try { localStorage.clear(); localStorage.setItem("cityscooter.mode", "sim"); localStorage.setItem("cityscooter.view", "python"); } catch (e) {}
    window.__radioOut = []; window.__radioHeaders = []; window.__radioState = [];
    window.addEventListener("message", (e) => {
      const m = e.data || {};
      if (m.kind === "radio_output" && m.data instanceof Uint8Array) {
        window.__radioHeaders.push(Array.from(m.data.subarray(0, 3)).join(","));
        window.__radioOut.push(new TextDecoder().decode(m.data.subarray(3)));
      } else if (m.kind === "state_change" && m.change && m.change.radio) {
        window.__radioState.push(m.change.radio.enabled + ":" + m.change.radio.group);
      }
    });
  });
  await page.goto("http://127.0.0.1:8795/tools/shell-dev/?keyboard=0", { waitUntil: "load" });
  const ev = (fn, ...a) => page.evaluate(fn, ...a);
  await page.waitForFunction(() => document.getElementById("cs-sim").src.includes("usermbit"), { timeout: 5000 });
  await sleep(3000);
  const setCode = (code) => ev((c) => { const t = document.getElementById("cs-code"); t.value = c; t.dispatchEvent(new Event("input")); }, code);
  const log = () => ev(() => [...document.querySelectorAll("#cs-log .cs-line:not([data-old])")].map((r) => r.className + " :: " + r.textContent));
  const markOld = () => ev(() => document.querySelectorAll("#cs-log .cs-line").forEach((r) => { r.dataset.old = "1"; }));
  const radioIn = (bytes) => ev((b, o) => document.getElementById("cs-sim").contentWindow.postMessage({ kind: "radio_input", data: new Uint8Array(b) }, o), bytes, SIM);
  const ascii = (s) => Array.from(Buffer.from(s, "ascii"));

  // ---- the simulator's radio format (micropython-microbit-v2-simulator: board/radio.ts, README "radio_input")
  await setCode(PROBE);
  const simFrame = page.frames().find((f) => f.url().includes("python-simulator.usermbit.org"));
  await simFrame.waitForSelector(".play-button", { timeout: 20000 });
  await simFrame.click(".play-button");
  check("probe runs", await page.waitForFunction(() => window.__fakeUnity.lines.includes("sim|probe"), { timeout: 20000 }).then(() => true, () => false));
  await page.waitForFunction(() => window.__radioState.some((s) => s === "true:12"), { timeout: 5000 }).catch(() => {});
  check("state_change reports the radio on, in group 12", await ev(() => window.__radioState.slice(-1)[0] === "true:12"), await ev(() => window.__radioState.join(" ")));
  const t0 = await ev(() => window.__fakeUnity.total);
  await radioIn([1, 0, 1, ...ascii("p:BOT1:12:-1")]); await sleep(300);
  await radioIn(ascii("p:BOT1:13:-1")); await sleep(300);
  await radioIn([1, 0, 1, ...ascii("p:BOT1:14:-1")]); await sleep(300);
  await radioIn([1, 0, 1, ...ascii("0123456789012345678901234567890123456789")]); await sleep(300);
  const probe = await ev((n) => window.__fakeUnity.since(n), t0);
  check("radio_input with \\x01\\x00\\x01 + text: radio.receive() gives the text", probe.includes("sim|got:p:BOT1:12:-1"), probe.join(" "));
  check("radio_input without the header: radio.receive() raises ValueError (and drops that packet)",
    probe.includes("sim|err:received packet is not a string") && probe.includes("sim|got:p:BOT1:14:-1") && !probe.some((l) => l.includes("13:-1")), probe.join(" "));
  check("radio payloads stop at 32 bytes: 29 characters of text", probe.includes("sim|got:01234567890123456789012345678"), probe.slice(-1));

  // ---- race.py: the Lesson 1 starter plus the relay
  async function runRace(py, label) {
    await markOld();
    await ev(() => { window.__radioOut.length = 0; window.__radioHeaders.length = 0; });
    const before = await ev(() => window.__fakeUnity.total);
    await setCode(py);
    await page.click("#cs-run");
    check(label + ": race:AMY:12 reaches the game", await page.waitForFunction((n) => window.__fakeUnity.since(n).includes("sim|race:AMY:12"), { timeout: 8000 }, before).then(() => true, () => false));
    if ((await ev(() => document.getElementById("cs-bots").getAttribute("aria-pressed"))) !== "true") await page.click("#cs-bots");
    await sleep(2600);
    let lines = await ev((n) => window.__fakeUnity.since(n), before);
    for (const bot of ["BOT1", "BOT2", "BOT3"]) check(label + ": rx:p:" + bot + " reaches the game", lines.some((l) => new RegExp("^sim\\|rx:p:" + bot + ":\\d+:-?\\d$").test(l)));
    check(label + ": steering still goes to the game", lines.some((l) => /^sim\|st:-?\d+$/.test(l)));
    for (let i = 0; i < 4; i++) { await ev((l) => window.CityScooterShell.sendToDevices("*", l), "pos:" + (300 + i * 4) + ":1"); await sleep(250); }
    await ev(() => window.CityScooterShell.sendToDevices("*", "fin:65.10"));
    await sleep(700);
    const out = await ev(() => window.__radioOut.slice());
    check(label + ": pos:312:1 goes out by radio as p:AMY:312:1", out.includes("p:AMY:312:1"), out.join(" "));
    check(label + ": fin:65.10 goes out as f:AMY:65.10", out.includes("f:AMY:65.10"), out.join(" "));
    check(label + ": radio_output carries the 3-byte text header", await ev(() => window.__radioHeaders.length > 0 && window.__radioHeaders.every((h) => h === "1,0,1")));
    await page.click("#cs-bots-go");
    await sleep(1200);
    lines = await ev((n) => window.__fakeUnity.since(n), before);
    check(label + ": Send go -> rx:go:<seed>:uwa:2000", lines.some((l) => /^sim\|rx:go:\d{1,5}:uwa:2000$/.test(l)));
    const L = await log();
    check(label + ": monitor sums up the radio once a second", L.some((x) => /radio: \d+ messages\/s from BOT\d, BOT\d, BOT\d$/.test(x)), L.filter((x) => /radio/.test(x)).slice(-2).join(" | "));
    check(label + ": no Python error", !L.some((x) => /cs-l-error/.test(x)), L.filter((x) => /cs-l-error/.test(x)).slice(0, 3).join(" | "));
    await page.click("#cs-bots"); // robots off
    return lines;
  }
  const v2 = await runRace(RACE, "race.py on V2");
  check("race.py on V2: hi:v2", v2.includes("sim|hi:v2"));
  const v1 = await runRace(forceV1(RACE), "race.py forced V1");
  check("race.py forced V1: hi:v1", v1.includes("sim|hi:v1"));
  await page.screenshot({ path: path.join(OUT, "race_py_sim.png") });

  // ---- race_start.py: the teacher's starter
  async function runStarter(py, label) {
    await markOld();
    await ev(() => { window.__radioOut.length = 0; window.__radioState.length = 0; });
    const before = await ev(() => window.__fakeUnity.total);
    await setCode(py);
    await page.click("#cs-run");
    await page.waitForFunction(() => window.__radioState.slice(-1)[0] === "true:11", { timeout: 8000 }).catch(() => {});
    check(label + ": starts in group 11", await ev(() => window.__radioState.slice(-1)[0] === "true:11"), await ev(() => window.__radioState.join(" ")));
    await page.click(".cs-monitor-head strong");
    await page.keyboard.press("b"); await sleep(1500);
    check(label + ": B -> group 12", await ev(() => window.__radioState.slice(-1)[0] === "true:12"), await ev(() => window.__radioState.join(" ")));
    check(label + ": B sends nothing", (await ev(() => window.__radioOut.length)) === 0);
    await page.keyboard.press("a");
    await sleep(2500);
    check(label + ": A counts down first (nothing sent after 2.5 s)", (await ev(() => window.__radioOut.length)) === 0, await ev(() => window.__radioOut.join(" ")));
    await page.waitForFunction(() => window.__radioOut.length >= 5, { timeout: 5000 }).catch(() => {}); // the simulator's clock runs a bit slow
    await sleep(600);
    let out = await ev(() => window.__radioOut.slice());
    const go = out.filter((t) => /^go:\d{1,5}:uwa:2000$/.test(t));
    check(label + ": A sends go:<seed>:uwa:2000 5 times (one seed) after 3-2-1", go.length === 5 && new Set(go).size === 1 && out.length === 5, out.join(" "));
    check(label + ": ... to group 12", await ev(() => window.__radioState.slice(-1)[0] === "true:12"));
    let lines = await ev((n) => window.__fakeUnity.since(n), before);
    check(label + ": prints which race it started", lines.some((l) => /^sim\|race 12: go:\d+:uwa:2000$/.test(l)), lines.filter((l) => /race/.test(l)).join(" "));
    // A+B: every race, 11 to 15, the same road
    await ev(() => { window.__radioOut.length = 0; window.__radioState.length = 0; });
    await page.keyboard.down("a"); await page.keyboard.down("b"); await sleep(120);
    await page.keyboard.up("a"); await page.keyboard.up("b");
    await page.waitForFunction(() => window.__radioOut.length >= 25, { timeout: 9000 }).catch(() => {});
    await sleep(800);
    out = await ev(() => window.__radioOut.slice());
    const all = out.filter((t) => /^go:\d{1,5}:uwa:2000$/.test(t));
    check(label + ": A+B sends go 25 times (5 races x 5), one seed", all.length === 25 && new Set(all).size === 1 && out.length === 25, out.length + " " + out.slice(0, 3).join(" "));
    const groups = await ev(() => window.__radioState.filter((s) => s.startsWith("true:")).map((s) => s.slice(5)));
    check(label + ": A+B goes through groups 11, 12, 13, 14, 15, then back to 12", groups.join(",") === "11,12,13,14,15,12", groups.join(","));
    lines = await ev((n) => window.__fakeUnity.since(n), before);
    check(label + ": prints all five races", [11, 12, 13, 14, 15].every((g) => lines.some((l) => l.startsWith("sim|race " + g + ": go:"))));
    // B four more times: 13, 14, 15, then 20 (the final)
    for (let i = 0; i < 4; i++) { await page.keyboard.press("b"); await sleep(1300); }
    check(label + ": B after 15 is the final, group 20", await ev(() => window.__radioState.slice(-1)[0] === "true:20"), await ev(() => window.__radioState.slice(-3).join(" ")));
    const L = await log();
    check(label + ": no Python error", !L.some((x) => /cs-l-error/.test(x)), L.filter((x) => /cs-l-error/.test(x)).slice(0, 3).join(" | "));
  }
  await runStarter(STARTER, "race_start.py"); // it uses nothing V2-only, so there is no separate V1 branch to force
  await page.screenshot({ path: path.join(OUT, "race_start_sim.png") });

  check("no page errors", errors.length === 0, errors.slice(0, 5).join(" | "));
  console.log(failures ? failures + " FAILED" : "ALL PASSED");
  await browser.close();
  server.close();
  process.exit(failures ? 1 : 0);
})().catch((e) => { console.error(e); process.exit(2); });
