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
    starterUrl: "TemplateData/starter.py",
    starterText: null,
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
  <aside class="cs-panel">
    <div class="cs-pane" data-pane="keyboard">
      <h2>Play with the keyboard</h2>
      <p class="cs-muted">Click the game first so it gets your keys.</p>
      <table class="cs-keys">
        <tr><td><kbd>←</kbd> <kbd>→</kbd></td><td>steer</td></tr>
        <tr><td><kbd>Space</kbd></td><td>jump (and start a ride)</td></tr>
        <tr><td><kbd>S</kbd></td><td>brake</td></tr>
      </table>
      <h3>Got a micro:bit?</h3>
      <p>Choose <b>micro:bit (USB)</b> above and connect it.</p>
      <h3>No micro:bit?</h3>
      <p>Choose <b>Simulator</b> to run your own code on a virtual micro:bit.</p>
    </div>
    <div class="cs-pane" data-pane="usb">
      <button type="button" class="cs-primary cs-big" id="cs-connect">Connect micro:bit</button>
      <div class="cs-usb-msg" id="cs-usb-msg" hidden></div>
      <div id="cs-boards"></div>
      <h3>How to connect</h3>
      <ol class="cs-steps">
        <li>Flash your program (for example the starter) from <b>python.microbit.org</b>, then click <b>Disconnect</b> there.</li>
        <li>Plug the micro:bit into this computer with a USB data cable.</li>
        <li>Click <b>Connect micro:bit</b>, pick it in the list, click <b>Connect</b>. Press <b>A</b> to start a ride.</li>
      </ol>
      <p class="cs-muted">Tilt to steer · A jump · hold B brake. Up to 4 boards can play together.</p>
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
          <button type="button" id="cs-download">Download .py</button>
          <button type="button" id="cs-starter">Load starter</button>
          <div class="cs-keycard">
            <div><kbd>←</kbd><kbd>→</kbd> tilt</div>
            <div><kbd>A</kbd><kbd>B</kbd> buttons</div>
            <div>hold <kbd>M</kbd> shout</div>
            <div>hold <kbd>L</kbd> touch logo</div>
          </div>
        </div>
      </div>
      <div class="cs-editor-head"><b>main.py</b><span class="cs-muted" id="cs-saved"></span></div>
      <textarea id="cs-code" spellcheck="false" autocomplete="off" autocapitalize="off" wrap="off"
        aria-label="Your MicroPython code"></textarea>
    </div>
  </aside>
</main>
<div class="cs-overlay" id="cs-help" hidden>
  <div class="cs-dialog" role="dialog" aria-modal="true" aria-labelledby="cs-help-title">
    <button type="button" class="cs-close" id="cs-help-close" aria-label="Close">×</button>
    <h2 id="cs-help-title">Help</h2>
    <h3>How to connect a micro:bit</h3>
    <ol class="cs-steps">
      <li>On <b>python.microbit.org</b>, flash your program (start with the starter code), then click <b>Disconnect</b>.</li>
      <li>Plug the micro:bit into this computer with a USB cable.</li>
      <li>Here, choose <b>micro:bit (USB)</b>, click <b>Connect micro:bit</b> and pick it in the list. Press <b>A</b> to ride.</li>
    </ol>
    <h3>Something is wrong?</h3>
    <ul>
      <li><b>Use Chrome or Edge.</b> Other browsers cannot talk to USB devices.</li>
      <li><b>"Could not open" / board busy:</b> another tab is using it. On python.microbit.org click <b>Disconnect</b>, or close that tab.</li>
      <li><b>The list is empty:</b> plug it in again, or try another cable — some cables only charge.</li>
      <li><b>Connected but nothing happens:</b> your program must <code>print</code> commands like <code>st:20</code>. Flash it again — code you only ran in the REPL is gone after unplugging.</li>
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
    const colon = lower.indexOf(":");
    if (colon <= 0) return { kind: "unknown" };
    const key = lower.slice(0, colon).trim();
    const arg = s.slice(colon + 1).trim();
    switch (key) {
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

  // ------------------------------------------------------------------ devices
  function getDevice(id, kind) {
    let d = shell.devices.get(id);
    if (!d) {
      d = {
        id, kind, ver: "", connected: false, lastLineAt: 0, connectedAt: 0, silent: false,
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
      case "hello":
        if (d.ver !== c.value) {
          d.ver = c.value;
          sendStatus(d);
          renderChips();
          renderBoards();
        }
        break;
      case "malformed":
        logLine("warn", d.id, line, "st: needs a number, e.g. st:-35");
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
      r.querySelector(".cs-brake").classList.toggle("cs-on", t < d.brakeUntil);
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
      <span class="cs-brake">BRAKE</span>`;
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
    if (!["keyboard", "usb", "sim"].includes(mode)) mode = "keyboard";
    const prev = shell.mode;
    shell.mode = mode;
    document.body.dataset.mode = mode;
    document.querySelectorAll(".cs-modes button").forEach((b) => b.setAttribute("aria-pressed", String(b.dataset.mode === mode)));
    document.querySelectorAll(".cs-pane").forEach((p) => { p.hidden = p.dataset.pane !== mode; });
    if (user) storeSet(KEY_MODE, mode);

    const kb = mode !== "sim"; // in the simulator the arrow keys tilt the virtual micro:bit instead
    if (kb !== shell.keyboardEnabled || prev === mode) {
      shell.keyboardEnabled = kb;
      callUnity("SetKeyboardEnabled", kb ? "1" : "0");
    }
    if (mode === "sim") simEnter();
    else if (prev === "sim") simLeave();
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

  async function openBoard(d) {
    if (d.connected || d.opening) return;
    d.opening = true;
    try {
      await d.port.open({ baudRate: 115200 });
    } catch (e) {
      if (!/already open/i.test(errText(e))) {
        d.opening = false;
        usbMsg("Could not open the micro:bit. Another tab may be using this board, e.g. python.microbit.org — click Disconnect there "
          + "(or close that tab), then click Connect micro:bit again.", "cs-bad");
        renderBoards();
        return;
      }
    }
    d.opening = false;
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

  async function readLoop(d) {
    const decoder = new TextDecoder();
    let buf = "";
    while (d.connected && d.port.readable) {
      const reader = d.port.readable.getReader();
      d.reader = reader;
      try {
        for (;;) {
          const { value, done } = await reader.read();
          if (done) break;
          buf += decoder.decode(value, { stream: true });
          let i;
          while ((i = buf.indexOf("\n")) >= 0) {
            const line = buf.slice(0, i).replace(/\r$/, "");
            buf = buf.slice(i + 1);
            if (line.length) onDeviceLine(d, line);
          }
          if (buf.length > 1000) buf = "";
        }
      } catch (e) {
        // Unplugged or a serial glitch; the disconnect event handles the rest.
      } finally {
        try { reader.releaseLock(); } catch (e) { /* already released */ }
        d.reader = null;
      }
    }
  }

  function writeBoard(d, line) {
    if (!d.connected || !d.port.writable) return;
    d.chain = (d.chain || Promise.resolve()).then(async () => {
      if (!d.port.writable) return;
      const w = d.port.writable.getWriter();
      try { await w.write(new TextEncoder().encode(line + "\n")); } catch (e) { /* board gone */ } finally { w.releaseLock(); }
    });
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
        d.connected = false;
        d.silent = false;
        // cancel() rejects when the stream already errored (it has, after an unplug): swallow it.
        if (d.reader) d.reader.cancel().catch(() => { /* gone */ });
        sendStatus(d);
      }
      renderChips();
      renderBoards();
    });
    navigator.serial.getPorts().then((ports) => ports.forEach(addPort)).catch(() => { /* not allowed here */ });
  }

  function renderBoards() {
    const box = $("cs-boards");
    if (!box) return;
    box.textContent = "";
    const boards = [...shell.devices.values()].filter((d) => d.kind === "usb");
    for (const d of boards) {
      const c = document.createElement("div");
      c.className = "cs-board " + (d.connected ? "cs-ok" : "cs-off");
      const title = document.createElement("div");
      title.className = "cs-board-title";
      title.textContent = d.id + (d.ver ? " · " + d.ver.toUpperCase() : "") + " — "
        + (d.connected ? (isActive(d) ? "connected" : "connected (switch to micro:bit (USB) to use it)") : "unplugged — plug it back in");
      c.appendChild(title);
      if (d.silent) {
        const w = document.createElement("div");
        w.className = "cs-board-warn";
        w.textContent = SILENT_TEXT;
        c.appendChild(w);
      }
      box.appendChild(c);
    }
    $("cs-connect").textContent = boards.some((d) => d.connected) ? "Connect another micro:bit" : "Connect micro:bit";
  }

  function checkSilent() {
    const t = now();
    let changed = false;
    for (const d of shell.devices.values()) {
      if (!d.connected) continue;
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

  // ------------------------------------------------------------------ simulator
  const sim = { frame: null, loaded: false, ready: false, activated: false, running: false, buf: "", accel: 0, keys: { left: false, right: false } };

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
    sim.buf = "";
    simHint("Loading the simulator…");
    f.src = SIM_URL;
  }

  function simEnter() {
    simDevice(); // so the status bar shows "Simulator (click ▶)" before the first run
    if (!sim.loaded) simLoad();
    else if (sim.activated) simFlash(); // back from another mode: run the code again
  }

  function simLeave() {
    if (sim.activated) simPost({ kind: "stop" });
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
    const code = $("cs-code").value;
    simPost({ kind: "flash", filesystem: { "main.py": new TextEncoder().encode(code) } });
    sim.running = true;
    sim.buf = "";
    const d = simDevice();
    if (!d.ver) d.ver = "v2";
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
      default:
        break;
    }
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

  // ------------------------------------------------------------------ code editor
  function initEditor() {
    const ta = $("cs-code");
    const draft = storeGet(KEY_DRAFT);
    if (draft) ta.value = draft;
    else loadStarter(false);
    let saveTimer = 0;
    ta.addEventListener("input", () => {
      clearTimeout(saveTimer);
      saveTimer = setTimeout(() => { storeSet(KEY_DRAFT, ta.value); $("cs-saved").textContent = "saved in this browser"; }, 500);
      $("cs-saved").textContent = "";
    });
    ta.addEventListener("keydown", (e) => {
      if (e.key === "Tab" && !e.shiftKey && !e.ctrlKey && !e.altKey) {
        e.preventDefault();
        ta.setRangeText("    ", ta.selectionStart, ta.selectionEnd, "end");
        ta.dispatchEvent(new Event("input"));
      } else if (e.key === "Enter" && (e.ctrlKey || e.metaKey)) {
        e.preventDefault();
        simFlash();
      }
    });
  }

  async function loadStarter(ask) {
    if (ask && $("cs-code").value.trim() && !window.confirm("Replace your code with the starter code?")) return;
    try {
      if (shell.starterText == null) {
        const r = await fetch(shell.starterUrl, { cache: "no-cache" });
        if (!r.ok) throw new Error("HTTP " + r.status);
        shell.starterText = await r.text();
      }
      $("cs-code").value = shell.starterText;
      storeSet(KEY_DRAFT, shell.starterText);
    } catch (e) {
      logLine("warn", "", "Could not load the starter code (" + errText(e) + ").", "paste your code instead");
    }
  }

  function downloadCode() {
    const blob = new Blob([$("cs-code").value], { type: "text/x-python" });
    const a = document.createElement("a");
    a.href = URL.createObjectURL(blob);
    a.download = "cityscooter.py";
    document.body.appendChild(a);
    a.click();
    setTimeout(() => { URL.revokeObjectURL(a.href); a.remove(); }, 0);
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
    $("cs-download").addEventListener("click", downloadCode);
    $("cs-starter").addEventListener("click", () => loadStarter(true));

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
      initChrome(canvas);
      initEditor();
      initSerial();
      setInterval(renderStats, 200);
      setInterval(checkSilent, 500);
      setInterval(simTiltTick, 40);
      if (!("serial" in navigator)) usbMsg("This browser cannot talk to USB devices. Please use Chrome or Edge.", "cs-bad");
      setMode(storeGet(KEY_MODE) || "keyboard", false);
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
        if (d.kind === "usb") writeBoard(d, line);
        else if (d.kind === "sim") simPost({ kind: "serial_input", data: line + "\n" });
        sent = true;
      }
      if (sent) logLine("game", "game", line, "");
    },
  };
})();
