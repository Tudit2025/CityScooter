/* CityScooter lab check.
 * Plain script (no modules, no dependencies) so it loads on any lab PC.
 * Each section catches its own errors; one failing check must not stop the others. */
(() => {
  "use strict";

  const $ = (id) => document.getElementById(id);
  const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
  const now = () => performance.now();
  const STORE_KEY = "cityscooter-labcheck-v1";
  const MICROBIT_VID = 0x0d28;
  const SIM_ORIGIN = "https://python-simulator.usermbit.org";
  const SIM_URL = SIM_ORIGIN + "/v/0.1/simulator.html";

  const R = {
    tool: "cityscooter-labcheck",
    version: 1,
    pcLabel: "",
    startedAt: new Date().toISOString(),
    env: {},
    gpu: null,
    net: null,
    serial: { errors: [] },
    sim: { iframeLoaded: false, ready: false, stateKeys: [], errors: [], auto: null },
    manual: {},
    notes: "",
    pageErrors: [],
  };

  let PROGRAM = "";

  function errText(e) {
    if (!e) return "unknown error";
    if (typeof e === "string") return e;
    return (e.name ? e.name + ": " : "") + (e.message || String(e));
  }

  window.addEventListener("error", (e) => {
    R.pageErrors.push(String(e.message || e.error || "error"));
    scheduleRender();
  });
  window.addEventListener("unhandledrejection", (e) => {
    R.pageErrors.push("unhandled: " + errText(e.reason));
    scheduleRender();
  });

  // ---------------------------------------------------------------- storage
  // Per-viewer convenience only (label, ticks, notes survive a reload).
  function loadStore() {
    try { return JSON.parse(localStorage.getItem(STORE_KEY) || "{}"); } catch (e) { return {}; }
  }
  function saveStore() {
    try {
      localStorage.setItem(STORE_KEY, JSON.stringify({ pcLabel: R.pcLabel, manual: R.manual, notes: R.notes }));
    } catch (e) { /* storage blocked: fine */ }
  }

  // ---------------------------------------------------------------- ui helpers
  function chip(id, text, level) {
    const el = $(id);
    if (!el) return;
    el.textContent = text;
    el.className = "chip" + (level ? " " + level : "");
  }

  function drawSteer(bar, v) {
    const w = Math.min(100, Math.abs(v)) / 2;
    bar.style.width = w + "%";
    bar.style.left = v < 0 ? 50 - w + "%" : "50%";
  }

  let renderTimer = 0;
  function scheduleRender() {
    if (renderTimer) return;
    renderTimer = setTimeout(() => {
      renderTimer = 0;
      $("resultOut").textContent = JSON.stringify(buildResults(), null, 2);
    }, 300);
  }

  function buildResults() {
    return Object.assign({}, R, {
      generatedAt: new Date().toISOString(),
      serial: Object.assign({}, R.serial, { ports: serialPanels.map((p) => p.summary()) }),
    });
  }

  async function copyText(text) {
    try {
      await navigator.clipboard.writeText(text);
      return true;
    } catch (e) {
      const ta = document.createElement("textarea");
      ta.value = text;
      document.body.appendChild(ta);
      ta.select();
      let ok = false;
      try { ok = document.execCommand("copy"); } catch (e2) { ok = false; }
      ta.remove();
      return ok;
    }
  }

  async function copyResults() {
    const ok = await copyText(JSON.stringify(buildResults(), null, 2));
    const m = $("copyMsg");
    m.textContent = ok ? "已复制，直接粘贴给 Claude 就行。" : "复制失败，请手动全选第 8 步里的文字复制。";
    m.className = ok ? "msg" : "msg bad";
    if (!ok) $("resultOut").scrollIntoView({ behavior: "smooth" });
  }

  // ---------------------------------------------------------------- 1. environment
  function browserName(ua) {
    let m = ua.match(/Edg\/([\d.]+)/);
    if (m) return "Edge " + m[1];
    m = ua.match(/Chrome\/([\d.]+)/);
    if (m) return "Chrome " + m[1];
    m = ua.match(/Firefox\/([\d.]+)/);
    if (m) return "Firefox " + m[1];
    if (/Safari\//.test(ua)) return "Safari";
    return "未知浏览器";
  }

  async function detectEnv() {
    const ua = navigator.userAgent;
    const env = {
      browser: browserName(ua),
      userAgent: ua,
      secureContext: window.isSecureContext,
      webSerial: "serial" in navigator,
      webUSB: "usb" in navigator,
      cores: navigator.hardwareConcurrency || null,
      memoryGB: navigator.deviceMemory || null,
      screen: screen.width + "x" + screen.height + " @" + window.devicePixelRatio + "x",
      viewport: window.innerWidth + "x" + window.innerHeight,
      language: navigator.language,
      platform: navigator.platform,
    };
    try {
      if (navigator.userAgentData && navigator.userAgentData.getHighEntropyValues) {
        const hv = await navigator.userAgentData.getHighEntropyValues(["platform", "platformVersion", "fullVersionList"]);
        env.platform = hv.platform + " " + hv.platformVersion;
        if (hv.platform === "Windows") {
          const major = parseInt(hv.platformVersion, 10);
          env.windows = major >= 13 ? "Windows 11" : major > 0 ? "Windows 10" : "Windows 7/8";
        }
        if (hv.fullVersionList) env.fullVersions = hv.fullVersionList.map((b) => b.brand + " " + b.version).join(", ");
      }
    } catch (e) { /* keep navigator.platform */ }
    try {
      const c = document.createElement("canvas");
      const gl = c.getContext("webgl2");
      env.webgl2 = !!gl;
      if (gl) {
        const dbg = gl.getExtension("WEBGL_debug_renderer_info");
        env.gpu = dbg ? gl.getParameter(dbg.UNMASKED_RENDERER_WEBGL) : gl.getParameter(gl.RENDERER);
        env.gpuVendor = dbg ? gl.getParameter(dbg.UNMASKED_VENDOR_WEBGL) : gl.getParameter(gl.VENDOR);
        env.maxTextureSize = gl.getParameter(gl.MAX_TEXTURE_SIZE);
        const lose = gl.getExtension("WEBGL_lose_context");
        if (lose) lose.loseContext();
      }
    } catch (e) {
      env.webgl2 = false;
      env.webglError = errText(e);
    }
    // Policy probes: a blocked feature usually throws SecurityError here.
    if (env.webSerial) {
      try {
        const ports = await navigator.serial.getPorts();
        env.serialPolicy = "ok（已授权 " + ports.length + " 个）";
      } catch (e) { env.serialPolicy = errText(e); env.serialBlocked = true; }
    }
    if (env.webUSB) {
      try {
        const devs = await navigator.usb.getDevices();
        env.usbPolicy = "ok（已授权 " + devs.length + " 个）";
      } catch (e) { env.usbPolicy = errText(e); env.usbBlocked = true; }
    }
    R.env = env;
    renderEnv();
    scheduleRender();
  }

  function renderEnv() {
    const e = R.env;
    const rows = [
      ["浏览器", e.browser],
      ["系统", (e.windows ? e.windows + "，" : "") + e.platform],
      ["HTTPS 安全上下文", e.secureContext],
      ["Web Serial", e.webSerial, e.serialPolicy],
      ["WebUSB（备用方案）", e.webUSB, e.usbPolicy],
      ["WebGL2", e.webgl2],
      ["显卡", (e.gpu || "?") + (e.gpuVendor ? "（" + e.gpuVendor + "）" : "")],
      ["CPU 线程 / 内存", (e.cores || "?") + " 线程 / " + (e.memoryGB ? "≥" + e.memoryGB + " GB" : "?")],
      ["屏幕 / 窗口", e.screen + " / " + e.viewport],
      ["UA", e.userAgent],
    ];
    const table = $("envTable");
    table.replaceChildren();
    for (const [label, value, extra] of rows) {
      const tr = document.createElement("tr");
      const a = document.createElement("td");
      a.textContent = label;
      const b = document.createElement("td");
      if (typeof value === "boolean") {
        const s = document.createElement("span");
        s.className = value ? "good" : "badv";
        s.textContent = value ? "✓ 支持" : "✗ 不支持";
        b.appendChild(s);
      } else {
        b.textContent = value == null ? "?" : String(value);
      }
      if (extra) b.appendChild(document.createTextNode("　" + extra));
      tr.append(a, b);
      table.appendChild(tr);
    }
    if (!e.webSerial) chip("chip-env", "不支持 Web Serial，请换 Chrome 或 Edge", "bad");
    else if (e.serialBlocked) chip("chip-env", "Web Serial 可能被策略禁用", "bad");
    else if (!e.secureContext || !e.webgl2) chip("chip-env", "有问题，看下表", "warn");
    else chip("chip-env", "Web Serial 可用", "ok");
  }

  // ---------------------------------------------------------------- 2. GPU benchmark
  const VS = `#version 300 es
layout(location=0) in vec3 aPos;
layout(location=1) in vec3 aNormal;
layout(location=2) in vec3 iOffset;
layout(location=3) in vec3 iScale;
layout(location=4) in vec3 iColor;
uniform mat4 uViewProj;
uniform float uScroll;
uniform float uLoop;
uniform float uScrollOn;
out vec3 vNormal;
out vec3 vColor;
out vec3 vWorld;
void main() {
  vec3 off = iOffset;
  if (uScrollOn > 0.5) { off.z = mod(off.z + uScroll, uLoop) - uLoop + 20.0; }
  vec3 world = aPos * iScale + off;
  vWorld = world;
  vNormal = aNormal;
  vColor = iColor;
  gl_Position = uViewProj * vec4(world, 1.0);
}`;
  const FS = `#version 300 es
precision highp float;
in vec3 vNormal;
in vec3 vColor;
in vec3 vWorld;
uniform vec3 uLightDir;
uniform vec3 uCamPos;
uniform vec3 uFogColor;
out vec4 outColor;
void main() {
  vec3 n = normalize(vNormal);
  vec3 l = normalize(-uLightDir);
  float diff = max(dot(n, l), 0.0);
  vec3 v = normalize(uCamPos - vWorld);
  vec3 h = normalize(l + v);
  float spec = pow(max(dot(n, h), 0.0), 32.0) * 0.2;
  vec3 col = vColor * (0.35 + 0.75 * diff) + vec3(spec);
  float d = length(uCamPos - vWorld);
  float fog = clamp(1.0 - exp(-d * 0.006), 0.0, 1.0);
  outColor = vec4(mix(col, uFogColor, fog), 1.0);
}`;

  function cubeData() {
    const faces = [
      [[1, 0, 0], [[0.5, 0, -0.5], [0.5, 1, -0.5], [0.5, 1, 0.5], [0.5, 0, 0.5]]],
      [[-1, 0, 0], [[-0.5, 0, 0.5], [-0.5, 1, 0.5], [-0.5, 1, -0.5], [-0.5, 0, -0.5]]],
      [[0, 1, 0], [[-0.5, 1, -0.5], [-0.5, 1, 0.5], [0.5, 1, 0.5], [0.5, 1, -0.5]]],
      [[0, -1, 0], [[-0.5, 0, 0.5], [-0.5, 0, -0.5], [0.5, 0, -0.5], [0.5, 0, 0.5]]],
      [[0, 0, 1], [[0.5, 0, 0.5], [0.5, 1, 0.5], [-0.5, 1, 0.5], [-0.5, 0, 0.5]]],
      [[0, 0, -1], [[-0.5, 0, -0.5], [-0.5, 1, -0.5], [0.5, 1, -0.5], [0.5, 0, -0.5]]],
    ];
    const out = [];
    for (const [n, q] of faces) {
      for (const k of [0, 1, 2, 0, 2, 3]) out.push(q[k][0], q[k][1], q[k][2], n[0], n[1], n[2]);
    }
    return new Float32Array(out);
  }

  function rng(seed) {
    let a = seed >>> 0;
    return () => {
      a = (a + 0x6d2b79f5) >>> 0;
      let t = a;
      t = Math.imul(t ^ (t >>> 15), t | 1);
      t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
      return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
    };
  }

  function makeInstances(n, loop) {
    const r = rng(42);
    const data = new Float32Array(n * 9);
    for (let k = 0; k < n; k++) {
      const z = r() * loop;
      let x, w, h, d, c;
      if (k % 5 === 0) {
        x = (r() - 0.5) * 9; w = 0.6 + r() * 1.2; h = 0.5 + r(); d = 0.6 + r();
        c = [0.95, 0.55 + r() * 0.2, 0.1];
      } else {
        const side = r() < 0.5 ? -1 : 1;
        x = side * (9 + r() * 40); w = 4 + r() * 8; h = 4 + r() * 40; d = 4 + r() * 8;
        const g = 0.45 + r() * 0.4;
        c = [g * 0.9, g * 0.95, g];
      }
      data.set([x, 0, z, w, h, d, c[0], c[1], c[2]], k * 9);
    }
    return data;
  }

  const v3 = {
    sub: (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]],
    dot: (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2],
    cross: (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]],
    norm: (a) => { const l = Math.hypot(a[0], a[1], a[2]) || 1; return [a[0] / l, a[1] / l, a[2] / l]; },
  };
  function perspective(fovy, aspect, near, far) {
    const f = 1 / Math.tan(fovy / 2);
    const nf = 1 / (near - far);
    return [f / aspect, 0, 0, 0, 0, f, 0, 0, 0, 0, (far + near) * nf, -1, 0, 0, 2 * far * near * nf, 0];
  }
  function lookAt(eye, target, up) {
    const z = v3.norm(v3.sub(eye, target));
    const x = v3.norm(v3.cross(up, z));
    const y = v3.cross(z, x);
    return [x[0], y[0], z[0], 0, x[1], y[1], z[1], 0, x[2], y[2], z[2], 0, -v3.dot(x, eye), -v3.dot(y, eye), -v3.dot(z, eye), 1];
  }
  function mul(a, b) {
    const o = new Array(16);
    for (let c = 0; c < 4; c++) {
      for (let r = 0; r < 4; r++) {
        let s = 0;
        for (let k = 0; k < 4; k++) s += a[k * 4 + r] * b[c * 4 + k];
        o[c * 4 + r] = s;
      }
    }
    return o;
  }

  function compile(gl, type, src) {
    const s = gl.createShader(type);
    gl.shaderSource(s, src);
    gl.compileShader(s);
    if (!gl.getShaderParameter(s, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(s) || "shader compile failed");
    return s;
  }

  function measureRefresh() {
    return new Promise((resolve) => {
      const ts = [];
      const f = (t) => {
        ts.push(t);
        if (ts.length < 61) { requestAnimationFrame(f); return; }
        const d = [];
        for (let i = 1; i < ts.length; i++) d.push(ts[i] - ts[i - 1]);
        d.sort((a, b) => a - b);
        resolve(Math.round(1000 / d[Math.floor(d.length / 2)]));
      };
      requestAnimationFrame(f);
    });
  }

  function benchConfig(cfg) {
    return new Promise((resolve) => {
      const canvas = document.createElement("canvas");
      canvas.width = cfg.w;
      canvas.height = cfg.h;
      $("gpuHolder").replaceChildren(canvas);
      const gl = canvas.getContext("webgl2", { antialias: cfg.msaa, powerPreference: "high-performance" });
      if (!gl) { resolve({ name: cfg.name, error: "无法创建 WebGL2" }); return; }
      let prog;
      try {
        prog = gl.createProgram();
        gl.attachShader(prog, compile(gl, gl.VERTEX_SHADER, VS));
        gl.attachShader(prog, compile(gl, gl.FRAGMENT_SHADER, FS));
        gl.linkProgram(prog);
        if (!gl.getProgramParameter(prog, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(prog) || "link failed");
      } catch (e) { resolve({ name: cfg.name, error: errText(e) }); return; }

      const LOOP = 400;
      const cube = gl.createBuffer();
      gl.bindBuffer(gl.ARRAY_BUFFER, cube);
      gl.bufferData(gl.ARRAY_BUFFER, cubeData(), gl.STATIC_DRAW);

      const makeVao = (instances) => {
        const vao = gl.createVertexArray();
        gl.bindVertexArray(vao);
        gl.bindBuffer(gl.ARRAY_BUFFER, cube);
        gl.enableVertexAttribArray(0);
        gl.vertexAttribPointer(0, 3, gl.FLOAT, false, 24, 0);
        gl.enableVertexAttribArray(1);
        gl.vertexAttribPointer(1, 3, gl.FLOAT, false, 24, 12);
        const ib = gl.createBuffer();
        gl.bindBuffer(gl.ARRAY_BUFFER, ib);
        gl.bufferData(gl.ARRAY_BUFFER, instances, gl.STATIC_DRAW);
        for (let i = 0; i < 3; i++) {
          gl.enableVertexAttribArray(2 + i);
          gl.vertexAttribPointer(2 + i, 3, gl.FLOAT, false, 36, i * 12);
          gl.vertexAttribDivisor(2 + i, 1);
        }
        gl.bindVertexArray(null);
        return vao;
      };
      const vaoBoxes = makeVao(makeInstances(cfg.n, LOOP));
      const vaoGround = makeVao(new Float32Array([0, -0.1, -LOOP / 2 + 20, 240, 0.1, LOOP, 0.32, 0.33, 0.36]));

      const u = (name) => gl.getUniformLocation(prog, name);
      const eye = [0, 5, 14];
      const viewProj = mul(perspective(Math.PI / 3, cfg.w / cfg.h, 0.5, 600), lookAt(eye, [0, 2, -30], [0, 1, 0]));
      const fog = [0.74, 0.84, 0.95];
      gl.useProgram(prog);
      gl.uniformMatrix4fv(u("uViewProj"), false, new Float32Array(viewProj));
      gl.uniform1f(u("uLoop"), LOOP);
      gl.uniform3fv(u("uLightDir"), new Float32Array(v3.norm([-0.4, -1, -0.3])));
      gl.uniform3fv(u("uCamPos"), new Float32Array(eye));
      gl.uniform3fv(u("uFogColor"), new Float32Array(fog));
      gl.enable(gl.DEPTH_TEST);
      gl.viewport(0, 0, cfg.w, cfg.h);
      gl.clearColor(fog[0], fog[1], fog[2], 1);
      const uScroll = u("uScroll");
      const uScrollOn = u("uScrollOn");

      const frames = [];
      let start = null;
      let last = 0;
      let scroll = 0;
      const frame = (t) => {
        if (start === null) { start = t; last = t; }
        const dt = t - last;
        last = t;
        if (t - start > 500) frames.push(dt);
        scroll += dt * 0.04;
        gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
        gl.uniform1f(uScroll, scroll);
        gl.uniform1f(uScrollOn, 0);
        gl.bindVertexArray(vaoGround);
        gl.drawArraysInstanced(gl.TRIANGLES, 0, 36, 1);
        gl.uniform1f(uScrollOn, 1);
        gl.bindVertexArray(vaoBoxes);
        gl.drawArraysInstanced(gl.TRIANGLES, 0, 36, cfg.n);
        if (t - start < 4500) { requestAnimationFrame(frame); return; }
        const lose = gl.getExtension("WEBGL_lose_context");
        const s = frameStats(frames);
        if (lose) lose.loseContext();
        resolve(Object.assign({ name: cfg.name, resolution: cfg.w + "x" + cfg.h, boxes: cfg.n, msaa: cfg.msaa }, s));
      };
      requestAnimationFrame(frame);
    });
  }

  function frameStats(frames) {
    if (!frames.length) return { error: "没有采到帧（页面被切到后台了？）" };
    const sorted = frames.slice().sort((a, b) => a - b);
    const total = frames.reduce((a, b) => a + b, 0);
    return {
      avgFps: +(frames.length / (total / 1000)).toFixed(1),
      p95Ms: +sorted[Math.floor(sorted.length * 0.95)].toFixed(1),
      worstMs: +sorted[sorted.length - 1].toFixed(1),
      slowFramePct: +((100 * frames.filter((d) => d > 34).length) / frames.length).toFixed(1),
      frames: frames.length,
    };
  }

  async function runGpu() {
    const btn = $("gpuBtn");
    btn.disabled = true;
    const out = $("gpuOut");
    out.textContent = "测显示器刷新率…";
    chip("chip-gpu", "测试中", "");
    try {
      const refreshHz = await measureRefresh();
      const results = [];
      for (const cfg of [
        { name: "标准档", w: 1280, h: 720, n: 1500, msaa: false },
        { name: "高负载档", w: 1920, h: 1080, n: 6000, msaa: true },
      ]) {
        out.textContent = "显示器约 " + refreshHz + " Hz，正在跑" + cfg.name + "…";
        results.push(await benchConfig(cfg));
      }
      const std = results[0];
      const heavy = results[1];
      let verdict = "吃力";
      let level = "bad";
      if (std.avgFps >= 0.9 * refreshHz && heavy.avgFps >= 0.75 * refreshHz) { verdict = "充裕"; level = "ok"; }
      else if (std.avgFps >= 45) { verdict = "够用"; level = "ok"; }
      else if (std.avgFps >= 28) { verdict = "勉强"; level = "warn"; }
      R.gpu = { refreshHz, verdict, results };
      out.textContent =
        "显示器约 " + refreshHz + " Hz\n" +
        results.map((r) => r.error ? r.name + "：" + r.error
          : r.name + "（" + r.resolution + "，" + r.boxes + " 个物体" + (r.msaa ? "，抗锯齿" : "") + "）：" +
            r.avgFps + " fps，95% 帧在 " + r.p95Ms + " ms 内，最慢一帧 " + r.worstMs + " ms").join("\n") +
        "\n结论：" + verdict;
      chip("chip-gpu", verdict, level);
    } catch (e) {
      R.gpu = { error: errText(e) };
      out.textContent = "出错：" + errText(e);
      chip("chip-gpu", "出错", "bad");
    }
    btn.disabled = false;
    scheduleRender();
  }

  // ---------------------------------------------------------------- 3. network
  async function runNet() {
    const btn = $("netBtn");
    btn.disabled = true;
    const out = $("netOut");
    chip("chip-net", "测速中", "");
    const runs = [];
    try {
      for (let i = 0; i < 3; i++) {
        out.textContent = "第 " + (i + 1) + " 次下载…";
        const t0 = now();
        const res = await fetch("speedtest.bin", { cache: "no-store" });
        if (!res.ok) throw new Error("HTTP " + res.status);
        const buf = await res.arrayBuffer();
        const sec = (now() - t0) / 1000;
        runs.push({ MB: +(buf.byteLength / 1048576).toFixed(2), seconds: +sec.toFixed(2), Mbps: +((buf.byteLength * 8) / 1e6 / sec).toFixed(1) });
      }
      const mbps = runs.map((r) => r.Mbps).sort((a, b) => a - b)[1];
      const load20 = (20 * 8) / mbps;
      R.net = { runs, medianMbps: mbps, est20MBSeconds: +load20.toFixed(1) };
      out.textContent =
        runs.map((r, i) => "第 " + (i + 1) + " 次：" + r.MB + " MB 用了 " + r.seconds + " 秒（" + r.Mbps + " Mbps）").join("\n") +
        "\n中位数 " + mbps + " Mbps → 20 MB 的游戏首次加载约 " + load20.toFixed(1) + " 秒（之后有浏览器缓存）";
      chip("chip-net", mbps + " Mbps", load20 <= 15 ? "ok" : load20 <= 45 ? "warn" : "bad");
    } catch (e) {
      R.net = { runs, error: errText(e) };
      out.textContent = "出错：" + errText(e);
      chip("chip-net", "出错", "bad");
    }
    btn.disabled = false;
    scheduleRender();
  }

  // ---------------------------------------------------------------- 4. Web Serial
  const serialPanels = [];
  let panelSeq = 0;
  const hex4 = (n) => (n == null ? "????" : n.toString(16).padStart(4, "0"));

  function serialMsg(text, bad) {
    const el = $("serialMsg");
    el.textContent = text;
    el.className = bad ? "msg bad" : "msg";
  }

  class PortPanel {
    constructor(port) {
      this.port = port;
      this.idx = ++panelSeq;
      const info = port.getInfo ? port.getInfo() : {};
      this.vid = info.usbVendorId;
      this.pid = info.usbProductId;
      this.state = "new";
      this.ver = null;
      this.lines = 0;
      this.other = 0;
      this.stCount = 0;
      this.st = null;
      this.jp = 0;
      this.br = 0;
      this.rateWindow = [];
      this.peakRate = 0;
      this.opens = 0;
      this.disconnects = 0;
      this.reconnectSeconds = [];
      this.lastDisconnectAt = 0;
      this.errors = [];
      this.pingSeq = 0;
      this.pings = new Map();
      this.rtts = [];
      this.lostPings = 0;
      this.echo = { crash: false, gift: false };
      this.echoMismatch = 0;
      this.echoSamples = [];
      this.logLines = [];
      this.sendChain = Promise.resolve();
      this.keepReading = false;
      this.reader = null;
      this.dirty = true;
      this.buildDom();
    }

    buildDom() {
      const el = document.createElement("div");
      el.className = "port";
      el.innerHTML =
        '<h3><span class="name"></span><span class="chip ver">版本?</span><span class="chip st"></span></h3>' +
        '<div class="steer"><div class="steer-mid"></div><div class="steer-bar"></div></div>' +
        '<div class="stats"></div>' +
        '<div class="row"><button data-a="crash">发送 crash</button><button data-a="gift">发送 gift</button><button data-a="close">断开</button></div>' +
        '<pre class="log"></pre>';
      el.querySelector(".name").textContent = "板子 #" + this.idx + "（USB " + hex4(this.vid) + ":" + hex4(this.pid) + "）";
      el.querySelector('[data-a="crash"]').addEventListener("click", () => this.send("crash"));
      el.querySelector('[data-a="gift"]').addEventListener("click", () => this.send("gift"));
      el.querySelector('[data-a="close"]').addEventListener("click", () => this.close());
      this.el = el;
      $("serialPorts").appendChild(el);
    }

    setState(s) {
      this.state = s;
      const map = {
        connected: ["已连接", "ok"],
        disconnected: ["已断开，等待自动重连", "warn"],
        closed: ["已关闭", ""],
        failed: ["打开失败", "bad"],
        new: ["连接中", ""],
      };
      const [text, level] = map[s] || [s, ""];
      const c = this.el.querySelector(".chip.st");
      c.textContent = text;
      c.className = "chip st" + (level ? " " + level : "");
      this.dirty = true;
      updateSerialChip();
      scheduleRender();
    }

    log(line) {
      this.logLines.push(line);
      if (this.logLines.length > 14) this.logLines.shift();
      this.dirty = true;
    }

    err(e) {
      const t = errText(e);
      this.errors.push(t);
      if (this.errors.length > 20) this.errors.shift();
      this.log("! " + t);
    }

    async open() {
      try {
        await this.port.open({ baudRate: 115200 });
      } catch (e) {
        if (!/already open/i.test(errText(e))) {
          this.err(e);
          this.setState("failed");
          serialMsg("板子 #" + this.idx + " 打不开：" + errText(e) + "。如果别的标签页或程序正占用这块板，先关掉再试。", true);
          return;
        }
      }
      this.opens++;
      if (this.lastDisconnectAt) {
        const secs = +((Date.now() - this.lastDisconnectAt) / 1000).toFixed(1);
        this.reconnectSeconds.push(secs);
        this.log("— 已自动重连（断开 " + secs + " 秒）—");
        this.lastDisconnectAt = 0;
      }
      this.setState("connected");
      this.keepReading = true;
      this.readLoop();
      this.startPing();
    }

    async readLoop() {
      const decoder = new TextDecoder();
      let buf = "";
      while (this.keepReading && this.port.readable) {
        let reader;
        try { reader = this.port.readable.getReader(); } catch (e) { this.err(e); break; }
        this.reader = reader;
        try {
          for (;;) {
            const { value, done } = await reader.read();
            if (done) break;
            if (!value) continue;
            buf += decoder.decode(value, { stream: true });
            let i;
            while ((i = buf.indexOf("\n")) >= 0) {
              const line = buf.slice(0, i).replace(/\r$/, "");
              buf = buf.slice(i + 1);
              this.onLine(line);
            }
            if (buf.length > 2000) buf = "";
          }
        } catch (e) {
          // BreakError / FramingError / BufferOverrunError are recoverable; NetworkError = device lost.
          this.err(e);
        } finally {
          try { reader.releaseLock(); } catch (e) { /* ignore */ }
          this.reader = null;
        }
      }
    }

    onLine(line) {
      const t = now();
      this.lines++;
      this.rateWindow.push(t);
      while (this.rateWindow.length && t - this.rateWindow[0] > 2000) this.rateWindow.shift();
      this.peakRate = Math.max(this.peakRate, this.rateWindow.length / 2);
      if (line.startsWith("st:")) {
        const v = parseFloat(line.slice(3));
        if (!isNaN(v)) { this.st = v; this.stCount++; }
      } else if (line === "br") {
        this.br++;
      } else if (line === "jp") {
        this.jp++;
        this.log(line);
      } else if (line.startsWith("hi:")) {
        if (this.ver !== line.slice(3)) {
          this.ver = line.slice(3);
          this.el.querySelector(".chip.ver").textContent = this.ver.toUpperCase();
          updateSerialChip();
        }
      } else if (line.startsWith("echo:")) {
        this.onEcho(line.slice(5));
      } else {
        this.other++;
        this.log(line);
      }
      this.dirty = true;
    }

    onEcho(msg) {
      if (msg.startsWith("ping:")) {
        const t0 = this.pings.get(msg.slice(5));
        if (t0 !== undefined) {
          this.rtts.push(now() - t0);
          if (this.rtts.length > 30) this.rtts.shift();
          this.pings.delete(msg.slice(5));
          return;
        }
      } else if (msg === "crash" || msg === "gift") {
        this.echo[msg] = true;
        this.log("echo:" + msg + "（板子收到了）");
        scheduleRender();
        return;
      }
      // Anything else means the board read a partial or garbled line.
      this.echoMismatch++;
      if (this.echoSamples.length < 6) this.echoSamples.push(msg);
      this.log("echo:" + msg + "（不完整？）");
    }

    send(text) {
      this.sendChain = this.sendChain.then(async () => {
        if (!this.port.writable) { this.log("（没连上，没发出去：" + text + "）"); return; }
        const w = this.port.writable.getWriter();
        try {
          await w.write(new TextEncoder().encode(text + "\n"));
          if (!text.startsWith("ping:")) this.log("→ " + text);
        } catch (e) {
          this.err(e);
        } finally {
          try { w.releaseLock(); } catch (e) { /* ignore */ }
        }
      });
      return this.sendChain;
    }

    startPing() {
      this.stopPing();
      this.pingTimer = setInterval(() => {
        const t = now();
        for (const [k, t0] of this.pings) {
          if (t - t0 > 6000) { this.pings.delete(k); this.lostPings++; }
        }
        const seq = String(++this.pingSeq);
        this.pings.set(seq, t);
        this.send("ping:" + seq);
      }, 2000);
    }

    stopPing() {
      if (this.pingTimer) clearInterval(this.pingTimer);
      this.pingTimer = 0;
    }

    onDisconnect() {
      this.disconnects++;
      this.lastDisconnectAt = Date.now();
      this.keepReading = false;
      this.stopPing();
      this.log("— USB 断开，插回或刷完程序后会自动重连 —");
      this.setState("disconnected");
    }

    async close() {
      this.keepReading = false;
      this.stopPing();
      try { if (this.reader) await this.reader.cancel(); } catch (e) { /* ignore */ }
      await sleep(50);
      try { await this.port.close(); } catch (e) { /* ignore */ }
      this.lastDisconnectAt = 0;
      this.setState("closed");
    }

    render() {
      if (!this.dirty) return;
      this.dirty = false;
      const rate = this.rateWindow.length && now() - this.rateWindow[this.rateWindow.length - 1] < 2000 ? this.rateWindow.length / 2 : 0;
      const rtt = this.medianRtt();
      if (this.st != null) drawSteer(this.el.querySelector(".steer-bar"), this.st);
      this.el.querySelector(".stats").textContent =
        "转向 " + (this.st == null ? "—" : this.st) + "　·　" + rate.toFixed(1) + " 行/秒（峰值 " + this.peakRate.toFixed(1) + "）" +
        "　·　跳 " + this.jp + " 次　·　刹车消息 " + this.br + "\n" +
        "游戏→板子往返 " + (rtt == null ? "—" : rtt + " ms") + "　·　断开 " + this.disconnects + " / 重连 " + this.reconnectSeconds.length +
        "　·　其它输出 " + this.other + " 行";
      this.el.querySelector(".log").textContent = this.logLines.join("\n");
    }

    medianRtt() {
      if (!this.rtts.length) return null;
      const s = this.rtts.slice().sort((a, b) => a - b);
      return Math.round(s[Math.floor(s.length / 2)]);
    }

    summary() {
      return {
        board: this.idx,
        usb: hex4(this.vid) + ":" + hex4(this.pid),
        version: this.ver,
        state: this.state,
        lines: this.lines,
        peakLinesPerSec: +this.peakRate.toFixed(1),
        steerMsgs: this.stCount,
        lastSteer: this.st,
        jumps: this.jp,
        brakeMsgs: this.br,
        otherLines: this.other,
        rttMedianMs: this.medianRtt(),
        rttSamples: this.rtts.length,
        lostPings: this.lostPings,
        echoCrash: this.echo.crash,
        echoGift: this.echo.gift,
        echoMismatch: this.echoMismatch,
        echoSamples: this.echoSamples,
        opens: this.opens,
        disconnects: this.disconnects,
        reconnectSeconds: this.reconnectSeconds,
        errors: this.errors.slice(-10),
        recentLog: this.logLines.slice(-8),
      };
    }
  }

  function updateSerialChip() {
    const live = serialPanels.filter((p) => p.state === "connected");
    if (!serialPanels.length) { chip("chip-serial", "未连接", ""); return; }
    const vers = live.map((p) => (p.ver || "?").toUpperCase()).join("、");
    if (live.length) chip("chip-serial", live.length + " 块板已连接（" + vers + "）", "ok");
    else chip("chip-serial", "没有连着的板", "warn");
  }

  function addPort(port) {
    let p = serialPanels.find((x) => x.port === port);
    if (!p) {
      p = new PortPanel(port);
      serialPanels.push(p);
    }
    p.open();
    return p;
  }

  async function connectClicked() {
    if (!("serial" in navigator)) {
      serialMsg("这个浏览器不支持 Web Serial，请换 Chrome 或 Edge。", true);
      return;
    }
    try {
      const opts = $("serialNoFilter").checked ? undefined : { filters: [{ usbVendorId: MICROBIT_VID }] };
      const port = await navigator.serial.requestPort(opts);
      const existing = serialPanels.find((p) => p.port === port);
      if (existing && existing.state === "connected") { serialMsg("这块板已经连着了。"); return; }
      addPort(port);
      serialMsg("");
    } catch (e) {
      if (e && e.name === "NotFoundError") {
        serialMsg("没有选中板子。列表是空的话：检查 USB 线（有些线只能充电），或者勾上“显示所有串口”再试。");
      } else {
        R.serial.errors.push(errText(e));
        serialMsg("出错：" + errText(e), true);
      }
    }
    scheduleRender();
  }

  function initSerial() {
    if (!("serial" in navigator)) {
      chip("chip-serial", "浏览器不支持", "bad");
      $("serialConnectBtn").disabled = true;
      serialMsg("这个浏览器不支持 Web Serial，请换 Chrome 或 Edge。", true);
      return;
    }
    navigator.serial.addEventListener("disconnect", (e) => {
      const p = serialPanels.find((x) => x.port === e.target);
      if (p) p.onDisconnect();
    });
    navigator.serial.addEventListener("connect", (e) => {
      const port = e.target;
      let p = serialPanels.find((x) => x.port === port);
      if (!p) {
        const info = port.getInfo ? port.getInfo() : {};
        p = serialPanels.find((x) => x.state === "disconnected" && x.vid === info.usbVendorId && x.pid === info.usbProductId);
        if (p) p.port = port;
      }
      if (p) p.open();
      else addPort(port);
    });
    // Ports this site was already allowed to use reopen without a prompt.
    navigator.serial.getPorts().then((ports) => ports.forEach(addPort)).catch((e) => {
      R.serial.errors.push("getPorts: " + errText(e));
    });
  }

  // ---------------------------------------------------------------- 6. simulator
  // The simulator only creates its AudioContext when the user clicks ITS OWN play
  // button; it then sends request_flash. Flashing before that throws inside the
  // iframe ("Context must be pre-created from a user event") and wedges it until
  // reload. So we never flash until we have seen request_flash once ("activated").
  const sim = { frame: null, ready: false, activated: false, loadStart: 0, buf: "", waiters: [], readyWaiters: [], activationWaiters: [], logLines: [] };

  function simLog(line) {
    sim.logLines.push(line);
    if (sim.logLines.length > 60) sim.logLines.shift();
    const el = $("simLog");
    el.textContent = sim.logLines.join("\n");
    el.scrollTop = el.scrollHeight;
  }

  function simPost(msg) {
    if (sim.frame && sim.frame.contentWindow) sim.frame.contentWindow.postMessage(msg, SIM_ORIGIN);
  }
  function simFlash() {
    if (!PROGRAM) { simLog("测试程序还没加载好"); return false; }
    if (!sim.activated) {
      simLog("先点一下左边模拟器里的 ▶ 按钮（模拟器要求第一次由人来点）");
      $("simOut").textContent = "请先点模拟器里的 ▶ 按钮";
      return false;
    }
    simPost({ kind: "flash", filesystem: { "main.py": new TextEncoder().encode(PROGRAM) } });
    R.sim.flashedAt = new Date().toISOString();
    return true;
  }

  function waitSimActivated(ms) {
    if (sim.activated) return Promise.resolve(true);
    return new Promise((resolve) => {
      sim.activationWaiters.push(resolve);
      setTimeout(() => resolve(sim.activated), ms);
    });
  }
  const simSet = (id, value) => simPost({ kind: "set_value", id, value });
  const simSerial = (text) => simPost({ kind: "serial_input", data: text });

  function simLine(line) {
    R.sim.lines = (R.sim.lines || 0) + 1;
    if (line.startsWith("st:")) {
      const v = parseFloat(line.slice(3));
      if (!isNaN(v)) drawSteer($("simSteer"), v);
    } else if (line !== "br" && !line.startsWith("hi:")) {
      simLog(line);
    }
    for (const w of sim.waiters.slice()) {
      if (w.pred(line)) {
        sim.waiters.splice(sim.waiters.indexOf(w), 1);
        w.resolve(true);
      }
    }
  }

  function waitSimLine(pred, ms) {
    return new Promise((resolve) => {
      const w = { pred, resolve };
      sim.waiters.push(w);
      setTimeout(() => {
        const k = sim.waiters.indexOf(w);
        if (k >= 0) { sim.waiters.splice(k, 1); resolve(false); }
      }, ms);
    });
  }

  function waitSimReady(ms) {
    if (sim.ready) return Promise.resolve(true);
    return new Promise((resolve) => {
      sim.readyWaiters.push(resolve);
      setTimeout(() => resolve(sim.ready), ms);
    });
  }

  function initSim() {
    const f = $("simFrame");
    sim.frame = f;
    sim.loadStart = now();
    f.addEventListener("load", () => {
      R.sim.iframeLoaded = true;
      R.sim.iframeLoadMs = Math.round(now() - sim.loadStart);
      scheduleRender();
    });
    window.addEventListener("message", (e) => {
      if (!sim.frame || e.source !== sim.frame.contentWindow) return;
      const d = e.data || {};
      switch (d.kind) {
        case "ready":
          sim.ready = true;
          R.sim.ready = true;
          R.sim.readyMs = Math.round(now() - sim.loadStart);
          R.sim.stateKeys = d.state ? Object.keys(d.state) : [];
          chip("chip-sim", "已就绪", "ok");
          simLog("ready（" + R.sim.readyMs + " ms）：" + R.sim.stateKeys.join(", "));
          sim.readyWaiters.splice(0).forEach((r) => r(true));
          break;
        case "request_flash":
          sim.activated = true;
          R.sim.playClicked = true;
          simLog("request_flash（点了 ▶）→ 刷入测试程序");
          simFlash();
          sim.activationWaiters.splice(0).forEach((r) => r(true));
          break;
        case "serial_output": {
          sim.buf += String(d.data || "");
          let i;
          while ((i = sim.buf.indexOf("\n")) >= 0) {
            const line = sim.buf.slice(0, i).replace(/\r$/, "");
            sim.buf = sim.buf.slice(i + 1);
            simLine(line);
          }
          if (sim.buf.length > 2000) sim.buf = "";
          break;
        }
        case "state_change":
          R.sim.stateChanges = (R.sim.stateChanges || 0) + 1;
          break;
        case "internal_error": {
          const m = errText(d.error);
          R.sim.errors.push(m);
          simLog("internal_error：" + m);
          break;
        }
        case "radio_output":
          simLog("radio_output " + (d.data ? d.data.length : 0) + " 字节");
          break;
        default:
          break;
      }
      scheduleRender();
    });
    f.src = SIM_URL;
    setTimeout(() => {
      if (!sim.ready) {
        R.sim.readyTimeout = true;
        chip("chip-sim", "20 秒没就绪", "bad");
        scheduleRender();
      }
    }, 20000);
  }

  function fmtAuto(A) {
    const mark = (v) => (v === true ? "✓" : v === false ? "✗" : "…");
    return [
      "模拟器就绪　" + mark(A.ready),
      "点了 ▶　　 " + mark(A.playClicked),
      "程序在跑　　" + mark(A.programRuns),
      "倾斜→转向　" + mark(A.tiltWorks),
      "按 A → 跳　" + mark(A.buttonWorks),
      "游戏→模拟器消息　" + mark(A.serialInputWorks) + (A.serialRttMs ? "（往返 " + A.serialRttMs + " ms）" : ""),
      A.error ? "出错：" + A.error : "",
    ].filter(Boolean).join("\n");
  }

  async function simAutoTest() {
    const btn = $("simAutoBtn");
    btn.disabled = true;
    const A = (R.sim.auto = { startedAt: new Date().toISOString() });
    const out = $("simOut");
    const show = () => { out.textContent = fmtAuto(A); scheduleRender(); };
    try {
      show();
      A.ready = await waitSimReady(20000);
      show();
      if (!A.ready) return;
      if (sim.activated) {
        A.playClicked = true;
        simFlash();
      } else {
        out.textContent = fmtAuto(A) + "\n\n→ 请点左边模拟器里的 ▶ 按钮（90 秒内）";
        A.playClicked = await waitSimActivated(90000);
        show();
        if (!A.playClicked) return;
        // request_flash handler has already flashed the program.
      }
      A.programRuns = await waitSimLine((l) => l.startsWith("st:"), 15000);
      show();
      if (!A.programRuns) return;
      // Register each waiter before acting, so a fast reply can't slip past it.
      const tilt = waitSimLine((l) => l === "st:100", 3000);
      simSet("accelerometerX", 800);
      A.tiltWorks = await tilt;
      simSet("accelerometerX", 0);
      show();
      const jump = waitSimLine((l) => l === "jp", 3000);
      simSet("buttonA", 1);
      await sleep(150);
      simSet("buttonA", 0);
      A.buttonWorks = await jump;
      show();
      const echo = waitSimLine((l) => l === "echo:crash", 3000);
      const t0 = now();
      simSerial("crash\n");
      A.serialInputWorks = await echo;
      if (A.serialInputWorks) A.serialRttMs = Math.round(now() - t0);
      show();
    } catch (e) {
      A.error = errText(e);
    } finally {
      btn.disabled = false;
      show();
      const pass = A.ready && A.playClicked && A.programRuns && A.tiltWorks && A.buttonWorks && A.serialInputWorks;
      chip("chip-sim", pass ? "全部通过" : !A.ready ? "没就绪" : !A.playClicked ? "等你点 ▶" : "部分没通过", pass ? "ok" : A.ready && !A.playClicked ? "warn" : "bad");
    }
  }

  function initSimKeys() {
    const held = new Set();
    const handler = (down) => (e) => {
      if (!$("simKeys").checked || !sim.ready) return;
      const tag = (e.target && e.target.tagName) || "";
      if (tag === "INPUT" || tag === "TEXTAREA") return;
      const k = e.key.length === 1 ? e.key.toLowerCase() : e.key;
      if (down && held.has(k)) { if (k.startsWith("Arrow")) e.preventDefault(); return; }
      if (down) held.add(k); else held.delete(k);
      switch (k) {
        case "ArrowLeft":
          simSet("accelerometerX", down ? -700 : held.has("ArrowRight") ? 700 : 0);
          e.preventDefault();
          break;
        case "ArrowRight":
          simSet("accelerometerX", down ? 700 : held.has("ArrowLeft") ? -700 : 0);
          e.preventDefault();
          break;
        case "a": simSet("buttonA", down ? 1 : 0); break;
        case "b": simSet("buttonB", down ? 1 : 0); break;
        case "m": simSet("soundLevel", down ? 200 : 10); break;
        case "l": simSet("pinLogo", down ? 1 : 0); break;
        default: break;
      }
    };
    window.addEventListener("keydown", handler(true));
    window.addEventListener("keyup", handler(false));
  }

  // ---------------------------------------------------------------- 7. manual checks
  const MANUAL = [
    ["v1_steer", "V1：倾斜板子时，转向条会跟着动"],
    ["v2_steer", "V2：倾斜板子时，转向条会跟着动"],
    ["v1_face", "V1：点“发送 crash”后板子显示哭脸"],
    ["v2_face_sound", "V2：点“发送 crash”后显示哭脸，而且有声音"],
    ["two_boards", "两块板同时连在这台电脑上都正常"],
    ["conflict_receive", "python.microbit.org 连着同一块板时，本页还在收数据"],
    ["conflict_flash", "本页连着板子时，python.microbit.org 能正常刷程序"],
    ["conflict_reconnect", "刷完程序后本页自己恢复收数据（不用再点连接）"],
    ["sim_visible", "模拟器里能看到 micro:bit 板子"],
    ["sim_sound", "模拟器收到 crash 时有声音（没声音就先点一下模拟器里的板子再试）"],
    ["student_account", "这次是用学生账号登录测的"],
    ["edge_too", "Edge 也测过（可选）"],
  ];

  function renderManual() {
    const box = $("manualList");
    box.replaceChildren();
    for (const [id, text] of MANUAL) {
      const label = document.createElement("label");
      const cb = document.createElement("input");
      cb.type = "checkbox";
      cb.checked = !!R.manual[id];
      cb.addEventListener("change", () => {
        R.manual[id] = cb.checked;
        saveStore();
        scheduleRender();
      });
      const span = document.createElement("span");
      span.textContent = text;
      label.append(cb, span);
      box.appendChild(label);
    }
  }

  // ---------------------------------------------------------------- init
  async function loadProgram() {
    try {
      const r = await fetch("labcheck.py", { cache: "no-store" });
      if (!r.ok) throw new Error("HTTP " + r.status);
      PROGRAM = await r.text();
      $("progText").textContent = PROGRAM;
    } catch (e) {
      $("progText").textContent = "测试程序加载失败：" + errText(e);
      R.pageErrors.push("labcheck.py: " + errText(e));
    }
  }

  function init() {
    const st = loadStore();
    R.pcLabel = st.pcLabel || "";
    R.manual = st.manual || {};
    R.notes = st.notes || "";
    $("pcLabel").value = R.pcLabel;
    $("notes").value = R.notes;
    $("pcLabel").addEventListener("input", (e) => { R.pcLabel = e.target.value.trim(); saveStore(); scheduleRender(); });
    $("notes").addEventListener("input", (e) => { R.notes = e.target.value; saveStore(); scheduleRender(); });
    $("copyBtn").addEventListener("click", copyResults);
    $("copyBtn2").addEventListener("click", copyResults);
    $("copyProgBtn").addEventListener("click", async (e) => {
      const ok = await copyText(PROGRAM);
      e.target.textContent = ok ? "已复制" : "复制失败";
      setTimeout(() => { e.target.textContent = "复制代码"; }, 1500);
    });
    $("gpuBtn").addEventListener("click", runGpu);
    $("netBtn").addEventListener("click", runNet);
    $("serialConnectBtn").addEventListener("click", connectClicked);
    $("simAutoBtn").addEventListener("click", simAutoTest);
    $("simRunBtn").addEventListener("click", () => { if (sim.ready) simFlash(); else simLog("模拟器还没就绪"); });
    $("simCrashBtn").addEventListener("click", () => { if (sim.activated) simSerial("crash\n"); else simFlash(); });
    renderManual();
    initSerial();
    initSimKeys();
    loadProgram().then(initSim);
    detectEnv();
    setInterval(() => {
      for (const p of serialPanels) p.render();
    }, 250);
    scheduleRender();
  }

  init();
})();
