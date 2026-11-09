/* CityScooter web shell (Claude, 2026-11-06). Plain ES2020, no dependencies, no CDN.
 *
 * Contract with Unity (docs/bridge.md) — do not change names or formats:
 *   JS -> Unity  SendMessage("DeviceBridge", "OnDeviceLines",  "id|line\nid|line")
 *                SendMessage("DeviceBridge", "OnDeviceStatus", "id|kind|1or0|version")
 *                SendMessage("DeviceBridge", "SetKeyboardEnabled", "1" | "0")
 *   Unity -> JS  CityScooterShell.onUnityReady(protocolVersion)
 *                CityScooterShell.sendToDevices(target, line)   target = device id or "*"
 *
 * Modes: Keyboard (game keys only), micro:bit USB (Web Serial, up to 4 boards: usb1..usb4),
 * Simulator (official MicroPython simulator in an iframe, device id "sim").
 * Only the current mode's devices reach the game. Student-facing text is English.
 */
(() => {
  "use strict";

  const MICROBIT_VID = 0x0d28;
  const SIM_ORIGIN = "https://python-simulator.usermbit.org";
  const SIM_URL = SIM_ORIGIN + "/v/0.1/simulator.html";
  const MAX_BOARDS = 4;
  const MONITOR_MAX = 300;
  const SILENT_MS = 3000;
  const BRAKE_HOLD_MS = 300; // matches ControlMixer.BrakeHold
  const KEY_MODE = "cityscooter.mode";
  const KEY_DRAFT = "cityscooter.draft";
  const KEY_BLOCKS = "cityscooter.blocks";
  const KEY_VIEW = "cityscooter.view";
  const KEY_PANEL = "cityscooter.panelWidth";

  const SILENT_TEXT = "Connected, but this micro:bit is not sending anything. Flash a program that prints, like the starter. "
    + "Code you only ran in the REPL is gone after unplugging.";

  // ------------------------------------------------------------------ helpers
  const $ = (id) => document.getElementById(id);
  const now = () => performance.now();
  const storeGet = (k) => { try { return window.localStorage.getItem(k); } catch (e) { return null; } };
  const storeSet = (k, v) => { try { window.localStorage.setItem(k, v); } catch (e) { /* private window or blocked */ } };
  const clamp = (v, lo, hi) => Math.max(lo, Math.min(hi, v));
  const errText = (e) => (e && (e.message || e.name)) ? String(e.message || e.name) : String(e);

  // ------------------------------------------------------------------ state
  const shell = {
    mode: "keyboard",
    unity: null,
    unityReady: false,
    outbox: [],              // "id|line" waiting for the next frame
    devices: new Map(),      // id -> device
    keyboardEnabled: true,
    keyboardAllowed: true,   // boot option keyboard: false = micro:bit only (lessons): no Keyboard mode, no keys reach the game
    starterUrl: "TemplateData/starter.py",
    starterText: null,
    codeBase: "TemplateData/",                  // where code/ lives
    vendorBase: "TemplateData/vendor/",         // Blockly; copied in from web-vendor/ after the Unity build
    codePacks: ["code/game-cityscooter.js"],    // the game's block pack(s), relative to codeBase
    usbLog: [],              // USB events with times (open retries, glitches, reconnects): CityScooterShell.usbLog
  };

  // ------------------------------------------------------------------ layout
  const LAYOUT = `
<header class="cs-bar">
  <div class="cs-brand">CityScooter</div>
  <nav class="cs-modes" aria-label="How do you want to play?">
    <button type="button" data-mode="keyboard">Keyboard</button>
    <button type="button" data-mode="usb">micro:bit (USB)</button>
    <button type="button" data-mode="sim">Simulator</button>
  </nav>
  <div class="cs-chips" id="cs-chips"></div>
  <div class="cs-spacer"></div>
  <button type="button" class="cs-ghost" id="cs-help-btn">Help</button>
  <button type="button" class="cs-ghost" id="cs-fs-btn">Fullscreen</button>
</header>
<main class="cs-main">
  <section class="cs-game">
    <div class="cs-stage" id="cs-stage">
      <div class="cs-loading" id="cs-loading">
        <div>Loading CityScooter… <b id="cs-progress">0%</b></div>
        <div class="cs-progressbar"><i id="cs-progressbar"></i></div>
      </div>
    </div>
    <section class="cs-monitor" aria-label="Serial monitor">
      <div class="cs-monitor-head">
        <strong>Serial monitor</strong>
        <span class="cs-muted" id="cs-mon-note">What your micro:bit prints shows up here.</span>
        <span class="cs-spacer"></span>
        <button type="button" class="cs-small" id="cs-pause">Pause</button>
        <button type="button" class="cs-small" id="cs-clear">Clear</button>
      </div>
      <div class="cs-stats" id="cs-stats"></div>
      <div class="cs-log" id="cs-log"></div>
    </section>
  </section>
  <div class="cs-splitter" id="cs-splitter" role="separator" aria-orientation="vertical" title="Drag to resize"></div>
  <aside class="cs-panel">
    <div class="cs-pane" data-pane="keyboard">
      <h2>Play with the keyboard</h2>
      <p class="cs-muted">Click the game first so it gets your keys.</p>
      <table class="cs-keys">
        <tr><td><kbd>←</kbd> <kbd>→</kbd></td><td>steer</td></tr>
        <tr><td><kbd>Space</kbd></td><td>jump (and start a ride)</td></tr>
        <tr><td><kbd>S</kbd></td><td>brake</td></tr>
        <tr><td><kbd>F</kbd></td><td>punch boxes, bins and cones out of the way</td></tr>
        <tr><td><kbd>R</kbd></td><td>ring the bell: people step aside</td></tr>
        <tr><td><kbd>E</kbd></td><td>boost for 2 s (then it recharges)</td></tr>
        <tr><td><kbd>T</kbd></td><td>cartoon mode on/off: people you bump go flying</td></tr>
        <tr><td><kbd>S</kbd> (police after you)</td><td>brake to a stop: pull over</td></tr>
      </table>
      <h3>Got a micro:bit?</h3>
      <p>Choose <b>micro:bit (USB)</b> above and connect it.</p>
      <h3>No micro:bit?</h3>
      <p>Choose <b>Simulator</b> to run your own code on a virtual micro:bit.</p>
    </div>
    <div class="cs-pane" data-pane="usb">
      <div class="cs-usb-top">
        <button type="button" class="cs-primary cs-big" id="cs-connect">Connect micro:bit</button>
        <div class="cs-usb-msg" id="cs-usb-msg" hidden></div>
        <div id="cs-boards"></div>
        <ol class="cs-steps cs-steps-row">
          <li>Click <b>Connect micro:bit</b> and pick it in the list.</li>
          <li>Make your code below and click <b>Send to micro:bit</b>. It restarts with your code.</li>
          <li>Press <b>A</b> to ride.</li>
        </ol>
      </div>
      <div class="cs-code-slot"></div>
    </div>
    <div class="cs-pane" data-pane="sim">
      <div class="cs-sim-top">
        <div class="cs-sim-frame">
          <iframe id="cs-sim" title="micro:bit simulator" allow="autoplay"></iframe>
          <div class="cs-sim-hint" id="cs-sim-hint">Click <b>▶</b> on the micro:bit to start</div>
        </div>
        <div class="cs-sim-side">
          <button type="button" class="cs-primary" id="cs-run">Run my code</button>
          <button type="button" id="cs-sim-reset">Reset simulator</button>
          <div class="cs-keycard">
            <div><kbd>←</kbd><kbd>→</kbd> tilt</div>
            <div><kbd>A</kbd><kbd>B</kbd> buttons</div>
            <div>hold <kbd>M</kbd> shout</div>
            <div>hold <kbd>L</kbd> touch logo</div>
          </div>
        </div>
      </div>
      <div class="cs-bots">
        <button type="button" class="cs-small" id="cs-bots" aria-pressed="false"
          title="3 robot riders send p:BOT1:… to your virtual micro:bit by radio, 4 times a second, like real micro:bits in your race">Robot racers</button>
        <button type="button" class="cs-small" id="cs-bots-go" title="Start a race: sends go:…:uwa:2000 to your virtual micro:bit by radio">Send go</button>
        <span class="cs-bots-note" id="cs-bots-note">No micro:bits to race? Turn on robots that race you by radio.</span>
      </div>
      <div class="cs-code-slot"></div>
    </div>
    <section class="cs-code" id="cs-code-section" aria-label="Your code">
      <div class="cs-code-head">
        <div class="cs-tabs" role="group" aria-label="Code view">
          <button type="button" data-view="blocks">Blocks</button>
          <button type="button" data-view="python">Python</button>
        </div>
        <span class="cs-muted cs-saved" id="cs-saved"></span>
        <span class="cs-spacer"></span>
        <button type="button" class="cs-small" id="cs-starter">Starter</button>
        <button type="button" class="cs-small cs-accent" id="cs-download">Download main.py</button>
        <button type="button" class="cs-small cs-primary" id="cs-send">Send to micro:bit</button>
      </div>
      <div class="cs-code-body">
        <div class="cs-blocks-view" id="cs-blocks-view">
          <div class="cs-blockly" id="cs-blockly"><div class="cs-blockly-msg" id="cs-blockly-msg">Loading blocks…</div></div>
          <details class="cs-preview" id="cs-preview-box" open>
            <summary>Python made by your blocks</summary>
            <pre id="cs-preview"></pre>
          </details>
        </div>
        <div class="cs-python-view" id="cs-python-view" hidden>
          <div class="cs-python-note" id="cs-python-note" hidden>
            <span>This Python is yours now — your blocks did not change it.</span>
            <button type="button" class="cs-small" id="cs-from-blocks">Use my blocks' code</button>
          </div>
          <textarea id="cs-code" spellcheck="false" autocomplete="off" autocapitalize="off" wrap="off"
            aria-label="Your MicroPython code"></textarea>
        </div>
      </div>
    </section>
  </aside>
</main>
<div class="cs-overlay" id="cs-help" hidden>
  <div class="cs-dialog" role="dialog" aria-modal="true" aria-labelledby="cs-help-title">
    <button type="button" class="cs-close" id="cs-help-close" aria-label="Close">×</button>
    <h2 id="cs-help-title">Help</h2>
    <h3>How to connect a micro:bit</h3>
    <ol class="cs-steps">
      <li>Plug the micro:bit in with a USB cable. Choose <b>micro:bit (USB)</b>, click <b>Connect micro:bit</b> and pick it in the list.</li>
      <li>Make your code with <b>Blocks</b> or <b>Python</b> and click <b>Send to micro:bit</b>. It restarts with your code
        in a few seconds. Press <b>A</b> to ride.</li>
      <li><b>Send did not work?</b> The micro:bit may not have MicroPython on it yet (a new board, or one used with MakeCode).
        Do it once the long way: click <b>Download main.py</b>; on <b>python.microbit.org</b> click <b>Open…</b>, pick main.py,
        click <b>Send to micro:bit</b>, then <b>Disconnect</b>. After that, Send works here.</li>
    </ol>
    <h3>Blocks and Python</h3>
    <p>Every block program is real MicroPython: open <b>Python made by your blocks</b> to see it. Switch to <b>Python</b> to
      change the code as text — text changes do not go back into blocks.</p>
    <h3>Something is wrong?</h3>
    <ul>
      <li><b>Use Chrome or Edge.</b> Other browsers cannot talk to USB devices.</li>
      <li><b>"Could not open" / board busy:</b> another tab is using it. On python.microbit.org click <b>Disconnect</b>, or close that tab.</li>
      <li><b>The list is empty:</b> plug it in again, or try another cable — some cables only charge.</li>
      <li><b>I changed my code but the micro:bit does the same thing:</b> it keeps running the last code you sent. Click <b>Send to micro:bit</b> again.</li>
      <li><b>Connected but nothing happens:</b> your program must <code>print</code> commands like <code>st:20</code>. Send it again.</li>
      <li><b>Steering is stuck or slow:</b> remove <code>display.scroll</code> from your loop — it pauses your program.</li>
      <li><b>Red lines in the monitor:</b> your program stopped with an error. Read the last red line and its hint.</li>
      <li><b>No micro:bit?</b> Choose <b>Simulator</b>, paste your code, click ▶ on the virtual micro:bit.</li>
    </ul>
  </div>
</div>`;

  // ------------------------------------------------------------------ protocol (mirrors CommandParser.cs)
  const NUMBER = /^[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?$/;

  function classify(line) {
    const s = line.trim();
    const lower = s.toLowerCase();
    if (lower === "jp") return { kind: "jump" };
    if (lower === "br") return { kind: "brake" };
    if (lower === "pu") return { kind: "punch" };
    if (lower === "bl") return { kind: "bell" };
    if (lower === "toon") return { kind: "toon", value: 1 };
    if (lower === "bo") return { kind: "boost" };
    if (lower === "sur") return { kind: "surrender" };
    if (lower === "trick") return { kind: "trick" }; // v0.3 ramps
    const colon = lower.indexOf(":");
    if (colon <= 0) return { kind: "unknown" };
    const key = lower.slice(0, colon).trim();
    const arg = s.slice(colon + 1).trim();
    switch (key) {
      case "zone": return { kind: "gameword", hint: ZONE_WORD_HINT }; // ---- Districts (v0.3): the game sends zone:<id>; from a board it does nothing
      // ---- Race (radio race v1, docs/radio-race.md "实现约定")
      case "race": { // race:NAME:GROUP. Name: letters and digits only, capitals, up to 5 (like the game). Group 0-255.
        const m = /^([^:]*):\s*(\d{1,3})$/.exec(arg);
        const name = m ? m[1].replace(/[^A-Za-z0-9]/g, "").toUpperCase().slice(0, 5) : "";
        const group = m ? parseInt(m[2], 10) : -1;
        return name && group <= 255 ? { kind: "race", name, group } : { kind: "malformed" };
      }
      case "rx": return { kind: "rx", value: arg }; // what the board heard by radio; the game reads p:, f: and go: in it
      case "pos": case "fin": return { kind: "gameword", hint: RACE_WORD_HINT };
      case "p": case "f": case "go": return { kind: "radioword" };
      case "start": { // ---- Districts (v0.3): "start:kingspark" (spaces and capitals dropped, like CommandParser)
        const id = arg.toLowerCase().replace(/[^a-z]/g, "").slice(0, 16);
        return ZONE_NAMES[id] ? { kind: "start", value: id } : { kind: "malformed" };
      }
      case "st":
        if (NUMBER.test(arg)) {
          const v = parseFloat(arg);
          if (isFinite(v)) return { kind: "steer", value: clamp(v, -100, 100) };
        }
        return { kind: "malformed" };
      case "hi": return { kind: "hello", value: arg.toLowerCase() };
      case "echo": return { kind: "echo" };
      case "jp": return { kind: "jump" };
      case "br": return { kind: "brake" };
      case "pu": return { kind: "punch" };
      case "bl": return { kind: "bell" };
      case "bo": return { kind: "boost" };
      case "do": return /^(chase|heli|calm|car|slowcar|roadworks|police|crossing|manhole|ramp|stars\s*:\s*[0-5]|turn\s*:\s*(l|r|left|right|-1|1))$/i.test(arg)
        ? { kind: "trigger", value: arg.toLowerCase() } : { kind: "malformed" };
      case "say": return /^[A-Za-z0-9_!?-]{1,16}$/.test(arg) && !/^(go|crash|gift|ko|boing|tired|arrested|busted|lost|echo|jp|br|pu|bl|bo|toon|say|do|sur|trick|start)$/i.test(arg)
        ? { kind: "say", value: arg } : { kind: "malformed" };
      case "sur": return { kind: "surrender" };
      case "trick": return { kind: "trick" }; // v0.3 ramps: "trick:1" is forgiven like "jp:1"
      case "toon": {
        const v = arg.toLowerCase();
        if (v === "1" || v === "on" || v === "true") return { kind: "toon", value: 1 };
        if (v === "0" || v === "off" || v === "false") return { kind: "toon", value: 0 };
        return { kind: "malformed" };
      }
      default: return { kind: "unknown" };
    }
  }

  const ERROR_HINTS = [
    [/IndentationError/, "line up the spaces at the start of each line in the block"],
    [/SyntaxError/, "check brackets, quotes and the colon at the end of if / while / def lines"],
    [/NameError/, "check the spelling of a variable name — Python cares about capital letters"],
    [/TypeError/, "you mixed text and numbers, e.g. use \"st:\" + str(steer)"],
    [/AttributeError/, "check the spelling after the dot, e.g. button_a.was_pressed()"],
    [/ImportError|no module named/i, "that module is not on this micro:bit (a V1 has fewer features)"],
    [/out of bounds/i, "LED x and y go from 0 to 4"],
    [/music mode/i, "sound uses pin 0 — touch pin 1 or 2 instead"],
    [/not a string/, "a micro:bit in your radio group sent bytes, not text (maybe a MakeCode program): use another group"],
    [/ValueError/, "a value had the wrong form, e.g. int(\"abc\")"],
    [/MemoryError/, "your program used too much memory — keep lists and strings small"],
    [/ZeroDivisionError/, "you divided by zero"],
    [/IndexError/, "a list position that does not exist"],
    [/KeyboardInterrupt/, "the program was stopped"],
    [/Traceback/, "your program stopped with an error — read the last red line"],
  ];
  const errorHint = (line) => {
    for (const [re, hint] of ERROR_HINTS) if (re.test(line)) return hint;
    return "";
  };
  const looksLikeError = (line) => /Traceback|Error\b|^\s+File "/.test(line);

  // ---- Districts (v0.3): the monitor names the part of Perth a zone:<id> message means (route order).
  const ZONE_NAMES = { uwa: "UWA", matilda: "Matilda Bay", kingspark: "Kings Park", westperth: "West Perth",
    cbd: "Perth CBD", quay: "Elizabeth Quay", mountsbay: "Mounts Bay Road",
    // v0.4 south line
    canningbridge: "Canning Bridge", murdoch: "Murdoch", cockburn: "Cockburn Central", aubingrove: "Aubin Grove",
    kwinana: "Kwinana", wellard: "Wellard", rockingham: "Rockingham", warnbro: "Warnbro" };
  const zoneHint = (line) => {
    const m = /^zone:([a-z]+)$/.exec(line);
    return m ? "entering " + (ZONE_NAMES[m[1]] || m[1]) : "";
  };
  const ZONE_WORD_HINT = "zone: is a message FROM the game (you rode into a new part of Perth). Use \"when entering\" to react to it.";
  // ---- end Districts (v0.3)

  // ---- Race (radio race v1): hints for race messages printed the wrong way round
  const RACE_WORD_HINT = "pos: and fin: are messages FROM the game (where you are in a race, your time). \"send my position by radio\" sends them on as p: and f:";
  const RADIO_WORD_HINT = "p:, f: and go: travel by radio: use radio.send(). The game reads what your micro:bit heard as rx:… (\"pass radio messages to the game\")";
  const RACE_HINT = "race: needs your name and race number, e.g. race:AMY:12 (name: 1-5 letters or digits; race number = radio group, 0-255)";

  // ------------------------------------------------------------------ devices
  function getDevice(id, kind) {
    let d = shell.devices.get(id);
    if (!d) {
      d = {
        id, kind, ver: "", connected: false, lastLineAt: 0, connectedAt: 0, silent: false, rxBuf: "", rawRx: null, sending: false,
        steer: 0, stTimes: [], jpTimes: [], brakeUntil: 0, jpBuckets: new Array(10).fill(0), jpBucketAt: 0,
        statRow: null, card: null,
      };
      shell.devices.set(id, d);
    }
    return d;
  }

  const isActive = (d) => d.connected && ((d.kind === "usb" && shell.mode === "usb") || (d.kind === "sim" && shell.mode === "sim"));

  function sendStatus(d) {
    callUnity("OnDeviceStatus", [d.id, d.kind, isActive(d) ? "1" : "0", d.ver || ""].join("|"));
  }

  // A new program is about to run on this board (Send, or Run in the simulator) while it stays connected: tell the game
  // the old program has gone, like a re-plugged board, so the events it set up for the start of every ride
  // ("on start" -> do:...) go with it. The caller sends the "connected" status again straight after.
  function sendGone(d) {
    if (isActive(d)) callUnity("OnDeviceStatus", [d.id, d.kind, "0", d.ver || ""].join("|"));
  }

  function onDeviceLine(d, line) {
    const t = now();
    d.lastLineAt = t;
    if (d.silent) { d.silent = false; renderBoards(); }
    const c = classify(line);
    switch (c.kind) {
      case "steer":
        d.steer = c.value;
        d.stTimes.push(t);
        break;
      case "jump":
        d.jpTimes.push(t);
        bumpJpBucket(d, t);
        break;
      case "brake":
        d.brakeUntil = t + BRAKE_HOLD_MS;
        break;
      case "punch":
        d.punchUntil = t + 300;
        break;
      case "bell":
        d.bellUntil = t + 300;
        break;
      case "toon":
        d.toon = c.value === 1;
        break;
      case "boost":
        d.boostUntil = t + 300;
        break;
      case "start": // ---- Districts (v0.3): programs send it every second, so the monitor notes only a change
        if (d.start !== c.value) {
          d.start = c.value;
          logLine("game", d.id, "rides start in " + ZONE_NAMES[c.value], "");
        }
        break;
      case "gameword": // ---- Districts (v0.3), race
        logLine("print", d.id, line, c.hint);
        break;
      // ---- Race (radio race v1). race: comes every second (with hi:), so the monitor notes only a change; rx: lines
      // are counted and shown once a second ("radio: 14 messages/s from AMY, BEN, CHLOE"), or they would flood it.
      case "race":
        if (d.race !== c.name + ":" + c.group) {
          d.race = c.name + ":" + c.group;
          logLine("game", d.id, "joins race " + c.group + " as " + c.name, "");
        }
        break;
      case "rx":
        noteRx(d, c.value);
        break;
      case "radioword":
        logLine("print", d.id, line, RADIO_WORD_HINT);
        break;
      case "say":
        break; // the game repeats it to every board; the monitor shows that as a game line
      case "trigger":
        logLine("game", d.id, "asks the game for: " + c.value, "");
        break;
      case "surrender":
        d.surUntil = t + 600;
        break;
      case "trick": // v0.3 ramps
        d.trickUntil = t + 600;
        break;
      case "hello":
        if (d.ver !== c.value) {
          d.ver = c.value;
          sendStatus(d);
          renderChips();
          renderBoards();
        }
        break;
      case "malformed":
        logLine("warn", d.id, line, /^\s*toon/i.test(line) ? "toon: needs 1 (on) or 0 (off), e.g. toon:1"
          : /^\s*say/i.test(line) ? "say: one word of your own (letters, digits, _ - ! ?, up to 16), not a game word like crash"
          : /^\s*start/i.test(line) ? "start: a district id, e.g. start:kingspark or start:rockingham (" + Object.keys(ZONE_NAMES).join(", ") + ")"
          : /^\s*do/i.test(line) ? "do: one of chase, heli, calm, stars:0-5, turn:L, turn:R, car, slowcar, roadworks, police, crossing, manhole, ramp"
          : /^\s*race/i.test(line) ? RACE_HINT
          : "st: needs a number, e.g. st:-35");
        break;
      default: {
        const err = looksLikeError(line);
        logLine(err ? "error" : "print", d.id, line, err ? errorHint(line) : "");
      }
    }
    if (isActive(d)) {
      shell.outbox.push(d.id + "|" + line);
      if (shell.outbox.length > 400) shell.outbox.splice(0, shell.outbox.length - 400);
    }
  }

  // ---- Race (radio race v1): radio traffic in the monitor, once a second instead of one line per message.
  // rx: lines a board passes to the game (from all the riders in its group, about 4 a second each), pos:/fin: the game
  // sends (4 a second), and in the simulator what the student's own program sent by radio.
  const radioCount = () => ({ n: 0, names: [], other: 0, example: "" });
  const raceOut = { n: 0, last: "" };   // pos:/fin: from the game this second
  const seenRace = { go: "", fin: {} }; // go: and f: are repeated on purpose: noted once

  function noteRx(d, payload) {
    const r = d.rx || (d.rx = radioCount());
    r.n++;
    const m = /^(p|f):([A-Za-z0-9]{1,5}):/.exec(payload);
    if (m) {
      if (!r.names.includes(m[2]) && r.names.length < 12) r.names.push(m[2]);
      const fin = /^f:([A-Za-z0-9]{1,5}):(\d+(\.\d+)?)$/.exec(payload);
      if (fin && seenRace.fin[d.id + fin[1]] !== fin[2]) {
        seenRace.fin[d.id + fin[1]] = fin[2];
        logLine("radio", d.id, "radio: " + fin[1] + " finished in " + fin[2] + " s", "");
      }
      return;
    }
    const go = /^go:(\d{1,5}):([a-z]*):(\d+)$/.exec(payload);
    if (go) {
      if (seenRace.go !== d.id + payload) {
        seenRace.go = d.id + payload;
        logLine("radio", d.id, "radio: go! A race from " + (ZONE_NAMES[go[2]] || go[2]) + ", " + go[3] + " m (road number " + go[1] + ")", "");
      }
      return;
    }
    r.other++;
    r.example = payload.slice(0, 40);
  }

  function noteRadioSent(d, text) {
    const r = d.tx || (d.tx = { n: 0, last: "" });
    r.n++;
    r.last = text.slice(0, 40);
  }

  function radioSummary() {
    const plural = (n, w) => n + " " + w + (n === 1 ? "" : "s");
    for (const d of shell.devices.values()) {
      const r = d.rx;
      if (r && r.n) {
        let text = "radio: " + plural(r.n, "message") + "/s";
        if (r.names.length) text += " from " + r.names.join(", ");
        if (r.other) text += (r.names.length ? " · " + plural(r.other, "other") : "") + ", e.g. \"" + r.example + "\"";
        logLine("radio", d.id, text, r.other ? "the game only reads p:, f: and go: in rx:" : "");
        d.rx = radioCount();
      }
      if (d.tx && d.tx.n) {
        logLine("radio", d.id, "radio sent: " + plural(d.tx.n, "message") + "/s, last " + d.tx.last, "");
        d.tx = null;
      }
    }
    if (raceOut.n) {
      logLine("game", "game", raceOut.last, raceOut.n + " this second (shown once a second)");
      raceOut.n = 0;
    }
  }

  // jp per second for the last 10 seconds (bar chart in the monitor).
  function advanceJpBuckets(d, t) {
    const sec = Math.floor(t / 1000);
    if (!d.jpBucketAt || sec - d.jpBucketAt > 10) { d.jpBuckets.fill(0); d.jpBucketAt = sec; }
    while (d.jpBucketAt < sec) { d.jpBuckets.shift(); d.jpBuckets.push(0); d.jpBucketAt++; }
  }

  function bumpJpBucket(d, t) {
    advanceJpBuckets(d, t);
    d.jpBuckets[9]++;
  }

  // ------------------------------------------------------------------ Unity
  const pendingCalls = [];

  function callUnity(method, arg) {
    if (!shell.unity || !shell.unityReady) {
      // Keep only the latest status per device / keyboard flag until DeviceBridge is ready.
      const key = method === "OnDeviceStatus" ? method + arg.split("|")[0] : method;
      const i = pendingCalls.findIndex((p) => p.key === key);
      if (i >= 0) pendingCalls.splice(i, 1);
      pendingCalls.push({ key, method, arg });
      return;
    }
    try { shell.unity.SendMessage("DeviceBridge", method, arg); } catch (e) { console.warn("[CityScooter] SendMessage failed", e); }
  }

  function pump() {
    if (shell.unity && shell.unityReady && shell.outbox.length) {
      const batch = shell.outbox.splice(0, shell.outbox.length).join("\n");
      try { shell.unity.SendMessage("DeviceBridge", "OnDeviceLines", batch); } catch (e) { console.warn("[CityScooter] SendMessage failed", e); }
    }
    flushLog();
    requestAnimationFrame(pump);
  }

  // ------------------------------------------------------------------ serial monitor
  const monitor = { paused: false, pending: [], skipped: 0 };

  function logLine(cls, dev, text, hint) {
    monitor.pending.push({ cls, dev, text, hint });
    if (monitor.pending.length > MONITOR_MAX) {
      monitor.skipped += monitor.pending.length - MONITOR_MAX;
      monitor.pending.splice(0, monitor.pending.length - MONITOR_MAX);
    }
  }

  function flushLog() {
    if (monitor.paused || !monitor.pending.length) return;
    const log = $("cs-log");
    const stick = log.scrollTop + log.clientHeight >= log.scrollHeight - 4;
    const frag = document.createDocumentFragment();
    for (const e of monitor.pending) {
      const row = document.createElement("div");
      row.className = "cs-line cs-l-" + e.cls;
      if (e.dev) {
        const s = document.createElement("span");
        s.className = "cs-dev";
        s.textContent = e.dev;
        row.appendChild(s);
      }
      const t = document.createElement("span");
      t.textContent = e.text;
      row.appendChild(t);
      if (e.hint) {
        const h = document.createElement("span");
        h.className = "cs-hint";
        h.textContent = "← " + e.hint;
        row.appendChild(h);
      }
      frag.appendChild(row);
    }
    monitor.pending.length = 0;
    log.appendChild(frag);
    while (log.childElementCount > MONITOR_MAX) log.firstElementChild.remove();
    if (stick) log.scrollTop = log.scrollHeight;
  }

  function setPaused(p) {
    monitor.paused = p;
    $("cs-pause").textContent = p ? "Resume" : "Pause";
    $("cs-pause").classList.toggle("cs-on", p);
    $("cs-mon-note").textContent = p ? "Paused — new lines are kept until you resume." : "What your micro:bit prints shows up here.";
  }

  function renderStats() {
    const box = $("cs-stats");
    const t = now();
    const shown = [...shell.devices.values()].filter((d) => d.connected);
    for (const d of shell.devices.values()) {
      if (!shown.includes(d) && d.statRow) { d.statRow.remove(); d.statRow = null; }
    }
    if (!shown.length) {
      box.textContent = shell.mode === "keyboard" ? "Keyboard mode: no micro:bit in use." : "No micro:bit sending yet.";
      box.classList.add("cs-empty");
      return;
    }
    if (box.classList.contains("cs-empty")) { box.textContent = ""; box.classList.remove("cs-empty"); }
    for (const d of shown) {
      while (d.stTimes.length && t - d.stTimes[0] > 1000) d.stTimes.shift();
      while (d.jpTimes.length && t - d.jpTimes[0] > 1000) d.jpTimes.shift();
      advanceJpBuckets(d, t);
      if (!d.statRow) d.statRow = makeStatRow(d, box);
      const r = d.statRow;
      r.classList.toggle("cs-inactive", !isActive(d));
      r.querySelector(".cs-sid").textContent = d.id + (d.ver ? " " + d.ver.toUpperCase() : "");
      const pct = (d.steer + 100) / 2;
      r.querySelector(".cs-steer-fill").style.left = Math.min(50, pct) + "%";
      r.querySelector(".cs-steer-fill").style.width = Math.abs(pct - 50) + "%";
      r.querySelector(".cs-steer-val").textContent = (d.steer > 0 ? "+" : "") + Math.round(d.steer);
      r.querySelector(".cs-strate").textContent = d.stTimes.length + " st/s";
      r.querySelector(".cs-jp-big").textContent = d.jpTimes.length;
      r.querySelector(".cs-br").classList.toggle("cs-on", t < d.brakeUntil);
      r.querySelector(".cs-pu").classList.toggle("cs-on", t < (d.punchUntil || 0));
      r.querySelector(".cs-bl").classList.toggle("cs-on", t < (d.bellUntil || 0));
      r.querySelector(".cs-toon").classList.toggle("cs-on", !!d.toon);
      r.querySelector(".cs-bo").classList.toggle("cs-on", t < (d.boostUntil || 0));
      r.querySelector(".cs-sur").classList.toggle("cs-on", t < (d.surUntil || 0));
      r.querySelector(".cs-trick").classList.toggle("cs-on", t < (d.trickUntil || 0)); // v0.3 ramps
      const max = Math.max(1, ...d.jpBuckets);
      const bars = r.querySelectorAll(".cs-jpbars i");
      d.jpBuckets.forEach((n, i) => { bars[i].style.height = Math.round((n / max) * 100) + "%"; bars[i].title = n + " jp"; });
    }
  }

  function makeStatRow(d, box) {
    const r = document.createElement("div");
    r.className = "cs-stat";
    r.innerHTML = `<span class="cs-sid"></span>
      <span class="cs-steer" title="Steering from st: (-100 left … +100 right)"><span class="cs-steer-fill"></span><span class="cs-steer-mid"></span></span>
      <span class="cs-steer-val"></span><span class="cs-strate cs-muted"></span>
      <span class="cs-jp" title="jp messages in the last second"><span class="cs-jp-big">0</span><span class="cs-muted">jp in 1 s</span></span>
      <span class="cs-jpbars" title="jp per second, last 10 seconds">${"<i></i>".repeat(10)}</span>
      <span class="cs-acts"><span class="cs-brake cs-br">BRAKE</span><span class="cs-brake cs-pu" title="pu: punch">PUNCH</span><span class="cs-brake cs-bl" title="bl: bell">BELL</span><span class="cs-brake cs-bo" title="bo: boost">BOOST</span><span class="cs-brake cs-toon" title="toon:1 / toon:0: cartoon mode">TOON</span><span class="cs-brake cs-sur" title="sur: surrender to the police (pull over)">SUR</span><span class="cs-brake cs-trick" title="trick: a spin or flip while flying off a ramp">TRICK</span></span>`;
    box.appendChild(r);
    return r;
  }

  // ------------------------------------------------------------------ status chips and mode
  function renderChips() {
    const box = $("cs-chips");
    box.textContent = "";
    for (const d of shell.devices.values()) {
      if (d.kind === "sim" && shell.mode !== "sim" && !d.connected) continue;
      const c = document.createElement("span");
      c.className = "cs-chip " + (d.connected ? (isActive(d) ? "cs-ok" : "cs-idle") : "cs-off");
      const label = d.kind === "sim" ? "Simulator" : d.id;
      const off = d.kind === "sim" ? (sim.activated ? " (stopped)" : " (click ▶)") : " (unplugged)";
      c.textContent = label + (d.ver ? " " + d.ver.toUpperCase() : "") + (d.connected ? (isActive(d) ? "" : " (not in use)") : off);
      if (d.silent) { c.classList.add("cs-warn"); c.textContent += " — silent"; }
      box.appendChild(c);
    }
  }

  function setMode(mode, user) {
    if (!["keyboard", "usb", "sim"].includes(mode)) mode = shell.keyboardAllowed ? "keyboard" : "usb";
    if (mode === "keyboard" && !shell.keyboardAllowed) mode = "usb"; // e.g. remembered from before the keyboard was turned off
    const prev = shell.mode;
    shell.mode = mode;
    document.body.dataset.mode = mode;
    document.querySelectorAll(".cs-modes button").forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.mode === mode)));
    document.querySelectorAll(".cs-pane").forEach((p) => { p.hidden = p.dataset.pane !== mode; });
    if (user) storeSet(KEY_MODE, mode);

    // In the simulator the arrow keys tilt the virtual micro:bit instead; micro:bit-only pages never pass keys to the game.
    const kb = shell.keyboardAllowed && mode !== "sim";
    if (kb !== shell.keyboardEnabled || prev === mode) {
      shell.keyboardEnabled = kb;
      callUnity("SetKeyboardEnabled", kb ? "1" : "0");
    }
    if (mode === "sim") simEnter();
    else if (prev === "sim") simLeave();
    placeCode();
    for (const d of shell.devices.values()) sendStatus(d);
    shell.outbox.length = 0;
    renderChips();
    renderBoards();
    renderStats();
  }

  // ------------------------------------------------------------------ USB (Web Serial)
  let nextUsb = 1;

  function usbMsg(text, cls) {
    const m = $("cs-usb-msg");
    m.hidden = !text;
    m.textContent = text || "";
    m.className = "cs-usb-msg " + (cls || "");
  }

  function boardFor(port) {
    for (const d of shell.devices.values()) if (d.kind === "usb" && d.port === port) return d;
    // A re-plugged board can come back as a new port object: reuse the first unplugged id.
    for (const d of shell.devices.values()) if (d.kind === "usb" && !d.connected && !d.opening) { d.port = port; return d; }
    const usbCount = [...shell.devices.values()].filter((d) => d.kind === "usb").length;
    if (usbCount >= MAX_BOARDS) return null;
    const d = getDevice("usb" + nextUsb++, "usb");
    d.port = port;
    return d;
  }

  // Opening fails for a moment right after a page refresh or a re-plug / re-flash: Windows has not let go of the port
  // yet, or is still loading the USB driver (slower on a V1). Lesson 1 (11/9): V1 boards needed several refreshes.
  // So try again for about 6 s before giving up. Delays before each try:
  const OPEN_RETRY_MS = [0, 250, 500, 1000, 1500, 2500];
  // Chrome's default read buffer is only 255 bytes; while Unity loads or a frame is slow the page reads late and it
  // overflows (a "buffer overrun" read error). 8 KB is half a minute of a board printing st: 20 times a second.
  const USB_BUFFER = 8192;
  const usbErr = (e) => (e && e.name && e.name !== "Error" ? e.name + ": " : "") + errText(e);

  // USB events in the monitor (and in CityScooterShell.usbLog for the teacher): why a board dropped out.
  function usbLog(d, text) {
    const entry = new Date().toLocaleTimeString() + " " + d.id + " " + text;
    shell.usbLog.push(entry);
    if (shell.usbLog.length > 100) shell.usbLog.shift();
    logLine("warn", d.id, "USB: " + text, "");
  }

  async function openBoard(d) {
    if (d.connected || d.opening) return;
    d.opening = true;
    d.gone = false;
    const port = d.port;
    let failed = null;
    for (let i = 0; i < OPEN_RETRY_MS.length; i++) {
      if (OPEN_RETRY_MS[i]) await sleepMs(OPEN_RETRY_MS[i]);
      if (d.gone || d.port !== port) { d.opening = false; renderBoards(); return; } // unplugged meanwhile
      try {
        await port.open({ baudRate: 115200, bufferSize: USB_BUFFER });
        failed = null;
        break;
      } catch (e) {
        if (/already open/i.test(errText(e))) { failed = null; break; }
        failed = e;
        if (i === 0) usbMsg("Connecting to the micro:bit…", "cs-warn");
        usbLog(d, "could not open the port (" + usbErr(e) + ")" + (i + 1 < OPEN_RETRY_MS.length ? ", trying again" : ", gave up"));
      }
    }
    d.opening = false;
    if (failed) {
      usbMsg("Could not open the micro:bit. Another tab may be using this board, e.g. python.microbit.org — click Disconnect there "
        + "(or close that tab), then click Connect micro:bit again. Still stuck: unplug it, count to 3, plug it back in.", "cs-bad");
      renderBoards();
      return;
    }
    if (d.reconnects) usbLog(d, "connected again");
    d.connected = true;
    d.connectedAt = now();
    d.lastLineAt = 0;
    d.silent = false;
    usbMsg("");
    if (shell.mode === "keyboard") setMode("usb", false);
    sendStatus(d);
    renderChips();
    renderBoards();
    readLoop(d);
  }

  // Bytes from a board: lines for the game, or raw text for "Send to micro:bit" while it talks to the REPL.
  function feed(d, text) {
    if (d.rawRx) { d.rawRx(text); return; }
    d.rxBuf += text;
    let i;
    while ((i = d.rxBuf.indexOf("\n")) >= 0) {
      const line = d.rxBuf.slice(0, i).replace(/\r$/, "");
      d.rxBuf = d.rxBuf.slice(i + 1);
      if (line.length) onDeviceLine(d, line);
    }
    if (d.rxBuf.length > 1000) d.rxBuf = "";
  }

  async function readLoop(d) {
    const decoder = new TextDecoder();
    const port = d.port;
    d.rxBuf = "";
    while (d.connected && d.port === port && port.readable) {
      const reader = port.readable.getReader();
      d.reader = reader;
      let ended = false;
      try {
        for (;;) {
          const { value, done } = await reader.read();
          if (done) { ended = true; break; }
          feed(d, decoder.decode(value, { stream: true }));
        }
      } catch (e) {
        // A glitch (buffer overrun, framing, break): Chrome gives the port a new readable and the loop carries on.
        // Unplugged: readable becomes null and the disconnect event handles the rest.
        if (d.connected && port.readable) noteGlitch(d, e);
      } finally {
        try { reader.releaseLock(); } catch (e) { /* already released */ }
        d.reader = null;
      }
      if (ended && d.connected) await sleepMs(50); // never spin on a stream that keeps ending
    }
    // The stream died but no disconnect event came (the board is still plugged in): close the port and open it again,
    // instead of showing a connected board that never sends anything until the page is refreshed.
    if (d.connected && d.port === port && !d.gone) {
      d.connected = false;
      d.reconnects = (d.reconnects || 0) + 1;
      usbLog(d, "the connection broke without an unplug: reconnecting");
      sendStatus(d);
      renderChips();
      renderBoards();
      try { await port.close(); } catch (e) { /* already closed */ }
      openBoard(d);
    }
  }

  // Read glitches are normal now and then (the page was busy); many of them point at a cable, hub or old firmware.
  function noteGlitch(d, e) {
    const t = now();
    d.glitches = (d.glitches || 0) + 1;
    if (!d.glitchLoggedAt || t - d.glitchLoggedAt > 10000) {
      d.glitchLoggedAt = t;
      usbLog(d, "read glitch (" + usbErr(e) + "), still connected" + (d.glitches > 1 ? " — " + d.glitches + " so far" : ""));
    }
  }

  // Writes go one after another (a WritableStream has one writer at a time). Rejects if the board is gone.
  function writeBytes(d, bytes) {
    const p = (d.chain || Promise.resolve()).then(async () => {
      if (!d.connected || !d.port.writable) throw new Error("unplugged");
      const w = d.port.writable.getWriter();
      try { await w.write(bytes); } finally { w.releaseLock(); }
    });
    d.chain = p.catch(() => { /* reported to the caller */ });
    return p;
  }

  function writeBoard(d, line) {
    // While sending code the board is in its REPL: a game message there would be typed into it.
    if (!d.connected || !d.port.writable || d.sending) return false;
    writeBytes(d, new TextEncoder().encode(line + "\n")).catch(() => { /* board gone */ });
    return true;
  }

  function addPort(port) {
    const d = boardFor(port);
    if (!d) { usbMsg("Up to " + MAX_BOARDS + " micro:bits can be connected at once.", "cs-bad"); return; }
    openBoard(d);
  }

  async function connectClicked() {
    if (!("serial" in navigator)) {
      usbMsg("This browser cannot talk to USB devices. Please use Chrome or Edge.", "cs-bad");
      return;
    }
    try {
      addPort(await navigator.serial.requestPort({ filters: [{ usbVendorId: MICROBIT_VID }] }));
    } catch (e) {
      if (e && e.name === "NotFoundError") {
        usbMsg("No micro:bit chosen. If the list was empty: plug it in again, or try another cable — some cables only charge.", "cs-warn");
      } else if (e && e.name === "SecurityError") {
        usbMsg("USB access is blocked on this computer or page. Ask your teacher.", "cs-bad");
      } else {
        usbMsg("Connect failed: " + errText(e), "cs-bad");
      }
    }
  }

  function initSerial() {
    if (!("serial" in navigator)) return;
    navigator.serial.addEventListener("connect", (e) => addPort(e.target)); // re-plug / re-flash
    navigator.serial.addEventListener("disconnect", (e) => {
      for (const d of shell.devices.values()) {
        if (d.kind !== "usb" || d.port !== e.target) continue;
        if (d.connected) usbLog(d, "unplugged (or restarting after a flash)");
        d.connected = false;
        d.gone = true;
        d.silent = false;
        // cancel() rejects when the stream already errored (it has, after an unplug): swallow it.
        if (d.reader) d.reader.cancel().catch(() => { /* gone */ });
        sendStatus(d);
      }
      renderChips();
      renderBoards();
    });
    navigator.serial.getPorts().then((ports) => ports.forEach(addPort)).catch(() => { /* not allowed here */ });
    // Let go of the boards when the page goes away, so the refreshed page can open them straight away.
    window.addEventListener("pagehide", () => {
      for (const d of shell.devices.values()) {
        if (d.kind !== "usb" || !d.port || !d.connected) continue;
        d.connected = false;
        const port = d.port;
        Promise.resolve(d.reader ? d.reader.cancel() : null).catch(() => { /* gone */ })
          .then(() => port.close()).catch(() => { /* best effort while unloading */ });
      }
    });
  }

  function renderBoards() {
    const box = $("cs-boards");
    if (!box) return;
    box.textContent = "";
    const boards = [...shell.devices.values()].filter((d) => d.kind === "usb");
    const several = boards.filter((d) => d.connected).length > 1;
    for (const d of boards) {
      const c = document.createElement("div");
      c.className = "cs-board " + (d.connected ? "cs-ok" : "cs-off");
      const title = document.createElement("div");
      title.className = "cs-board-title";
      title.textContent = d.id + (d.ver ? " · " + d.ver.toUpperCase() : "") + " — "
        + (d.connected ? (isActive(d) ? "connected" : "connected (switch to micro:bit (USB) to use it)") : "unplugged — plug it back in");
      if (several && d.connected) {
        const b = document.createElement("button");
        b.type = "button";
        b.className = "cs-small cs-primary cs-board-send";
        b.textContent = "Send my code here";
        b.disabled = d.sending;
        b.addEventListener("click", () => sendClicked(d));
        title.appendChild(b);
      }
      c.appendChild(title);
      if (d.sending) {
        const s = document.createElement("div");
        s.className = "cs-board-sending";
        s.textContent = "Sending your code… " + d.sendPct + "% — keep it plugged in";
        c.appendChild(s);
      } else if (d.silent) {
        const w = document.createElement("div");
        w.className = "cs-board-warn";
        w.textContent = SILENT_TEXT;
        c.appendChild(w);
      }
      box.appendChild(c);
    }
    $("cs-connect").textContent = boards.some((d) => d.connected) ? "Connect another micro:bit" : "Connect micro:bit";
    const busy = boards.find((d) => d.sending);
    $("cs-send").disabled = !!busy;
    $("cs-send").textContent = busy ? "Sending… " + busy.sendPct + "%" : "Send to micro:bit";
  }

  function checkSilent() {
    const t = now();
    let changed = false;
    for (const d of shell.devices.values()) {
      if (!d.connected || d.sending) continue;
      const since = d.lastLineAt || d.connectedAt;
      const silent = t - since > SILENT_MS;
      if (silent && !d.silent) {
        if (d.kind === "usb") logLine("warn", d.id, "No data from this micro:bit for 3 seconds.", "flash a program that prints, like the starter");
        else logLine("warn", d.id, "Your code is running but prints nothing.", "the game needs lines like st:20 or jp — use print()");
      }
      if (silent !== d.silent) { d.silent = silent; changed = true; }
    }
    if (changed) { renderChips(); renderBoards(); }
  }

  // ------------------------------------------------------------------ Send to micro:bit (main.py over the serial REPL)
  // A micro:bit that already has MicroPython (flashed once from python.microbit.org) takes a new main.py over the USB
  // serial port we already have open: stop the program (Ctrl-C), raw REPL (Ctrl-A), write the file in small pieces,
  // check its length and byte sum, reset. The same steps as microfs / Mu; 32-byte writes with short pauses so a V1's
  // small serial buffer keeps up. No firmware is flashed, so a board without MicroPython gets the "long way" message.
  const SEND_PIECE = 64;      // file bytes per f.write()
  const SEND_BURST = 32;      // serial bytes per write
  const SEND_GAP_MS = 10;
  const NO_REPL = "This micro:bit did not answer. It probably does not have MicroPython on it yet (a new board, or one used "
    + "with MakeCode). Do it once the long way: click Download main.py; on python.microbit.org click Open…, pick main.py, "
    + "Send to micro:bit, then Disconnect. After that, Send works here.";
  const sleepMs = (ms) => new Promise((r) => setTimeout(r, ms));

  class SendError extends Error {} // message is shown to the student

  function pyBytes(bytes) {
    let s = "b'";
    for (const c of bytes) {
      if (c === 0x5c) s += "\\\\";
      else if (c === 0x27) s += "\\'";
      else if (c >= 0x20 && c < 0x7f) s += String.fromCharCode(c);
      else s += "\\x" + c.toString(16).padStart(2, "0");
    }
    return s + "'";
  }

  function rawRepl(d) {
    const enc = new TextEncoder();
    const rx = { text: "" };
    d.rawRx = (t) => { rx.text += t; };
    const unplugged = () => { throw new SendError("The micro:bit was unplugged while sending. Plug it back in and click Send again."); };
    const write = (bytes) => writeBytes(d, bytes).catch(unplugged);
    const send = (str) => write(enc.encode(str));
    async function sendSlow(str) {
      const b = enc.encode(str);
      for (let i = 0; i < b.length; i += SEND_BURST) {
        await write(b.subarray(i, i + SEND_BURST));
        await sleepMs(SEND_GAP_MS);
      }
    }
    async function waitFor(test, ms) {
      const end = now() + ms;
      for (;;) {
        if (!d.connected) unplugged();
        const r = test(rx.text);
        if (r) return r;
        if (now() > end) return null;
        await sleepMs(10);
      }
    }
    return {
      rx, send, sendSlow,
      async enter() {
        for (let attempt = 0; attempt < 3; attempt++) {
          await send("\x03"); await sleepMs(80); await send("\x03"); await sleepMs(250); // stop the running program
          rx.text = "";
          await send("\x01");
          if (await waitFor((t) => t.includes("raw REPL; CTRL-B to exit\r\n>"), 1500)) return;
        }
        throw new SendError(NO_REPL);
      },
      // Run Python in the raw REPL: "OK" <stdout> \x04 <stderr> \x04 ">".
      async exec(cmd) {
        rx.text = "";
        await sendSlow(cmd);
        await send("\x04");
        const r = await waitFor((t) => {
          const ok = t.indexOf("OK"); if (ok < 0) return null;
          const a = t.indexOf("\x04", ok + 2); if (a < 0) return null;
          const b = t.indexOf("\x04", a + 1); if (b < 0 || t.indexOf(">", b + 1) < 0) return null;
          return { out: t.slice(ok + 2, a), err: t.slice(a + 1, b) };
        }, 5000);
        if (!r) throw new SendError("The micro:bit stopped answering. Unplug it, plug it back in and click Send again.");
        if (r.err.trim()) {
          const last = r.err.trim().split(/\r?\n/).pop();
          throw new SendError(/ENOSPC|: 28$|Errno 28/.test(last) ? "The micro:bit has no room for your code. Make it shorter, or do it once "
            + "the long way (Download main.py, python.microbit.org)." : "The micro:bit said: " + last);
        }
        return r.out;
      },
      end() { d.rawRx = null; },
    };
  }

  function sendClicked(target) {
    if (!("serial" in navigator)) { usbMsg("This browser cannot talk to USB devices. Please use Chrome or Edge.", "cs-bad"); return; }
    let d = target;
    if (!d) {
      const boards = [...shell.devices.values()].filter((x) => x.kind === "usb" && x.connected);
      if (!boards.length) { usbMsg("Connect your micro:bit first: click Connect micro:bit above.", "cs-warn"); return; }
      if (boards.length > 1) { usbMsg(boards.length + " micro:bits are connected: click Send my code here on the one you want.", "cs-warn"); return; }
      d = boards[0];
    }
    sendCode(d);
  }

  async function sendCode(d) {
    if (d.sending || !d.connected) return;
    if (code.view === "blocks") await ensureBlocks();
    const text = currentCode().replace(/\r\n?/g, "\n");
    if (!text.trim()) { usbMsg("Your code is empty — make some blocks or write some Python first.", "cs-warn"); return; }
    const bytes = new TextEncoder().encode(text);
    let sum = 0;
    for (const b of bytes) sum += b;
    d.sending = true;
    d.sendPct = 0;
    usbMsg("Sending your code to " + d.id + "… keep it plugged in.", "");
    logLine("game", d.id, "Sending your code (" + bytes.length + " bytes)…", "");
    renderBoards();
    const repl = rawRepl(d);
    let wrote = false;
    try {
      await repl.enter();
      sendGone(d); // the old program has stopped: the game forgets what it set up for every ride
      sendStatus(d);
      await repl.exec("f=open('main.py','wb')\nw=f.write");
      wrote = true;
      for (let i = 0; i < bytes.length; i += SEND_PIECE) {
        await repl.exec("w(" + pyBytes(bytes.subarray(i, i + SEND_PIECE)) + ")");
        d.sendPct = Math.min(99, Math.round(((i + SEND_PIECE) / bytes.length) * 100));
        renderBoards();
      }
      await repl.exec("f.close()");
      const back = (await repl.exec("f=open('main.py','rb')\nn=0\ns=0\nwhile True:\n c=f.read(64)\n if not c:\n  break\n n+=len(c)\n"
        + " s+=sum(c)\nf.close()\nprint(n,s)")).trim();
      if (back !== bytes.length + " " + sum) throw new SendError("Your code did not arrive in one piece. Click Send again.");
      // Restart the board so it runs the new main.py; the serial port stays open (only the main chip resets).
      repl.rx.text = "";
      await repl.sendSlow("import microbit\nmicrobit.reset()\n");
      await repl.send("\x04");
      await sleepMs(400);
      const t = repl.rx.text, ok = t.indexOf("OK");
      repl.end();
      d.rxBuf = "";
      d.sending = false;
      d.lastLineAt = now();
      if (ok >= 0) feed(d, t.slice(ok + 2)); // the new program's first lines
      usbMsg("Sent! " + d.id + " is running your new code. Press A to ride.", "cs-good");
      logLine("game", d.id, "Sent! Your micro:bit restarted with your new code.", "");
    } catch (e) {
      const msg = e instanceof SendError ? e.message : "Sending failed: " + errText(e);
      // Leave the raw REPL and restart whatever main.py is there now.
      try { if (d.connected) { await repl.send("\x02"); await sleepMs(50); await repl.send("\x04"); } } catch (e2) { /* gone */ }
      repl.end();
      d.rxBuf = "";
      d.sending = false;
      d.lastLineAt = now();
      usbMsg(msg + (wrote && !/long way/.test(msg) ? " (Your old code was replaced part way, so send again before you ride.)" : ""), "cs-bad");
      logLine("warn", d.id, msg, "");
    }
    renderBoards();
    renderChips();
  }

  // ------------------------------------------------------------------ simulator
  const sim = { frame: null, loaded: false, ready: false, activated: false, running: false, buf: "", accel: 0, keys: { left: false, right: false },
    radioOn: false }; // radioOn: the program turned the radio on (state_change from the simulator)

  function simPost(msg) {
    if (sim.frame && sim.frame.contentWindow) sim.frame.contentWindow.postMessage(msg, SIM_ORIGIN);
  }

  function simSet(id, value) {
    if (!sim.ready) return;
    const ranges = { accelerometerX: [-2000, 2000], soundLevel: [0, 255] };
    const r = ranges[id] || [0, 1];
    simPost({ kind: "set_value", id, value: clamp(Math.round(value), r[0], r[1]) });
  }

  function simDevice() { return getDevice("sim", "sim"); }

  function simHint(text) {
    const h = $("cs-sim-hint");
    h.hidden = !text;
    if (text) h.innerHTML = text;
  }

  function simLoad() {
    // A fresh iframe each time: the only way to reset a cross-origin page, and a wedged simulator needs it.
    const old = $("cs-sim");
    const f = old.cloneNode(false);
    f.removeAttribute("src");
    old.replaceWith(f);
    sim.frame = f;
    sim.loaded = true;
    sim.ready = false;
    sim.activated = false;
    sim.running = false;
    sim.radioOn = false;
    sim.buf = "";
    simHint("Loading the simulator…");
    f.src = SIM_URL;
  }

  function simEnter() {
    simDevice(); // so the status bar shows "Simulator (click ▶)" before the first run
    if (!sim.loaded) simLoad();
    else if (sim.activated) simFlash(); // back from another mode: run the code again
  }

  // The simulator reports an internal error ("Must be running as called via HAL") when it stops a program while a
  // radio packet is waiting for radio.receive(). So with robot racers feeding it: stop feeding at once and give the
  // program a few loops to read its queue before a flash or a stop. Returns that wait in ms (0 if nothing was fed).
  function simQuietRadio() {
    const feeding = bots.on && sim.running && sim.radioOn;
    sim.radioOn = false; // the robots stop now; the next program turns the radio on again (state_change)
    return feeding ? 250 : 0;
  }

  function simLeave() {
    const wait = simQuietRadio();
    if (sim.activated) {
      if (wait) setTimeout(() => { if (shell.mode !== "sim") simPost({ kind: "stop" }); }, wait); // back already: it flashes
      else simPost({ kind: "stop" });
    }
    sim.running = false;
    const d = simDevice();
    d.connected = false;
    releaseSimKeys();
  }

  function simFlash() {
    if (!sim.activated) {
      simHint("First click <b>▶</b> on the micro:bit — after that, <b>Run my code</b> works.");
      $("cs-sim-hint").classList.remove("cs-pulse"); void $("cs-sim-hint").offsetWidth; $("cs-sim-hint").classList.add("cs-pulse");
      return;
    }
    const wait = simQuietRadio(); // the new program turns the radio on again (state_change)
    const main = new TextEncoder().encode(currentCode());
    const flash = () => simPost({ kind: "flash", filesystem: { "main.py": main } });
    if (wait) setTimeout(flash, wait); else flash();
    sim.running = true;
    sim.buf = "";
    const d = simDevice();
    if (!d.ver) d.ver = "v2";
    d.race = ""; // a new program: note its race: again
    sendGone(d); // the old program has gone (the new one is connected again just below)
    d.connected = true;
    d.connectedAt = now();
    sendStatus(d);
    simHint("");
    logLine("game", "", "Simulator: your code is running.", "");
    renderChips();
  }

  function onSimMessage(e) {
    if (!sim.frame || e.source !== sim.frame.contentWindow) return;
    const m = e.data || {};
    switch (m.kind) {
      case "ready":
        sim.ready = true;
        if (!sim.activated) simHint("Click <b>▶</b> on the micro:bit to start");
        break;
      case "request_flash":
        // The student clicked the simulator's own play button: from now on we may flash any time.
        sim.activated = true;
        if (shell.mode === "sim") simFlash();
        break;
      case "serial_output": {
        sim.buf += String(m.data || "");
        let i;
        while ((i = sim.buf.indexOf("\n")) >= 0) {
          const line = sim.buf.slice(0, i).replace(/\r$/, "");
          sim.buf = sim.buf.slice(i + 1);
          if (line.length && shell.mode === "sim") onDeviceLine(simDevice(), line);
        }
        if (sim.buf.length > 2000) sim.buf = "";
        break;
      }
      case "internal_error":
        logLine("error", "sim", "Simulator problem: " + errText(m.error), "click Reset simulator");
        break;
      // ---- Race (radio race v1): is the radio on (robot racers may only send then), and what the program sends by radio
      case "state_change": {
        const r = m.change && m.change.radio;
        if (r) sim.radioOn = !!r.enabled;
        break;
      }
      case "radio_output": {
        // radio.send("p:…") arrives as bytes with MicroPython's 3-byte text header \x01\x00\x01 in front
        const b = m.data instanceof Uint8Array ? m.data : new Uint8Array(0);
        const text = b.length >= 3 && b[0] === 1 && b[1] === 0 && b[2] === 1 ? new TextDecoder().decode(b.subarray(3))
          : "(bytes) " + Array.from(b.subarray(0, 12)).join(" ");
        if (shell.mode === "sim") noteRadioSent(simDevice(), text);
        break;
      }
      default:
        break;
    }
  }

  // ---- Race (radio race v1): robot racers. Students without a board test their relay code in the simulator: three
  // robots "send" p:BOTn:<metres>:<lane> to the virtual micro:bit's radio (radio_input) 4 times a second, like
  // micro:bits in the same race group. radio_input takes the bytes radio.send() puts on air: \x01\x00\x01 + the text
  // (radio.receive() raises ValueError without that header). Checked 11/9 against micropython-microbit-v2-simulator
  // (board/radio.ts) and in the embedded simulator. The simulator ignores the group (it assumes "your group").
  const BOT_SPEEDS = [13, 15, 17]; // m/s; the game's top speed is about 22, so they look honest to its "?" check
  const bots = { on: false, t0: 0, metres: 2000, finished: [], tick: 0 };

  function simRadioSend(text) {
    // The simulator throws inside the iframe if the radio is off, so only send while the program has it on.
    if (shell.mode !== "sim" || !sim.running || !sim.radioOn) return false;
    const body = new TextEncoder().encode(text);
    const data = new Uint8Array(3 + body.length);
    data.set([1, 0, 1]);
    data.set(body, 3);
    simPost({ kind: "radio_input", data });
    return true;
  }

  function botsNote(text) {
    const n = $("cs-bots-note");
    if (n.textContent !== text) n.textContent = text;
  }

  function botsReset(delayMs, metres) {
    bots.t0 = now() + delayMs;
    bots.metres = metres;
    bots.finished = BOT_SPEEDS.map(() => 0);
  }

  // Every 250 ms: riding (p: 4 a second), finished (f: twice a second for 10 s), then waiting (p:…:0:0 once a second),
  // the same as a real micro:bit relaying its game.
  function botsTick() {
    if (!bots.on) return;
    bots.tick++;
    if (shell.mode !== "sim" || !sim.running) { botsNote("Robots are waiting for your code to run."); return; }
    if (!sim.radioOn) { botsNote("Robots can't reach you: your code has not turned the radio on (join race)."); return; }
    const t = (now() - bots.t0) / 1000;
    const shown = [];
    BOT_SPEEDS.forEach((v, i) => {
      const name = "BOT" + (i + 1);
      const lane = Math.round(2 * Math.sin(t * 0.4 + i * 1.7));
      if (t < 0) { // counting down after go
        simRadioSend("p:" + name + ":0:" + lane);
        shown.push(name + " ready");
        return;
      }
      if (!bots.finished[i]) {
        const dist = Math.max(0, Math.floor(v * t + 15 * Math.sin(t * 0.5 + i * 2)));
        if (dist < bots.metres) {
          simRadioSend("p:" + name + ":" + dist + ":" + lane);
          shown.push(name + " " + dist + " m");
          return;
        }
        bots.finished[i] = Math.max(0.01, t);
      }
      const time = bots.finished[i].toFixed(2);
      if (t - bots.finished[i] < 10) {
        if (bots.tick % 2 === 0) simRadioSend("f:" + name + ":" + time);
        shown.push(name + " " + time + " s");
      } else {
        if (bots.tick % 4 === 0) simRadioSend("p:" + name + ":0:0");
        shown.push(name + " waiting");
      }
    });
    botsNote(shown.join(" · "));
  }

  function botsToggle() {
    bots.on = !bots.on;
    $("cs-bots").setAttribute("aria-pressed", String(bots.on));
    if (bots.on) botsReset(0, 2000);
    botsNote(bots.on ? "Robots: getting ready…" : "Robots off.");
  }

  function botsGoClicked() {
    if (shell.mode !== "sim" || !sim.running || !sim.radioOn) {
      botsNote("Run code that turns the radio on first (join race), then Send go.");
      return;
    }
    const go = "go:" + (1 + Math.floor(Math.random() * 99999)) + ":uwa:2000";
    for (let i = 0; i < 5; i++) setTimeout(() => simRadioSend(go), i * 100); // like a real starter: 5 times in 0.5 s
    logLine("game", "sim", "Robots sent " + go + " by radio: a race from UWA, 2000 m", "");
    if (bots.on) botsReset(3000, 2000); // they count down 3-2-1 with the game, then race from 0
  }

  function simResetClicked() {
    simLeave();
    simLoad();
    renderChips();
  }

  // Keyboard -> virtual micro:bit (only in Simulator mode and only when not typing).
  const typing = (el) => el && (el.tagName === "TEXTAREA" || el.tagName === "INPUT" || el.isContentEditable);

  function simKey(e, down) {
    if (shell.mode !== "sim" || typing(e.target) || e.ctrlKey || e.altKey || e.metaKey) return;
    let handled = true;
    switch (e.key) {
      case "ArrowLeft": sim.keys.left = down; break;
      case "ArrowRight": sim.keys.right = down; break;
      case "a": case "A": if (!e.repeat) simSet("buttonA", down ? 1 : 0); break;
      case "b": case "B": if (!e.repeat) simSet("buttonB", down ? 1 : 0); break;
      case "m": case "M": if (!e.repeat) simSet("soundLevel", down ? 200 : 10); break;
      case "l": case "L": if (!e.repeat) simSet("pinLogo", down ? 1 : 0); break;
      default: handled = false;
    }
    if (handled) e.preventDefault();
  }

  function releaseSimKeys() {
    sim.keys.left = sim.keys.right = false;
    if (sim.ready) { simSet("buttonA", 0); simSet("buttonB", 0); simSet("soundLevel", 10); simSet("pinLogo", 0); }
  }

  function simTiltTick() {
    if (shell.mode !== "sim" || !sim.ready) return;
    const target = (sim.keys.right ? 700 : 0) - (sim.keys.left ? 700 : 0);
    if (sim.accel === target) return;
    sim.accel += clamp(target - sim.accel, -100, 100); // ~0.3 s from level to full tilt
    simSet("accelerometerX", sim.accel);
  }

  // ------------------------------------------------------------------ code: Blocks and Python views
  // Blocks (Blockly -> MicroPython, code/microbit-blocks.js + the game's pack) is loaded on first use, so keyboard
  // players never download it. The Python view is a plain textarea. Blocks -> Python is one way: once the student
  // edits the Python, the blocks no longer overwrite it unless they click "Use my blocks' code".
  const code = {
    view: "blocks", blocks: null, loading: null, failed: false,
    pythonIsFromBlocks: false, // textarea holds untouched generated code
    lastGenerated: "",
  };

  function loadScript(src) {
    return new Promise((resolve, reject) => {
      const s = document.createElement("script");
      s.src = src;
      s.onload = resolve;
      s.onerror = () => reject(new Error("could not load " + src));
      document.head.appendChild(s);
    });
  }

  function ensureBlocks() {
    if (code.blocks || code.failed) return Promise.resolve(code.blocks);
    if (code.loading) return code.loading;
    const v = shell.vendorBase + "blockly/";
    const files = [v + "blockly_compressed.js", v + "blocks_compressed.js", v + "python_compressed.js", v + "msg/en.js",
      shell.codeBase + "code/microbit-blocks.js", ...shell.codePacks.map((p) => shell.codeBase + p)];
    code.loading = files.reduce((p, f) => p.then(() => loadScript(f)), Promise.resolve()).then(() => {
      $("cs-blockly-msg").remove();
      code.blocks = window.MicrobitBlocks.mount($("cs-blockly"), { media: v + "media/", onChange: onBlocksChanged });
      let saved = null;
      try { saved = JSON.parse(storeGet(KEY_BLOCKS) || "null"); } catch (e) { saved = null; }
      try {
        if (saved) code.blocks.load(saved); else code.blocks.loadStarter();
      } catch (e) {
        code.blocks.loadStarter(); // a draft from an older block set
      }
      onBlocksChanged();
      return code.blocks;
    }).catch((e) => {
      code.failed = true;
      $("cs-blockly-msg").textContent = "Blocks could not load (" + errText(e) + "). Use the Python view.";
      return null;
    });
    return code.loading;
  }

  function onBlocksChanged() {
    if (!code.blocks) return;
    let py;
    try { py = code.blocks.code(); } catch (e) { py = "# Could not turn the blocks into Python: " + errText(e) + "\n"; }
    code.lastGenerated = py;
    $("cs-preview").textContent = py;
    try { storeSet(KEY_BLOCKS, JSON.stringify(code.blocks.save())); } catch (e) { /* storage full or blocked */ }
    if (code.pythonIsFromBlocks) $("cs-code").value = py;
    flashSaved();
  }

  function flashSaved() {
    const s = $("cs-saved");
    s.textContent = "saved in this browser";
    clearTimeout(flashSaved.t);
    flashSaved.t = setTimeout(() => { s.textContent = ""; }, 1500);
  }

  function setView(view, user) {
    code.view = view === "python" ? "python" : "blocks";
    if (user) storeSet(KEY_VIEW, code.view);
    document.querySelectorAll(".cs-tabs button").forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.view === code.view)));
    $("cs-blocks-view").hidden = code.view !== "blocks";
    $("cs-python-view").hidden = code.view !== "python";
    const ta = $("cs-code");
    if (code.view === "python") {
      // First visit, or Python still equals what the blocks made: show the blocks' code.
      if (code.lastGenerated && (!ta.value.trim() || code.pythonIsFromBlocks || ta.value === shell.starterText)) {
        ta.value = code.lastGenerated;
        code.pythonIsFromBlocks = true;
      }
      $("cs-python-note").hidden = code.pythonIsFromBlocks || !code.lastGenerated;
    } else {
      ensureBlocks().then(() => {
        if (!code.blocks) return;
        code.blocks.resize();
        if (code.blocks.needsFit) code.blocks.fit(); // loaded while hidden
      });
    }
  }

  // The code panel lives in whichever mode pane is showing (USB or Simulator).
  function placeCode() {
    const pane = document.querySelector('.cs-pane[data-pane="' + shell.mode + '"]');
    const slot = pane && pane.querySelector(".cs-code-slot");
    const section = $("cs-code-section");
    if (!slot) { section.hidden = true; return; }
    if (section.parentElement !== slot) slot.appendChild(section);
    section.hidden = false;
    setView(code.view, false);
  }

  function currentCode() {
    if (code.view === "blocks" && code.blocks) return code.blocks.code();
    return $("cs-code").value;
  }

  function initEditor() {
    const ta = $("cs-code");
    const draft = storeGet(KEY_DRAFT);
    if (draft) ta.value = draft;
    loadStarterText(); // keep the text starter around for the Python view
    code.view = storeGet(KEY_VIEW) === "python" ? "python" : "blocks";
    let saveTimer = 0;
    ta.addEventListener("input", () => {
      code.pythonIsFromBlocks = false;
      $("cs-python-note").hidden = !code.lastGenerated;
      clearTimeout(saveTimer);
      saveTimer = setTimeout(() => { storeSet(KEY_DRAFT, ta.value); flashSaved(); }, 500);
    });
    ta.addEventListener("keydown", (e) => {
      if (e.key === "Tab" && !e.shiftKey && !e.ctrlKey && !e.altKey) {
        e.preventDefault();
        ta.setRangeText("    ", ta.selectionStart, ta.selectionEnd, "end");
        ta.dispatchEvent(new Event("input"));
      } else if (e.key === "Enter" && (e.ctrlKey || e.metaKey)) {
        e.preventDefault();
        if (shell.mode === "usb") sendClicked(); else simFlash();
      }
    });
    document.querySelectorAll(".cs-tabs button").forEach((b) => b.addEventListener("click", () => setView(b.dataset.view, true)));
    $("cs-from-blocks").addEventListener("click", () => {
      if (!window.confirm("Replace your Python with the code from your blocks?")) return;
      ta.value = code.lastGenerated;
      code.pythonIsFromBlocks = true;
      storeSet(KEY_DRAFT, ta.value);
      $("cs-python-note").hidden = true;
    });
  }

  async function loadStarterText() {
    try {
      if (shell.starterText == null) {
        const r = await fetch(shell.starterUrl, { cache: "no-cache" });
        if (!r.ok) throw new Error("HTTP " + r.status);
        shell.starterText = await r.text();
      }
      return shell.starterText;
    } catch (e) {
      logLine("warn", "", "Could not load the starter code (" + errText(e) + ").", "paste your code instead");
      return null;
    }
  }

  async function starterClicked() {
    if (code.view === "blocks") {
      if (!code.blocks || !window.confirm("Replace your blocks with the starter blocks?")) return;
      code.blocks.loadStarter();
      return;
    }
    if ($("cs-code").value.trim() && !window.confirm("Replace your Python with the starter code?")) return;
    const text = await loadStarterText();
    if (text == null) return;
    $("cs-code").value = text;
    code.pythonIsFromBlocks = false;
    storeSet(KEY_DRAFT, text);
  }

  function downloadCode() {
    const blob = new Blob([currentCode()], { type: "text/x-python" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = "main.py";
    document.body.appendChild(a);
    a.click();
    setTimeout(() => { URL.revokeObjectURL(a.href); a.remove(); }, 0);
  }

  // Drag the bar between the game and the panel; the width is remembered.
  function initSplitter() {
    const bar = $("cs-splitter");
    const main = document.querySelector(".cs-main");
    const apply = (w) => {
      const max = Math.max(360, window.innerWidth - 420);
      main.style.setProperty("--panel-w", clamp(Math.round(w), 360, max) + "px");
    };
    const saved = parseInt(storeGet(KEY_PANEL) || "", 10);
    if (saved) apply(saved);
    bar.addEventListener("pointerdown", (e) => {
      e.preventDefault();
      bar.setPointerCapture(e.pointerId);
      document.body.classList.add("cs-resizing");
      const move = (ev) => { apply(window.innerWidth - ev.clientX); if (code.blocks) code.blocks.resize(); };
      const up = () => {
        bar.removeEventListener("pointermove", move);
        document.body.classList.remove("cs-resizing");
        storeSet(KEY_PANEL, String(parseInt(getComputedStyle(main).getPropertyValue("--panel-w"), 10)));
      };
      bar.addEventListener("pointermove", move);
      bar.addEventListener("pointerup", up, { once: true });
    });
    window.addEventListener("resize", () => { if (code.blocks) code.blocks.resize(); });
  }

  // ------------------------------------------------------------------ page chrome
  function initChrome(canvas) {
    const app = document.createElement("div");
    app.className = "cs-app";
    app.innerHTML = LAYOUT;
    document.body.insertBefore(app, canvas);
    $("cs-stage").appendChild(canvas);
    canvas.addEventListener("pointerdown", () => canvas.focus());

    document.querySelectorAll(".cs-modes button").forEach((b) => b.addEventListener("click", () => setMode(b.dataset.mode, true)));
    $("cs-connect").addEventListener("click", connectClicked);
    $("cs-pause").addEventListener("click", () => setPaused(!monitor.paused));
    $("cs-clear").addEventListener("click", () => { $("cs-log").textContent = ""; monitor.pending.length = 0; });
    $("cs-run").addEventListener("click", simFlash);
    $("cs-sim-reset").addEventListener("click", simResetClicked);
    $("cs-bots").addEventListener("click", botsToggle);
    $("cs-bots-go").addEventListener("click", botsGoClicked);
    $("cs-download").addEventListener("click", downloadCode);
    $("cs-send").addEventListener("click", () => sendClicked());
    $("cs-starter").addEventListener("click", starterClicked);
    initSplitter();

    const help = $("cs-help");
    const showHelp = (v) => { help.hidden = !v; if (v) $("cs-help-close").focus(); };
    $("cs-help-btn").addEventListener("click", () => showHelp(true));
    $("cs-help-close").addEventListener("click", () => showHelp(false));
    help.addEventListener("click", (e) => { if (e.target === help) showHelp(false); });

    $("cs-fs-btn").addEventListener("click", () => {
      try {
        if (document.fullscreenElement) document.exitFullscreen();
        else document.documentElement.requestFullscreen();
      } catch (e) { /* not allowed */ }
    });
    document.addEventListener("fullscreenchange", () => {
      $("cs-fs-btn").textContent = document.fullscreenElement ? "Exit fullscreen" : "Fullscreen";
    });

    document.addEventListener("keydown", (e) => {
      if (e.key === "Escape" && !help.hidden) { showHelp(false); return; }
      simKey(e, true);
    });
    document.addEventListener("keyup", (e) => simKey(e, false));
    window.addEventListener("blur", releaseSimKeys);
    window.addEventListener("message", onSimMessage);
  }

  // ------------------------------------------------------------------ public API (docs/bridge.md)
  window.CityScooterShell = {
    boot(canvas, config, options) {
      if (options && options.starterUrl) shell.starterUrl = options.starterUrl;
      if (options && options.codeBase) shell.codeBase = options.codeBase;
      if (options && options.vendorBase) shell.vendorBase = options.vendorBase;
      if (options && options.codePacks) shell.codePacks = options.codePacks;
      if (options && options.keyboard === false) { shell.keyboardAllowed = false; shell.keyboardEnabled = false; }
      initChrome(canvas);
      if (!shell.keyboardAllowed) {
        // micro:bit only: no Keyboard mode to choose (Unity also gets SetKeyboardEnabled("0") in every mode).
        document.querySelectorAll('.cs-modes button[data-mode="keyboard"], .cs-pane[data-pane="keyboard"]').forEach((e) => e.remove());
        document.body.dataset.keyboard = "off";
      }
      initEditor();
      initSerial();
      setInterval(renderStats, 200);
      setInterval(checkSilent, 500);
      setInterval(simTiltTick, 40);
      setInterval(botsTick, 250);     // ---- Race: robot racers in the simulator
      setInterval(radioSummary, 1000); // ---- Race: radio traffic in the monitor, once a second
      if (!("serial" in navigator)) usbMsg("This browser cannot talk to USB devices. Please use Chrome or Edge.", "cs-bad");
      setMode(storeGet(KEY_MODE) || (shell.keyboardAllowed ? "keyboard" : "usb"), false);
      requestAnimationFrame(pump);

      createUnityInstance(canvas, config, (p) => {
        $("cs-progress").textContent = Math.round(p * 100) + "%";
        $("cs-progressbar").style.width = Math.round(p * 100) + "%";
      }).then((instance) => {
        shell.unity = instance;
        $("cs-loading").remove();
        if (shell.mode !== "sim") canvas.focus();
      }).catch((e) => {
        $("cs-loading").textContent = "The game failed to load: " + errText(e) + ". Try reloading the page in Chrome or Edge.";
      });
    },

    /** USB events with times (open retries, glitches, reconnects); the teacher can copy it from the console. */
    get usbLog() { return shell.usbLog.slice(); },

    onUnityReady(version) {
      shell.unityReady = true;
      for (const p of pendingCalls.splice(0)) callUnity(p.method, p.arg);
      callUnity("SetKeyboardEnabled", shell.keyboardEnabled ? "1" : "0");
      for (const d of shell.devices.values()) sendStatus(d);
    },

    sendToDevices(target, line) {
      let sent = false;
      for (const d of shell.devices.values()) {
        if (!isActive(d) || (target !== "*" && target !== d.id)) continue;
        if (d.kind === "usb") { if (writeBoard(d, line)) sent = true; }
        else if (d.kind === "sim") { simPost({ kind: "serial_input", data: line + "\n" }); sent = true; }
      }
      if (!sent) return;
      // ---- Race: pos: comes 4 times a second (fin: twice), so the monitor shows the latest once a second
      if (/^(pos|fin):/.test(line)) { raceOut.n++; raceOut.last = line; return; }
      logLine("game", "game", line, zoneHint(line));
    },
  };
})();
