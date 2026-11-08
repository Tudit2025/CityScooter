// Headless test of the Blocks editor in tools/shell-dev: generated MicroPython is valid, runs in the REAL simulator
// (V2 and forced-V1), Blocks/Python switching, persistence.   node blocktest.js <repoRoot> [outDir]
const http = require("http");
const fs = require("fs");
const path = require("path");
const os = require("os");
const { execFileSync } = require("child_process");
const puppeteer = require("puppeteer-core");

const ROOT = path.resolve(process.argv[2]);
const OUT = path.resolve(process.argv[3] || __dirname);
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
function pySyntax(code, tag) {
  const f = path.join(os.tmpdir(), "cs_blocks_" + tag + ".py");
  fs.writeFileSync(f, code);
  try { execFileSync("python", ["-c", "import ast,sys; ast.parse(open(sys.argv[1]).read())", f], { stdio: "pipe" }); return ""; }
  catch (e) { return String(e.stderr || e.message); }
}

// Every block in the toolbox, wired into one program.
const n = (v) => ({ shadow: { type: "math_number", fields: { NUM: v } } });
const t = (s) => ({ shadow: { type: "text", fields: { TEXT: s } } });
const B = (type, opts) => Object.assign({ type }, opts || {});
const val = (b) => ({ block: b });
const chain = (...bs) => { for (let i = bs.length - 2; i >= 0; i--) bs[i].next = { block: bs[i + 1] }; return { block: bs[0] }; };
const ALL = { blocks: { languageVersion: 0, blocks: [
  B("mb_on_start", { x: 10, y: 10, inputs: { DO: chain(
    B("mb_show", { inputs: { VALUE: val(B("mb_version")) } }),
    B("variables_set", { fields: { VAR: { id: "v1" } }, inputs: { VALUE: n(0) } }),
    B("math_change", { fields: { VAR: { id: "v1" } }, inputs: { DELTA: n(1) } }),
    B("mb_play", { fields: { TUNE: "POWER_UP" } })) } }),
  B("mb_forever", { x: 10, y: 200, inputs: { DO: chain(
    B("cs_steer", { inputs: { VALUE: val(B("math_constrain", { inputs: {
      VALUE: val(B("math_arithmetic", { fields: { OP: "DIVIDE" }, inputs: { A: val(B("mb_tilt", { fields: { AXIS: "x" } })), B: n(5) } })),
      LOW: n(-100), HIGH: n(100) } })) } }),
    B("controls_if", { inputs: {
      IF0: val(B("logic_operation", { fields: { OP: "OR" }, inputs: { A: val(B("mb_button_pressed", { fields: { BTN: "ab" } })), B: val(B("mb_logo_touched")) } })),
      DO0: chain(B("cs_jump")) } }),
    B("controls_if", { inputs: {
      IF0: val(B("logic_compare", { fields: { OP: "GT" }, inputs: { A: val(B("mb_sound_level")), B: n(120) } })),
      DO0: chain(B("cs_jump")) } }),
    B("controls_if", { extraState: { hasElse: true }, inputs: {
      IF0: val(B("logic_negate", { inputs: { BOOL: val(B("mb_pin_touched", { fields: { PIN: "1" } })) } })),
      DO0: chain(B("variables_set", { fields: { VAR: { id: "v1" } }, inputs: { VALUE: val(B("math_modulo", { inputs: { DIVIDEND: val(B("mb_running_time")), DIVISOR: n(7) } })) } })),
      ELSE: chain(B("cs_brake")) } }),
    B("controls_repeat_ext", { inputs: { TIMES: n(1), DO: chain(B("mb_print", { inputs: { VALUE: val(B("text_join", { extraState: { itemCount: 2 },
      inputs: { ADD0: t("v="), ADD1: val(B("variables_get", { fields: { VAR: { id: "v1" } } })) } })) } })) } }),
    B("controls_if", { inputs: {
      IF0: val(B("logic_compare", { fields: { OP: "EQ" }, inputs: { A: val(B("math_random_int", { inputs: { FROM: n(1), TO: n(50) } })), B: n(1) } })),
      DO0: chain(B("mb_show", { inputs: { VALUE: val(B("math_round", { fields: { OP: "ROUND" }, inputs: { NUM: val(B("math_single", { fields: { OP: "ABS" }, inputs: { NUM: n(-3.4) } })) } })) } })) } }),
    B("controls_whileUntil", { fields: { MODE: "WHILE" }, inputs: { BOOL: val(B("logic_boolean", { fields: { BOOL: "FALSE" } })) } }),
    // police (11/8): pull over when the police car is right behind you
    B("controls_if", { inputs: {
      IF0: val(B("logic_compare", { fields: { OP: "LT" }, inputs: { A: val(B("cs_cop")), B: n(2) } })),
      DO0: chain(B("cs_surrender")) } })) } }),
  B("mb_on_button", { x: 500, y: 10, fields: { BTN: "b" }, inputs: { DO: chain(B("mb_stop_sound"), B("mb_pause", { inputs: { MS: n(10) } })) } }),
  B("mb_on_button", { x: 500, y: 120, fields: { BTN: "a" }, inputs: { DO: chain(B("cs_jump")) } }),
  B("cs_on_message", { x: 500, y: 220, fields: { MSG: "crash" }, inputs: { DO: chain(B("mb_show_icon", { fields: { ICON: "SAD" } }),
    B("mb_play", { fields: { TUNE: "WAWAWAWAA" } }), B("mb_print", { inputs: { VALUE: t("got crash") } })) } }),
  B("cs_on_message", { x: 500, y: 380, fields: { MSG: "gift" }, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: t("got gift") } }), B("mb_broadcast", { fields: { NAME: "party" } })) } }),
  // broadcast (11/8): two listeners for one message, and radio between boards
  B("mb_on_receive", { x: 2100, y: 10, fields: { NAME: "party" }, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: t("party time") } })) } }),
  B("mb_on_receive", { x: 2100, y: 120, fields: { NAME: "party" }, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: t("second listener") } }), B("mb_radio_send", { inputs: { VALUE: { shadow: { type: "text", fields: { TEXT: "gift!" } } } } })) } }),
  B("mb_on_radio", { x: 2100, y: 240, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: val(B("mb_radio_msg")) } })) } }),
  B("cs_on_message", { x: 500, y: 460, fields: { MSG: "go" }, inputs: { DO: chain(B("mb_clear"), B("cs_say", { fields: { WORD: "party" } })) } }),
  B("cs_on_word", { x: 2100, y: 360, fields: { WORD: "party" }, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: t("word party") } })) } }),
  // the teacher's example (11/8): count punches that land; after 3, the game starts a police chase
  B("cs_on_message", { x: 2100, y: 480, fields: { MSG: "ko" }, inputs: { DO: chain(
    B("math_change", { fields: { VAR: { id: "v3" } }, inputs: { DELTA: n(1) } }),
    B("controls_if", { inputs: {
      IF0: val(B("logic_compare", { fields: { OP: "EQ" }, inputs: { A: val(B("variables_get", { fields: { VAR: { id: "v3" } } })), B: n(3) } })),
      DO0: chain(B("cs_do", { fields: { EVENT: "chase" } }), B("cs_stars", { inputs: { N: n(2) } })) } })) } }),
  // protocol v1: punch, bell, gesture, ko / warn: / turn:
  B("mb_on_gesture", { x: 900, y: 10, fields: { GESTURE: "shake" }, inputs: { DO: chain(B("cs_punch")) } }),
  B("mb_on_presses", { x: 900, y: 520, fields: { BTN: "a", TIMES: "2" }, inputs: { DO: chain(B("cs_boost")) } }),
  B("mb_on_presses", { x: 900, y: 640, fields: { BTN: "a", TIMES: "3" }, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: val(B("text_join", { extraState: { itemCount: 2 }, inputs: { ADD0: t("presses "), ADD1: val(B("mb_presses", { fields: { BTN: "a" } })) } })) } })) } }),
  // long press (11/8): hold B for 1 s, and how long it was held when let go
  B("mb_on_hold", { x: 1700, y: 10, fields: { BTN: "b", MS: "1000" }, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: t("held B 1s") } })) } }),
  B("mb_on_release", { x: 1700, y: 120, fields: { BTN: "b" }, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: val(B("text_join", { extraState: { itemCount: 2 }, inputs: { ADD0: t("released after "), ADD1: val(B("mb_held", { fields: { BTN: "b" } })) } })) } })) } }),
  B("cs_on_message", { x: 900, y: 120, fields: { MSG: "warn:ped" }, inputs: { DO: chain(B("cs_bell"), B("cs_toon", { fields: { ON: "1" } })) } }),
  B("cs_on_message", { x: 900, y: 420, fields: { MSG: "boing" }, inputs: { DO: chain(B("cs_toon", { fields: { ON: "0" } })) } }),
  B("cs_on_message", { x: 900, y: 220, fields: { MSG: "ko" }, inputs: { DO: chain(B("mb_show_icon", { fields: { ICON: "YES" } })) } }),
  B("cs_on_message", { x: 900, y: 320, fields: { MSG: "turn:-1" }, inputs: { DO: chain(B("mb_show_icon", { fields: { ICON: "ARROW_W" } })) } }),
  // police (11/8): siren on chase:1, dodge on inc:C, stars from wanted:N, busted
  B("cs_on_message", { x: 1300, y: 10, fields: { MSG: "chase:1" }, inputs: { DO: chain(B("mb_show_icon", { fields: { ICON: "ANGRY" } })) } }),
  B("cs_on_message", { x: 1300, y: 120, fields: { MSG: "inc:C" }, inputs: { DO: chain(B("cs_steer", { inputs: { VALUE: n(100) } }),
    B("mb_print", { inputs: { VALUE: t("dodge") } })) } }),
  B("cs_on_message", { x: 1300, y: 260, fields: { MSG: "busted" }, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: t("got busted") } })) } }),
  B("cs_on_wanted", { x: 1300, y: 360, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: val(B("text_join", { extraState: { itemCount: 2 },
    inputs: { ADD0: t("wanted "), ADD1: val(B("cs_wanted")) } })) } })) } }),
  // ramps and air (v0.3): a trick when the game says air:1; after a landed trick, ask for another ramp
  B("cs_on_message", { x: 2500, y: 10, fields: { MSG: "air:1" }, inputs: { DO: chain(B("cs_trick"), B("mb_print", { inputs: { VALUE: t("in the air") } })) } }),
  B("cs_on_message", { x: 2500, y: 140, fields: { MSG: "land:trick" }, inputs: { DO: chain(B("mb_print", { inputs: { VALUE: t("trick landed") } }),
    B("cs_do", { fields: { EVENT: "ramp" } })) } }),
  B("cs_on_speed", { x: 500, y: 540, inputs: { DO: chain(B("mb_clear"),
    B("controls_for", { fields: { VAR: { id: "v2" } }, inputs: { FROM: n(0), TO: n(4), BY: n(1), DO: chain(B("mb_plot", { inputs: {
      X: val(B("variables_get", { fields: { VAR: { id: "v2" } } })), Y: n(4), B: val(B("cs_speed")) } })) } }),
    B("mb_print", { inputs: { VALUE: val(B("text_join", { extraState: { itemCount: 2 }, inputs: { ADD0: t("speed "), ADD1: val(B("cs_speed")) } })) } })) } }),
] }, variables: [{ name: "count", id: "v1" }, { name: "x", id: "v2" }, { name: "hits", id: "v3" }] };

(async () => {
  await new Promise((r) => server.listen(8793, "127.0.0.1", r));
  const browser = await puppeteer.launch({ executablePath: CHROME, headless: true, args: ["--autoplay-policy=no-user-gesture-required"] });
  const page = await browser.newPage();
  await page.setViewport({ width: 1366, height: 768 });
  const errors = [];
  page.on("pageerror", (e) => errors.push("pageerror: " + e.message));
  page.on("console", (m) => { if (m.type() === "error") errors.push("console.error: " + m.text()); });
  page.on("requestfailed", (r) => { if (!/usermbit|favicon/.test(r.url())) errors.push("requestfailed: " + r.url()); });
  page.on("response", (r) => { if (r.status() >= 400 && !/favicon/.test(r.url())) errors.push("HTTP " + r.status() + " " + r.url()); });
  page.on("request", (r) => { const u = r.url(); if (!u.startsWith("http://127.0.0.1") && !u.startsWith("data:") && !u.startsWith("blob:") && !/usermbit/.test(u)) errors.push("external request: " + u); });
  page.on("dialog", (d) => d.accept());
  await page.evaluateOnNewDocument(() => { try { if (!sessionStorage.getItem("keep")) localStorage.clear(); } catch (e) {} });
  const URL = "http://127.0.0.1:8793/tools/shell-dev/";
  await page.goto(URL, { waitUntil: "load" });
  await sleep(1300);
  const ev = (fn, ...a) => page.evaluate(fn, ...a);
  await page.screenshot({ path: path.join(OUT, "comic_keyboard.png") });
  check("keyboard mode does not load Blockly", !(await ev(() => !!window.Blockly)));

  // ---- Simulator mode: blocks load with the starter
  await page.click('.cs-modes button[data-mode="sim"]');
  const loaded = await page.waitForFunction(() => window.MicrobitBlocks && document.querySelector("#cs-blockly .blocklySvg"), { timeout: 15000 }).then(() => true, () => false);
  check("Blocks load in Simulator mode", loaded);
  await sleep(500);
  const starterPy = await ev(() => document.getElementById("cs-preview").textContent);
  for (const want of ['print("st:" + str(int(accelerometer.get_x() / 5)))', "if button_b.is_pressed():", 'print("br")',
    "if button_a.was_pressed():", 'print("jp")', 'if msg == "crash":', "display.show(Image.SAD)", 'elif msg == "gift":',
    'elif msg == "go":', "uart.init(baudrate=115200)", "def messages():", 'print("hi:" + VERSION)', "sleep(50)"]) {
    check("starter blocks make: " + want, starterPy.includes(want));
  }
  const syn1 = pySyntax(starterPy, "starter");
  check("starter blocks: valid Python", !syn1, syn1);
  fs.writeFileSync(path.join(OUT, "blocks_starter.py"), starterPy);
  await page.screenshot({ path: path.join(OUT, "comic_blocks.png") });

  // Run the starter blocks in the real simulator
  await page.waitForFunction(() => document.getElementById("cs-sim").src.includes("usermbit"), { timeout: 5000 });
  await sleep(3500);
  const simFrame = () => page.frames().find((f) => f.url().includes("python-simulator.usermbit.org"));
  await simFrame().waitForSelector(".play-button", { timeout: 20000 });
  await simFrame().click(".play-button");
  const running = await page.waitForFunction(() => window.__fakeUnity.lines.some((l) => l.startsWith("sim|st:")), { timeout: 20000 }).then(() => true, () => false);
  check("starter blocks run in the simulator (st: reaches the game)", running);
  await page.click(".cs-monitor-head strong");
  await page.keyboard.down("ArrowRight"); await sleep(900);
  const st = await ev(() => window.__fakeUnity.lines.filter((l) => l.startsWith("sim|st:")).slice(-1)[0]);
  await page.keyboard.up("ArrowRight");
  check("tilting right steers right (700/5 = 140 -> clamped by the game)", st && parseFloat(st.split(":")[1]) >= 100, st);
  const jp0 = await ev(() => window.__fakeUnity.lines.filter((l) => l === "sim|jp").length);
  await page.keyboard.press("a"); await sleep(500);
  check("button A jumps", (await ev(() => window.__fakeUnity.lines.filter((l) => l === "sim|jp").length)) > jp0);
  check("hi:v2 reported", await page.waitForFunction(() => window.__fakeUnity.lines.includes("sim|hi:v2"), { timeout: 3000 }).then(() => true, () => false));

  // ---- Every block in one program
  await ev((state) => { window.__ws = window.MicrobitBlocks; const api = document.querySelector("#cs-blockly"); void api; }, ALL);
  const allPy = await ev((state) => {
    const ws = window.Blockly.getMainWorkspace();
    ws.clear(); window.Blockly.serialization.workspaces.load(state, ws);
    return window.MicrobitBlocks.generate(ws);
  }, ALL);
  fs.writeFileSync(path.join(OUT, "blocks_all.py"), allPy);
  const syn2 = pySyntax(allPy, "all");
  check("all blocks: valid Python", !syn2, syn2);
  check("no module MicroPython lacks (numbers)", !/import numbers|from numbers/.test(allPy));
  for (const want of ["import music", "import random", "(pin_logo.is_touched() if VERSION == \"v2\" else False)",
    "(microphone.sound_level() if VERSION == \"v2\" else 0)", "display.set_pixel(int(", "game_speed = int(msg[4:])", "count = None",
    'print("pu")', 'print("bl")', 'accelerometer.was_gesture("shake")', 'msg == "warn:ped"', 'msg == "turn:-1"', 'print("toon:1")', 'print("toon:0")', 'msg == "boing"',
    'last_press_a = -1000', 'running_time() - last_press_a < 400', 'print("bo")', 'presses_a += 1', 'if presses_a == 3:', 'print("say:party")', 'msg == "party"', 'print("do:chase")', 'hits = (hits if type(hits) in (int, float) else 0) + 1', 'print("do:stars:" + str(max(0, min(5, int(2)))))',
    'broadcasts.append("party")', 'if got == "party":', 'import radio', 'radio.config(group=1)', "radio.send(str('gift!'))", 'got_radio = radio.receive()', 'del broadcasts[:]',
    'held_since_b = -1', 'if held_b >= 1000 and held_done_b < 1000:', 'elif held_since_b >= 0:',
    'print("sur")', 'msg == "chase:1"', 'msg == "inc:C"', 'msg == "busted"', 'wanted_level = int(msg[7:])', 'wanted_level = 0',
    'police_distance = int(msg[4:])', 'police_distance = 9', 'if police_distance < 2:',
    'msg == "air:1"', 'print("trick")', 'msg == "land:trick"', 'print("do:ramp")',
    "count = (count if type(count) in (int, float) else 0) + 1"]) {
    check("all blocks make: " + want, allPy.includes(want));
  }
  const disabledOrphans = await ev(() => window.Blockly.getMainWorkspace().getAllBlocks(false).filter((b) => !b.isEnabled()).length);
  check("no block in the full program is disabled", disabledOrphans === 0, disabledOrphans);
  const warned = await ev(async () => {
    const ws = window.Blockly.getMainWorkspace();
    const p = ws.newBlock("mb_pin_touched"); p.initSvg(); p.render(); p.setFieldValue("0", "PIN");
    await new Promise((r) => setTimeout(r, 400));
    const w = !!p.getIcon("warning");
    p.dispose(); return w;
  });
  check("pin 0 touch + sound shows a warning on the block", warned);

  async function runInSim(py, label) {
    const before = await ev(() => window.__fakeUnity.lines.length);
    // The monitor keeps the last 300 lines, so mark the old rows instead of counting them (indices shift when it trims).
    await ev(() => document.querySelectorAll("#cs-log .cs-line").forEach((r) => { r.dataset.old = "1"; }));
    await ev((code) => { document.querySelector('.cs-tabs button[data-view="python"]').click(); const t = document.getElementById("cs-code"); t.value = code; t.dispatchEvent(new Event("input")); }, py);
    await page.click("#cs-run");
    await sleep(2500);
    await page.click(".cs-monitor-head strong");
    for (const k of ["a", "b"]) { await page.keyboard.press(k); await sleep(200); }
    await sleep(500);
    await page.keyboard.press("a"); await sleep(120); await page.keyboard.press("a"); await sleep(120); await page.keyboard.press("a"); await sleep(300); // 3 quick presses
    await page.keyboard.down("b"); await sleep(1400); await page.keyboard.up("b"); await sleep(400); // a long press of B
    await page.keyboard.down("m"); await page.keyboard.down("l"); await sleep(600); await page.keyboard.up("m"); await page.keyboard.up("l");
    for (const m of ["spd:5", "crash", "gift", "go", "party", "warn:ped", "ko", "ko", "ko", "turn:-1", "spd:rn:car", "wanted:oops", "spd:9"]) { await ev((l) => window.CityScooterShell.sendToDevices("*", l), m); await sleep(350); }
    // police messages; no sur before the police car is close (cop:9 is far)
    for (const m of ["chase:1", "wanted:3", "cop:9", "inc:C"]) { await ev((l) => window.CityScooterShell.sendToDevices("*", l), m); await sleep(350); }
    const surBefore = await ev((n) => window.__fakeUnity.lines.slice(n).filter((l) => l === "sim|sur").length, before);
    for (const m of ["cop:1", "busted", "chase:0"]) { await ev((l) => window.CityScooterShell.sendToDevices("*", l), m); await sleep(350); }
    // ramps and air (v0.3)
    for (const m of ["air:1", "air:0", "land:trick"]) { await ev((l) => window.CityScooterShell.sendToDevices("*", l), m); await sleep(350); }
    await sleep(800);
    const lines = await ev((n) => window.__fakeUnity.lines.slice(n), before);
    const log = await ev(() => [...document.querySelectorAll("#cs-log .cs-line:not([data-old])")].map((r) => r.className + " :: " + r.textContent));
    check(label + ": wanted:3 runs the wanted block with the level", log.some((x) => /wanted 3/.test(x)), log.filter((x) => /wanted/.test(x)).slice(-2).join(" | "));
    check(label + ": inc:C handler dodges", log.some((x) => x.includes("dodge")) && lines.includes("sim|st:100"));
    check(label + ": no surrender while the police car is far", surBefore === 0, surBefore);
    check(label + ": cop:1 makes it surrender (sur)", lines.includes("sim|sur"), lines.filter((l) => !/st:/.test(l)).slice(-6).join(" "));
    check(label + ": busted handler ran", log.some((x) => x.includes("got busted")));
    check(label + ": air:1 makes it do a trick (trick)", lines.includes("sim|trick") && log.some((x) => x.includes("in the air")), lines.filter((l) => !/st:/.test(l)).slice(-6).join(" "));
    check(label + ": land:trick asks for another ramp (do:ramp)", lines.includes("sim|do:ramp") && log.some((x) => x.includes("trick landed")));
    check(label + ": no Python error", !log.some((x) => /cs-l-error/.test(x)), log.filter((x) => /cs-l-error/.test(x)).slice(0, 4).join(" | "));
    check(label + ": steers", lines.some((l) => /^sim\|st:-?\d+$/.test(l)));
    check(label + ": crash and gift handlers ran", log.some((x) => x.includes("got crash")) && log.some((x) => x.includes("got gift")),
      log.filter((x) => !/:: sim(st:|v=)/.test(x)).slice(0, 14).join(" | "));
    check(label + ": warn:ped rings the bell (bl)", lines.includes("sim|bl"), lines.filter((l) => !/st:/.test(l)).slice(-6).join(" "));
    check(label + ": warn:ped turns cartoon mode on (toon:1)", lines.includes("sim|toon:1"));
    check(label + ": 2nd quick press of A boosts (bo)", lines.includes("sim|bo"), lines.filter((l) => !/st:/.test(l)).slice(-10).join(" "));
    check(label + ": holding B for 1 s runs the hold block once", log.filter((x) => x.includes("held B 1s")).length === 1, log.filter((x) => /held B|released/.test(x)).slice(-4).join(" | "));
    check(label + ": letting go of B runs the release block with the hold time", log.some((x) => /released after (1[1-9]\d\d|[2-9]\d\d\d)/.test(x)), log.filter((x) => /released/.test(x)).slice(-2).join(" | "));
    check(label + ": 3 punches that land (ko) start a police chase (do:chase)", lines.includes("sim|do:chase"), lines.filter((l) => /do:|ko/.test(l)).slice(-4).join(" "));
    check(label + ": go makes the board ask the game to say party", lines.includes("sim|say:party"));
    check(label + ": when the game says word party runs", log.some((x) => x.includes("word party")), log.filter((x) => /word/.test(x)).slice(-2).join(" | "));
    check(label + ": gift broadcasts party, both listeners run", log.some((x) => x.includes("party time")) && log.some((x) => x.includes("second listener")), log.filter((x) => /party|listener/.test(x)).slice(-3).join(" | "));
    check(label + ": 3rd quick press runs the 3-times block", log.some((x) => x.includes("presses 3")), log.filter((x) => /presses/.test(x)).slice(-3).join(" | "));
    check(label + ": speed handler ran", log.some((x) => /speed 9/.test(x)), log.filter((x) => /speed/.test(x)).slice(-2).join(" | "));
    return lines;
  }
  const v2 = await runInSim(allPy, "all blocks on V2");
  check("all blocks on V2: hi:v2", v2.includes("sim|hi:v2"));
  const v1Py = allPy.replace("from microbit import microphone", "from microbit import microphone_not_on_v1");
  const v1 = await runInSim(v1Py, "all blocks forced V1");
  check("all blocks forced V1: hi:v1", v1.includes("sim|hi:v1"), v1.filter((l) => l.includes("hi:")).slice(-2));

  // ---- Blocks <-> Python
  await ev(() => document.querySelector('.cs-tabs button[data-view="blocks"]').click());
  await ev(() => { const ws = window.Blockly.getMainWorkspace(); ws.clear(); });
  await ev(() => document.querySelector('.cs-modes button[data-mode="usb"]').click());
  await sleep(300);
  check("code panel follows into USB mode", await ev(() => !!document.querySelector('.cs-pane[data-pane="usb"] #cs-code-section')));
  await ev(() => window.Blockly.getMainWorkspace() && window.Blockly.getMainWorkspace().clear());
  await page.click("#cs-starter"); // confirm dialog auto-accepted -> starter blocks
  await sleep(400);
  await ev(() => document.querySelector('.cs-tabs button[data-view="python"]').click());
  await sleep(200);
  const pyEdited = await ev(() => document.getElementById("cs-code").value);
  check("Python view shows code from the blocks (after the student's own edit it is kept)", /# Made with blocks|st:/.test(pyEdited));
  await ev(() => { const t = document.getElementById("cs-code"); t.value = "# my own python\n" + t.value; t.dispatchEvent(new Event("input")); });
  check("note offers to replace with blocks' code", !(await ev(() => document.getElementById("cs-python-note").hidden)));
  await ev(() => document.querySelector('.cs-tabs button[data-view="blocks"]').click());
  await ev(() => { const ws = window.Blockly.getMainWorkspace(); const b = ws.newBlock("mb_on_start"); b.initSvg(); b.render(); });
  await sleep(400);
  await ev(() => document.querySelector('.cs-tabs button[data-view="python"]').click());
  check("blocks do not overwrite edited Python", (await ev(() => document.getElementById("cs-code").value)).startsWith("# my own python"));
  await page.click("#cs-from-blocks");
  await sleep(200);
  check("Use my blocks' code replaces it", (await ev(() => document.getElementById("cs-code").value)).startsWith("# Made with blocks"));
  await ev(() => document.querySelector('.cs-tabs button[data-view="blocks"]').click());

  // ---- splitter
  const w0 = await ev(() => document.querySelector(".cs-panel").getBoundingClientRect().width);
  const bar = await (await page.$("#cs-splitter")).boundingBox();
  await page.mouse.move(bar.x + 5, bar.y + 100); await page.mouse.down(); await page.mouse.move(bar.x - 120, bar.y + 100, { steps: 5 }); await page.mouse.up();
  const w1 = await ev(() => document.querySelector(".cs-panel").getBoundingClientRect().width);
  check("splitter widens the panel", w1 > w0 + 80, w0 + " -> " + w1);
  await page.screenshot({ path: path.join(OUT, "comic_usb_blocks.png") });

  // ---- persistence across reload
  const savedBlocks = await ev(() => window.Blockly.getMainWorkspace().getAllBlocks(false).length);
  await ev(() => sessionStorage.setItem("keep", "1"));
  await page.reload({ waitUntil: "load" });
  await page.waitForFunction(() => window.MicrobitBlocks && document.querySelector("#cs-blockly .blocklySvg"), { timeout: 15000 }).catch(() => {});
  await sleep(800);
  check("mode, view and blocks restored after reload", (await ev(() => document.body.dataset.mode)) === "usb"
    && (await ev(() => window.Blockly.getMainWorkspace().getAllBlocks(false).length)) === savedBlocks, savedBlocks);

  // ---- 1920x1080 look
  await page.setViewport({ width: 1920, height: 1080 });
  await ev(() => document.querySelector('.cs-modes button[data-mode="sim"]').click());
  await sleep(2500);
  await page.screenshot({ path: path.join(OUT, "comic_sim_1080.png") });

  check("no page errors, no external requests", errors.length === 0, errors.slice(0, 6).join(" | "));
  console.log(failures ? failures + " FAILED" : "ALL PASSED");
  await browser.close();
  server.close();
  process.exit(failures ? 1 : 0);
})().catch((e) => { console.error(e); process.exit(2); });
