/* Stand-in for Unity's loader so the web shell can be developed without a Unity build.
 * createUnityInstance() returns an object with SendMessage(); calls are drawn on the canvas and kept in
 * window.__fakeUnity.calls. One second after loading it calls CityScooterShell.onUnityReady("0"), like
 * WebShellBridge.cs does. The dev toolbar sends game messages through CityScooterShell.sendToDevices(). */
(() => {
  "use strict";
  const fake = { calls: [], lines: [], steer: {}, keyboard: "1", status: {}, jumps: 0, total: 0 };
  // lines keeps the last 1000-2000 lines; total counts every line ever, so a test can take since(total) safely.
  fake.since = (total) => fake.lines.slice(Math.max(0, fake.lines.length - (fake.total - total)));
  window.__fakeUnity = fake;

  function draw(canvas) {
    const r = canvas.getBoundingClientRect();
    if (canvas.width !== Math.round(r.width) || canvas.height !== Math.round(r.height)) {
      canvas.width = Math.max(1, Math.round(r.width));
      canvas.height = Math.max(1, Math.round(r.height));
    }
    const g = canvas.getContext("2d");
    g.fillStyle = "#1e2a38";
    g.fillRect(0, 0, canvas.width, canvas.height);
    g.fillStyle = "#e8edf3";
    g.font = "bold 20px system-ui, sans-serif";
    g.fillText("Fake Unity (tools/shell-dev)", 20, 34);
    g.font = "14px ui-monospace, Consolas, monospace";
    let y = 64;
    g.fillText("keyboard enabled: " + fake.keyboard + "    jumps: " + fake.jumps, 20, y); y += 22;
    for (const [id, s] of Object.entries(fake.status)) { g.fillText("status " + s, 20, y); y += 20; }
    for (const [id, v] of Object.entries(fake.steer)) {
      g.fillText(id + " steer " + v.toFixed(0), 20, y);
      g.fillStyle = "#2f81f7";
      const w = 200, x0 = 180;
      g.fillRect(x0 + w / 2, y - 11, (v / 100) * (w / 2), 12);
      g.fillStyle = "#e8edf3";
      y += 20;
    }
    y += 8;
    g.fillStyle = "#93a0b0";
    for (const c of fake.calls.slice(-12)) { g.fillText(c.slice(0, 110), 20, y); y += 18; }
  }

  window.createUnityInstance = (canvas, config, onProgress) => new Promise((resolve) => {
    let p = 0;
    const t = setInterval(() => {
      p = Math.min(1, p + 0.2);
      if (onProgress) onProgress(p);
      if (p >= 1) {
        clearInterval(t);
        const instance = {
          SendMessage(obj, method, arg) {
            fake.calls.push(obj + "." + method + "(" + JSON.stringify(arg) + ")");
            if (fake.calls.length > 500) fake.calls.splice(0, 100);
            if (method === "OnDeviceLines") {
              for (const entry of String(arg).split("\n")) {
                const bar = entry.indexOf("|");
                const id = entry.slice(0, bar), line = entry.slice(bar + 1);
                fake.lines.push(entry);
                fake.total++;
                const m = /^\s*st\s*:\s*(-?[\d.]+)/i.exec(line);
                if (m) fake.steer[id] = Math.max(-100, Math.min(100, parseFloat(m[1])));
                if (/^\s*jp\s*$/i.test(line)) fake.jumps++;
              }
              if (fake.lines.length > 2000) fake.lines.splice(0, 1000);
            } else if (method === "OnDeviceStatus") {
              fake.status[String(arg).split("|")[0]] = arg;
            } else if (method === "SetKeyboardEnabled") {
              fake.keyboard = arg;
            }
          },
          SetFullscreen() {},
          Quit() { return Promise.resolve(); },
        };
        (function loop() { draw(canvas); requestAnimationFrame(loop); })();
        setTimeout(() => window.CityScooterShell.onUnityReady("0"), 1000);
        resolve(instance);
      }
    }, 120);
  });

  // Dev toolbar: pretend to be the game sending events to the micro:bits.
  window.addEventListener("DOMContentLoaded", () => {
    const bar = document.getElementById("dev-tools");
    let spd = 0, dist = 0, items = 0;
    const send = (line) => window.CityScooterShell.sendToDevices("*", line);
    for (const [label, fn] of [
      ["go", () => send("go")],
      ["crash", () => send("crash")],
      ["gift", () => send("gift")],
      ["spd +1", () => { spd = (spd + 1) % 10; send("spd:" + spd); }],
      ["pos +50", () => { dist += 50; send("pos:" + dist + ":0"); }], // radio race: the relay sends p:NAME:<dist>:0
      ["fin", () => send("fin:72.35")],
      // item race (docs/item-race.md): you got a rocket, one hit you, and "please radio this" (3 times, like the game)
      ["item", () => send("item:rocket")],
      ["hit", () => send("hit:rocket")],
      ["tx", () => { const tx = "tx:i:AMY:rocket:BOT2:" + (items = (items + 1) % 10); for (let i = 0; i < 3; i++) setTimeout(() => send(tx), i * 150); }],
    ]) {
      const b = document.createElement("button");
      b.type = "button";
      b.textContent = label;
      b.addEventListener("click", fn);
      bar.appendChild(b);
    }
  });
})();
