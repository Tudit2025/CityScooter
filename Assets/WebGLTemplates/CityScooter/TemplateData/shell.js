/* CityScooter web shell — minimal v0 (Claude), enough for USB micro:bits + keyboard.
 * The full shell (simulator mode, rich monitor, multi-board UI) replaces this file but must keep
 * the contract in docs/bridge.md:
 *   JS -> Unity  SendMessage("DeviceBridge", "OnDeviceLines",  "id|line\nid|line")
 *                SendMessage("DeviceBridge", "OnDeviceStatus", "id|kind|1or0|version")
 *                SendMessage("DeviceBridge", "SetKeyboardEnabled", "1" | "0")
 *   Unity -> JS  CityScooterShell.onUnityReady(protocolVersion)
 *                CityScooterShell.sendToDevices(target, line)   target = device id or "*"
 */
(() => {
  "use strict";
  const MICROBIT_VID = 0x0d28;
  const $ = (id) => document.getElementById(id);

  let unity = null;          // unityInstance once loaded
  let unityReady = false;    // DeviceBridge confirmed via onUnityReady
  const outbox = [];         // "id|line" waiting for Unity
  const boards = [];         // { id, port, ver, alive }
  let nextBoard = 1;

  function status(text, cls) {
    const el = $("cs-status");
    el.textContent = text;
    el.className = cls || "";
  }

  function monitor(text) {
    const m = $("cs-monitor");
    const lines = (m.textContent ? m.textContent.split("\n") : []).concat(text);
    m.textContent = lines.slice(-60).join("\n");
    m.scrollTop = m.scrollHeight;
  }

  function toUnity(id, line) {
    outbox.push(id + "|" + line);
    if (outbox.length > 400) outbox.splice(0, outbox.length - 400);
  }

  function pump() {
    if (unity && unityReady && outbox.length) {
      unity.SendMessage("DeviceBridge", "OnDeviceLines", outbox.splice(0, outbox.length).join("\n"));
    }
    requestAnimationFrame(pump);
  }

  function sendStatus(b, connected) {
    if (unity && unityReady) {
      unity.SendMessage("DeviceBridge", "OnDeviceStatus", [b.id, "usb", connected ? "1" : "0", b.ver || ""].join("|"));
    }
  }

  function describeBoards() {
    const live = boards.filter((b) => b.alive);
    if (!live.length) status("No micro:bit connected — keyboard works: ← → steer · Space jump · S brake");
    else status(live.map((b) => b.id + (b.ver ? " " + b.ver.toUpperCase() : "")).join(", ") + " connected", "ok");
  }

  async function openBoard(b) {
    try {
      await b.port.open({ baudRate: 115200 });
    } catch (e) {
      if (!/already open/i.test(String(e && e.message))) {
        status("Could not open " + b.id + ": " + e.message + " (is another tab using this board?)", "bad");
        return;
      }
    }
    b.alive = true;
    sendStatus(b, true);
    describeBoards();
    readLoop(b);
  }

  async function readLoop(b) {
    const decoder = new TextDecoder();
    let buf = "";
    while (b.alive && b.port.readable) {
      const reader = b.port.readable.getReader();
      b.reader = reader;
      try {
        for (;;) {
          const { value, done } = await reader.read();
          if (done) break;
          buf += decoder.decode(value, { stream: true });
          let i;
          while ((i = buf.indexOf("\n")) >= 0) {
            const line = buf.slice(0, i).replace(/\r$/, "");
            buf = buf.slice(i + 1);
            if (line.startsWith("hi:") && b.ver !== line.slice(3)) {
              b.ver = line.slice(3);
              describeBoards();
            }
            if (!line.startsWith("st:") && line !== "br" && !line.startsWith("hi:")) monitor(b.id + "  " + line);
            toUnity(b.id, line);
          }
          if (buf.length > 1000) buf = "";
        }
      } catch (e) {
        // Device lost or a serial glitch; the outer loop stops if the port is gone.
      } finally {
        try { reader.releaseLock(); } catch (e) { /* ignore */ }
      }
    }
  }

  async function writeTo(b, line) {
    if (!b.alive || !b.port.writable) return;
    b.chain = (b.chain || Promise.resolve()).then(async () => {
      const w = b.port.writable.getWriter();
      try { await w.write(new TextEncoder().encode(line + "\n")); } catch (e) { /* board gone */ }
      finally { w.releaseLock(); }
    });
  }

  function addPort(port) {
    let b = boards.find((x) => x.port === port);
    if (!b) {
      b = { id: "usb" + nextBoard++, port, ver: null, alive: false };
      boards.push(b);
    }
    if (!b.alive) openBoard(b);
  }

  async function connectClicked() {
    if (!("serial" in navigator)) {
      status("This browser has no Web Serial. Please use Chrome or Edge.", "bad");
      return;
    }
    try {
      addPort(await navigator.serial.requestPort({ filters: [{ usbVendorId: MICROBIT_VID }] }));
    } catch (e) {
      if (e && e.name !== "NotFoundError") status("Connect failed: " + e.message, "bad");
    }
  }

  function initSerial() {
    if (!("serial" in navigator)) return;
    navigator.serial.addEventListener("disconnect", (e) => {
      const b = boards.find((x) => x.port === e.target);
      if (!b) return;
      b.alive = false;
      sendStatus(b, false);
      describeBoards();
    });
    navigator.serial.addEventListener("connect", (e) => addPort(e.target)); // re-plug / re-flash
    navigator.serial.getPorts().then((ports) => ports.forEach(addPort)).catch(() => {});
  }

  window.CityScooterShell = {
    boot(canvas, config) {
      $("cs-connect").addEventListener("click", connectClicked);
      initSerial();
      requestAnimationFrame(pump);
      createUnityInstance(canvas, config, (p) => {
        $("cs-progress").textContent = Math.round(p * 100) + "%";
      }).then((instance) => {
        unity = instance;
        $("cs-loading").remove();
        canvas.focus();
      }).catch((e) => {
        $("cs-loading").textContent = "Failed to load: " + e;
      });
      canvas.addEventListener("pointerdown", () => canvas.focus());
    },

    onUnityReady(version) {
      unityReady = true;
      boards.forEach((b) => { if (b.alive) sendStatus(b, true); });
      describeBoards();
    },

    sendToDevices(target, line) {
      boards.forEach((b) => { if (target === "*" || target === b.id) writeTo(b, line); });
    },
  };
})();
