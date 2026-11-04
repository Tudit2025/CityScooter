#!/usr/bin/env node
// =============================================================================
//  CityScooter 远景天际线生成器（Perth CBD / Rockingham）
//  -----------------------------------------------------------------------------
//  用法（在 art-src/skyline/ 目录下）：
//    npm ci                               # 第一次：按 package-lock.json 安装 resvg（版本已锁定）
//    node generator.mjs                   # 生成全部场景 → out/
//    node generator.mjs --scene perth     # 只生成 Perth
//    node generator.mjs --seed 42         # 换随机种子（楼的高矮/位置/细节会变，地标位置不变）
//    node generator.mjs --svg-only        # 只输出 SVG（快，适合调参）
//    node generator.mjs --verify          # 额外做“左右无缝”像素级自检
//    node generator.mjs --seed 7 --out .tmp/seed7   # 试别的种子，输出到别的目录（不覆盖 out/；.tmp 已被 gitignore）
//    node generator.mjs --debug-shift .tmp/dbg      # 调试用：写出“平移半幅”前后的各层 SVG，排查接缝问题
//
//  设计要点：
//  * 所有输出都是确定性的：同样的参数 + 同样的 seed → 逐字节相同的 SVG / PNG。
//  * 每一层都可以水平无限平铺：连续地形用“整数周期”的正弦叠加；
//    跨越左右边缘的物体会自动在另一侧再画一份（见 Layer.body）。
//  * 参数都集中在文件最上面（CONFIG / PERTH / ROCKINGHAM），技术美术一般只需要改这里。
//  * 所有“像素”类参数都以 1024 高的画布为基准，改画布尺寸会等比缩放。
// =============================================================================

import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import { fileURLToPath } from 'node:url';
import { Resvg } from '@resvg/resvg-js';

const HERE = path.dirname(fileURLToPath(import.meta.url));

// =============================================================================
// 1. 全局参数
// =============================================================================
const CONFIG = {
  seed: 20261109,                         // 随机种子（首次上课日期）。同一个种子 → 完全相同的输出
  canvas: { width: 4096, height: 1024 },  // 母版尺寸。宽必须是 4 的倍数（无缝自检会平移半个画布）
  exports: [                              // 每张 PNG 导出的尺寸
    { suffix: '', width: 4096 },          //   母版 4096x1024
    { suffix: '-2k', width: 2048 },       //   Web 版 2048x512（WebGL 内存预算）
  ],
  outDir: 'out',                          // 输出目录（相对本文件）
  bleedAlpha: true,                       // 透明像素的 RGB 用邻近颜色填充，避免引擎里出现黑边
  tiledCheckWidth: 2048,                  // 拼接检查图用哪一档分辨率（两张并排）
};

// =============================================================================
// 2. 场景参数：Perth（珀斯 CBD，站在南岸 South Perth 隔着天鹅河往北看）
//    横向位置 x 都是 0~1 的比例（0 = 左边缘，1 = 右边缘）。左=西，右=东。
// =============================================================================
const PERTH = {
  title: 'Perth CBD',
  horizon: 0.845,          // 水面线（对岸岸线）所在高度，占画布高度的比例
  sampleStep: 2,           // 连续地形的采样步长（像素）。必须能整除 画布宽/2
  palette: {
    // 天空竖直渐变：[位置(0=顶,1=底), 颜色]。下午的珀斯：顶部晴空蓝 → 地平线暖米色
    sky: [[0.00, '#77b2dc'], [0.36, '#a3cde6'], [0.60, '#cde4ea'], [0.76, '#ebe9d8'], [0.85, '#f8e1bd'], [1.00, '#f4d3a5']],
    haze: '#d0e0e8',       // 大气透视色：越远的层越往这个颜色混（见各层的 haze）
    hills: '#5a7a99',      // Darling Range 远山
    park: '#3a5d52',       // Kings Park 树林
    building: '#2e4f6f',   // 楼宇暗面（偏蓝）
    buildingLit: '#d4c0a2',// 楼宇受光面（下午西晒 → 左侧受光，暖沙色）
    window: '#eef4f6',     // 窗带 / 竖梃高光
    ground: '#3a5365',     // 中景地面、低矮街区
    tree: '#294a40',       // 树（中景/近景）
    foreshore: '#1d3343',  // 近景岸线
    water: '#2c7aa8',      // 河水（靠岸）
    waterDeep: '#1a5580',  // 河水（画面底部）
    waterHi: '#e2f2f7',    // 水面波光
    copper: '#a2552d',     // 钟楼铜帆（背光）
    copperLit: '#e0945b',  // 钟楼铜帆（受光）
    glass: '#a8d9e4',      // 玻璃（钟楼尖塔）
    white: '#f7f5ee',      // Spanda 雕塑、白色桥梁
    bronze: '#8a5d3b',     // 体育场古铜色立面（暗）
    bronzeLit: '#d4a271',  // 体育场立面鳍片（亮）
    arena: ['#28455f', '#46677f', '#7896aa', '#a9bfcb'],  // Perth Arena 多面体的几种明暗
    swan: '#0f1216',       // 黑天鹅
    beak: '#d9412f',       // 黑天鹅红嘴
  },

  // ---------------- 远景层 far：Darling Range 远山 + Kings Park 山脊 + 远处楼群 ----------------
  far: {
    haze: 0.60,            // 整层往 haze 色混合的比例（0=不混，1=完全变成雾色）
    groundHaze: 0.18,      // 底部额外的雾（越往下越淡，模拟地面雾气）
    pxPerM: 2.0,           // 尺度：每米多少像素（1024 高画布）
    darling: { height: 128, base: 4, eastX: 0.84, eastBoost: 0.62, kMin: 2, kMax: 16 },
    // Kings Park（Mount Eliza）：x0 西侧坡脚 → x1 台地起点 → x2 悬崖边 → x3 东侧坡脚
    kingsPark: { x0: 0.985, x1: 0.050, x2: 0.150, x3: 0.188, height: 140, canopy: 1.0, haze: -0.16 },
    warMemorial: { x: 0.140, scale: 1.15 },  // 州战争纪念碑（方尖碑）
    dnaTower: { x: 0.088, scale: 1.15 },     // DNA 塔
    // 远处楼群分区：count=楼数（乘 density），h/w=高/宽范围（米），peak/spread=高楼集中的位置/范围
    zones: [
      { name: 'cbdBack', x0: 0.270, x1: 0.655, count: 20, h: [55, 150], w: [22, 44], peak: 0.47, spread: 0.13 },
      { name: 'northbridge', x0: 0.195, x1: 0.285, count: 6, h: [22, 64], w: [18, 34], peak: 0.27, spread: 0.08 },
      { name: 'burswood', x0: 0.800, x1: 0.950, count: 5, h: [20, 60], w: [22, 40], peak: 0.86, spread: 0.08 },
    ],
    density: 1.0,
  },

  // ---------------- 中景层 mid：CBD 楼群 + 地标 ----------------
  mid: {
    haze: 0.22,
    groundHaze: 0.24,
    pxPerM: 2.35,
    baseY: 0.832,          // 楼宇地面线（会被近景岸线挡住）
    landmarks: {           // x = 横向位置；scale = 大小倍数
      perthArena:   { x: 0.268, scale: 1.0 },
      qv1:          { x: 0.336, scale: 1.0 },
      brookfield:   { x: 0.393, scale: 1.0 },
      centralPark:  { x: 0.470, scale: 1.0 },
      matagarup:    { x: 0.771, scale: 1.0 },
      optusStadium: { x: 0.890, scale: 1.0 },
    },
    zones: [
      { name: 'westPerth', x0: 0.180, x1: 0.300, count: 7,  h: [26, 88],  w: [18, 34], peak: 0.30, spread: 0.08, row: 'back' },
      { name: 'cbdBack',   x0: 0.300, x1: 0.640, count: 15, h: [80, 185], w: [22, 42], peak: 0.47, spread: 0.12, row: 'back' },
      { name: 'cbdFront',  x0: 0.305, x1: 0.650, count: 11, h: [30, 100], w: [20, 40], peak: 0.47, spread: 0.18, row: 'front' },
      { name: 'eastPerth', x0: 0.640, x1: 0.735, count: 9,  h: [24, 80],  w: [18, 32], peak: 0.66, spread: 0.06, row: 'back', tone: 'light' },
    ],
    density: 1.0,          // 楼群密度总倍数（1.2 = 多 20% 的楼）
    fabric: { h: [4, 11], w: [12, 36], treeChance: 0.45 },  // 地面低矮街区（米）
  },

  // ---------------- 近景层 near：河岸线 + Elizabeth Quay + 钟楼 + 黑天鹅 ----------------
  near: {
    haze: 0.05,
    groundHaze: 0.10,
    pxPerM: 2.9,
    shore: { height: 10, gaps: [[0.418, 0.486], [0.712, 0.832]] },  // 岸线高度(px)；gaps = 开阔水面（EQ 内湾、东边河面）
    trees: { density: 1.0, h: [12, 40], palmChance: 0.22, gumChance: 0.3 },
    spanda:    { x: 0.398, scale: 1.15 },  // Spanda 白色椭圆环雕塑
    eqBridge:  { x: 0.452, scale: 1.0 },   // Elizabeth Quay 步行桥
    bellTower: { x: 0.537, scale: 1.18 },  // 天鹅钟楼 Bell Tower
    ferry:     { x: 0.585, scale: 1.0 },   // 渡轮（无字无标志）
    swans: [                                // 黑天鹅：dy = 离水面线的像素距离
      { x: 0.292, dy: 54, s: 1.15, dir: 1 },
      { x: 0.304, dy: 70, s: 0.95, dir: 1 },
      { x: 0.655, dy: 88, s: 1.15, dir: -1 },
    ],
    shimmer: { count: 170 },               // 水面波光条数
  },
};

// =============================================================================
// 3. 场景参数：Rockingham（学校所在地；站在 Rockingham Beach 前滩草坪上往西北看海）
//    近景：草坪 + 沙滩 + 诺福克松（Norfolk Island pine）+ 海边小楼
//    中景：近海 + 木栈桥 + 停泊的帆船
//    远景：海平线上低低的 Garden Island、Point Peron 岬角、Penguin Island
// =============================================================================
const ROCKINGHAM = {
  title: 'Rockingham Beach',
  horizon: 0.705,          // 海平线高度（占画布高度比例）
  sampleStep: 2,
  palette: {
    sky: [[0.00, '#6aacdd'], [0.34, '#9acbea'], [0.56, '#c8e3ee'], [0.67, '#e7eee3'], [0.72, '#f5e8cc'], [1.00, '#f3dbb3']],
    haze: '#d4e4ec',
    seaFar: '#79b6cb',     // 远海
    sea: '#2a93b4',        // 近海（Cockburn Sound 的蓝绿色）
    seaDeep: '#1d7397',
    seaShallow: '#5ec2c4', // 沙底浅水
    seaHi: '#effafb',      // 波光 / 浪花
    sandWet: '#cbb38a',
    sand: '#ead5ab',
    sandDry: '#f1e1bd',
    grass: '#6b9748',      // 前滩草坪
    path: '#ddd5c2',       // 海滨步道
    land: '#6a8463',       // 远处岛屿的灌木
    limestone: '#e6d6b0',  // 石灰岩崖
    pine: '#21402f',       // 诺福克松（暗）
    pineLit: '#4d7a55',    // 诺福克松（受光）
    trunk: '#5a4c40',
    tree: '#35583b',       // 白千层 / 胡椒薄荷树等矮树
    wall: '#efe9da',       // 海边小楼墙面
    wallShade: '#b9c3c7',
    roof: '#a65b40',       // 坡屋顶
    awning: '#3a8c88',     // 遮阳篷（无字）
    glass: '#4d6c84',
    jetty: '#594a3c',      // 木栈桥（暗）
    jettyLit: '#a58e70',   // 木栈桥（亮）
    hull: '#f4f4ef',
    sail: '#fbfaf5',
    mast: '#8f999f',
    ship: '#6b7a86',
    gull: '#fbfbf8',
  },
  far: {
    haze: 0.5, groundHaze: 0.1,
    gardenIsland: { x0: 0.020, x1: 0.400, height: 24 },  // 长条低岛
    causeway: { x0: 0.400, x1: 0.505 },                  // Garden Island 堤道（细线）
    pointPeron: { x0: 0.505, x1: 0.655, height: 52 },    // Point Peron 岬角（右端是石灰岩崖）
    penguinIsland: { x: 0.737, w: 0.054, height: 50 },   // Penguin Island（左侧陡崖）
    rocks: [[0.792, 0.009, 10], [0.806, 0.006, 6], [0.817, 0.004, 4]], // [x, 宽, 高px] 小礁石
    farCoast: { x0: 0.865, x1: 1.005, height: 9 },       // 远处海湾东岸（很淡）
    ships: [{ x: 0.905, s: 1.0 }],                       // 远洋货轮（无字无标志）
    sails: [{ x: 0.262, s: 1.0 }, { x: 0.285, s: 0.8 }, { x: 0.468, s: 0.9 }, { x: 0.688, s: 0.85 }],
    shimmer: { count: 90 },
  },
  mid: {
    haze: 0.16,
    jetty: { x0: 0.350, x1: 0.560, deckY: 0.800 },      // 木栈桥（右端是 T 形平台 + 小亭子）
    boats: [
      { x: 0.115, type: 'yacht', s: 1.0 },
      { x: 0.200, type: 'yacht', s: 0.85, sail: true },
      { x: 0.625, type: 'motor', s: 1.0 },
      { x: 0.805, type: 'yacht', s: 0.9 },
      { x: 0.935, type: 'yacht', s: 1.1, sail: true },
    ],
    shimmer: { count: 130 },
  },
  near: {
    haze: 0.03,
    waterline: 0.868,      // 沙滩上的水线
    grassY: 0.93,          // 草坪起点
    pines: { count: 17, h: [260, 560], keepOut: [[0.737, 0.032], [0.556, 0.022], [0.58, 0.02]] },  // keepOut: [x, 半宽] 不种松树
    trees: { count: 26, h: [32, 66] },
    buildings: [           // 海边小楼：w/h 为像素（1024 高画布）
      { x: 0.085, w: 184, h: 92, floors: 3, roof: 'flat' },
      { x: 0.430, w: 136, h: 64, floors: 2, roof: 'pitched' },
      { x: 0.640, w: 214, h: 116, floors: 4, roof: 'flat' },
      { x: 0.885, w: 152, h: 76, floors: 3, roof: 'pitched' },
    ],
    shelters: [{ x: 0.300 }, { x: 0.515 }, { x: 0.775 }],
    gulls: [                // 海鸥：y 为画布高度比例
      { x: 0.240, y: 0.42, s: 1.2 }, { x: 0.258, y: 0.45, s: 0.95 }, { x: 0.585, y: 0.36, s: 1.1 },
      { x: 0.712, y: 0.49, s: 0.9 }, { x: 0.722, y: 0.47, s: 0.75 }, { x: 0.905, y: 0.40, s: 1.0 },
    ],
  },
};

// =============================================================================
// 4. 基础工具：随机数 / 颜色 / 数学 / SVG 路径
// =============================================================================
function mulberry32(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6D2B79F5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}
function hashStr(s) {
  let h = 2166136261;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619); }
  return h >>> 0;
}
// 每个用途单独一个随机流（seed + 标签），这样改一个分区的参数不会打乱其它分区
class Rng {
  constructor(seed, tag) { this.r = mulberry32((seed ^ hashStr(tag)) >>> 0); }
  f() { return this.r(); }
  range(a, b) { return a + (b - a) * this.r(); }
  int(a, b) { return a + Math.floor((b - a + 1) * this.r()); }
  pick(arr) { return arr[Math.floor(this.r() * arr.length)]; }
  chance(p) { return this.r() < p; }
  weighted(table) { // [[value, weight], ...]
    const total = table.reduce((s, t) => s + t[1], 0);
    let x = this.r() * total;
    for (const [v, w] of table) { if ((x -= w) < 0) return v; }
    return table[table.length - 1][0];
  }
}

const clamp = (v, a, b) => Math.max(a, Math.min(b, v));
const lerp = (a, b, t) => a + (b - a) * t;
const smoothstep = (a, b, x) => { const t = clamp((x - a) / (b - a), 0, 1); return t * t * (3 - 2 * t); };
const mod = (a, n) => ((a % n) + n) % n;
// 环形距离：x 相对 c 的有符号距离（考虑左右平铺）
function wrapDelta(x, c, W) { let d = mod(x - c, W); if (d > W / 2) d -= W; return d; }

function hexToRgb(h) { const s = h.replace('#', ''); return [0, 2, 4].map(i => parseInt(s.slice(i, i + 2), 16)); }
function rgbToHex(c) { return '#' + c.map(v => clamp(Math.round(v), 0, 255).toString(16).padStart(2, '0')).join(''); }
function mix(a, b, t) { const A = hexToRgb(a), B = hexToRgb(b); return rgbToHex(A.map((v, i) => v + (B[i] - v) * t)); }

// 整数周期噪声：在 [0,W] 上严格周期，保证左右无缝。返回值约在 [-1,1]
function periodicNoise(rng, W, kMin, kMax, falloff = 1) {
  const terms = [];
  for (let k = kMin; k <= kMax; k++) terms.push({ k, a: Math.pow(k, -falloff) * rng.range(0.4, 1), p: rng.range(0, Math.PI * 2) });
  const norm = terms.reduce((s, t) => s + t.a, 0) * 0.7;
  return (x) => { let v = 0; for (const t of terms) v += t.a * Math.sin(2 * Math.PI * t.k * x / W + t.p); return clamp(v / norm, -1, 1); };
}

// --- SVG 数字/路径 ---
function fmt(n) { const r = Math.round(n * 100) / 100; return String(r === 0 ? 0 : r); }
function poly(pts) { return 'M' + pts.map(p => fmt(p[0]) + ' ' + fmt(p[1])).join('L') + 'Z'; }
function polyline(pts) { return 'M' + pts.map(p => fmt(p[0]) + ' ' + fmt(p[1])).join('L'); }
function rectP(x, y, w, h) { return `M${fmt(x)} ${fmt(y)}h${fmt(w)}v${fmt(h)}h${fmt(-w)}Z`; }
function ellipseP(cx, cy, rx, ry) {
  return `M${fmt(cx - rx)} ${fmt(cy)}a${fmt(rx)} ${fmt(ry)} 0 1 0 ${fmt(2 * rx)} 0a${fmt(rx)} ${fmt(ry)} 0 1 0 ${fmt(-2 * rx)} 0Z`;
}
const circleP = (cx, cy, r) => ellipseP(cx, cy, r, r);
// Catmull-Rom 曲线（开放）：返回从 pts[0] 出发的 "C..." 段（不含 M），用于有机曲线
function smoothSeg(pts, tension = 0.5) {
  let d = '';
  const n = pts.length, g = (i) => pts[clamp(i, 0, n - 1)];
  for (let i = 0; i < n - 1; i++) {
    const p0 = g(i - 1), p1 = g(i), p2 = g(i + 1), p3 = g(i + 2);
    const k = tension / 3;
    d += `C${fmt(p1[0] + (p2[0] - p0[0]) * k)} ${fmt(p1[1] + (p2[1] - p0[1]) * k)} ` +
         `${fmt(p2[0] - (p3[0] - p1[0]) * k)} ${fmt(p2[1] - (p3[1] - p1[1]) * k)} ${fmt(p2[0])} ${fmt(p2[1])}`;
  }
  return d;
}
const mv = (p) => `M${fmt(p[0])} ${fmt(p[1])}`;
const ln = (p) => `L${fmt(p[0])} ${fmt(p[1])}`;
const pathEl = (d, fill, extra = '') => `<path d="${d}" fill="${fill}"${extra}/>`;
const strokeEl = (d, color, w, extra = '') => `<path d="${d}" fill="none" stroke="${color}" stroke-width="${fmt(w)}"${extra}/>`;

// 本地坐标系（单位：米，原点在底部中心，y 向上）→ 画布像素
function frame(cx, baseY, s) {
  return {
    s, X: (x) => cx + x * s, Y: (y) => baseY - y * s,
    P: (pts) => pts.map(([x, y]) => [cx + x * s, baseY - y * s]),
  };
}
// 三次贝塞尔求值
function bez(p0, p1, p2, p3, t) {
  const m = 1 - t, a = m * m * m, b = 3 * m * m * t, c = 3 * m * t * t, d = t * t * t;
  return [a * p0[0] + b * p1[0] + c * p2[0] + d * p3[0], a * p0[1] + b * p1[1] + c * p2[1] + d * p3[1]];
}
// 把一条中心线加粗成多边形（宽度可变）：pts 为画布坐标，widthAt(i/(n-1)) 返回像素宽
function ribbon(pts, widthAt) {
  const L = [], R = [], n = pts.length;
  for (let i = 0; i < n; i++) {
    const a = pts[Math.max(0, i - 1)], b = pts[Math.min(n - 1, i + 1)];
    let tx = b[0] - a[0], ty = b[1] - a[1]; const len = Math.hypot(tx, ty) || 1; tx /= len; ty /= len;
    const w = widthAt(i / (n - 1)) / 2;
    L.push([pts[i][0] - ty * w, pts[i][1] + tx * w]); R.push([pts[i][0] + ty * w, pts[i][1] - tx * w]);
  }
  return poly([...L, ...R.reverse()]);
}

// =============================================================================
// 5. 图层：负责收集图元并处理左右无缝平铺
// =============================================================================
class Layer {
  constructor(S, name) {
    this.S = S; this.name = name; this.id = `${S.key}-${name}`;
    this.W = S.W; this.H = S.H;
    this.items = []; this.defs = new Map();
  }
  // 普通图元：x0/x1 为它在画布上的横向范围（要包含描边），跨边缘时会自动在另一侧复制
  add(z, x0, x1, svg) {
    const m = 4 * this.S.u; // 安全边距：抗锯齿 / 描边 / 曲线控制点略超出声明范围也不会漏掉副本
    if (svg) this.items.push({ kind: 'item', z, x0: x0 - m, x1: x1 + m, svg, n: this.items.length });
  }
  // 整幅连续图元（地形、水面）：fn(shift) 返回 SVG，内部必须使用周期函数
  addPeriodic(z, fn) { this.items.push({ kind: 'periodic', z, fn, n: this.items.length }); }
  // 竖直渐变材质（只沿 y 变化 → 平铺无缝）。返回 fill 字符串
  mat(name, cTop, cBot, yTop, yBot) {
    if (cBot == null || cTop === cBot) return cTop;
    const id = `${this.id}-${name}`;
    if (!this.defs.has(id)) {
      this.defs.set(id, `<linearGradient id="${id}" gradientUnits="userSpaceOnUse" x1="0" y1="${fmt(yTop)}" x2="0" y2="${fmt(yBot)}">` +
        `<stop offset="0" stop-color="${cTop}"/><stop offset="1" stop-color="${cBot}"/></linearGradient>`);
    }
    return `url(#${id})`;
  }
  defsSvg() { return [...this.defs.values()].join(''); }
  // shift：整体向右平移的像素数（无缝自检用）。正常输出 shift = 0
  body(shift = 0) {
    const W = this.W, out = [];
    const items = [...this.items].sort((a, b) => (a.z - b.z) || (a.n - b.n));
    for (const it of items) {
      if (it.kind === 'periodic') { out.push(it.fn(shift)); continue; }
      const kMin = Math.floor(-(it.x1 + shift) / W) + 1;
      const kMax = Math.ceil((W - it.x0 - shift) / W) - 1;
      for (let k = kMin; k <= kMax; k++) {
        const dx = shift + k * W;
        out.push(dx === 0 ? it.svg : `<g transform="translate(${fmt(dx)} 0)">${it.svg}</g>`);
      }
    }
    return out.join('\n');
  }
}

function svgDoc(W, H, defs, body, comment = '') {
  return `<?xml version="1.0" encoding="UTF-8"?>\n${comment ? `<!-- ${comment} -->\n` : ''}` +
    `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}" viewBox="0 0 ${W} ${H}">\n` +
    (defs ? `<defs>${defs}</defs>\n` : '') + body + '\n</svg>\n';
}

// 连续地形：f(x) 返回该处顶部 y（周期函数），向下填充到 yBottom
function terrainPath(S, f, shift, yBottom) {
  const step = S.p.sampleStep, pts = [];
  for (let x = -2 * step; x <= S.W + 2 * step; x += step) pts.push([x, f(x - shift)]);
  pts.push([S.W + 2 * step, yBottom], [-2 * step, yBottom]);
  return poly(pts);
}

// =============================================================================
// 6. 形状库：楼宇 / 树 / 小物件
// =============================================================================

// 楼顶轮廓 prof：[[x, yTop], ...]，x 单调不减，从左边缘到右边缘
function profPoly(prof, baseY) { return [[prof[0][0], baseY], ...prof, [prof[prof.length - 1][0], baseY]]; }
function clipProf(prof, xa, xb) {
  const out = [];
  const push = (p) => { const l = out[out.length - 1]; if (!l || l[0] !== p[0] || l[1] !== p[1]) out.push(p); };
  for (let i = 0; i < prof.length - 1; i++) {
    const [x0, y0] = prof[i], [x1, y1] = prof[i + 1];
    if (x1 < xa || x0 > xb) continue;
    if (x0 === x1) { push([x0, y0]); push([x1, y1]); continue; }
    const t0 = Math.max(0, (xa - x0) / (x1 - x0)), t1 = Math.min(1, (xb - x0) / (x1 - x0));
    push([x0 + (x1 - x0) * t0, y0 + (y1 - y0) * t0]);
    push([x0 + (x1 - x0) * t1, y0 + (y1 - y0) * t1]);
  }
  return out;
}

// 通用楼顶样式及权重（权重越大越常见）
const TOWER_STYLES = [
  ['flat', 1.2], ['box', 2.4], ['stepped', 1.7], ['slant', 0.9], ['chamfer', 1.0],
  ['round', 0.6], ['mast', 0.3], ['pyramid', 0.4], ['fins', 0.3], ['notch', 0.6],
];
const RESIDENTIAL_STYLES = [['flat', 1.5], ['box', 2.0], ['stepped', 0.8], ['slant', 0.5], ['round', 0.4]];
const FAR_STYLES = [['flat', 1.5], ['box', 2.0], ['stepped', 1.4], ['slant', 0.8], ['chamfer', 0.8], ['round', 0.4]];

// 通用写字楼几何：返回 { x0, x1, baseY, prof, extras[], shaftTop }
function towerGeom(rng, cx, baseY, w, h, style, u) {
  const x0 = cx - w / 2, x1 = cx + w / 2, top = baseY - h;
  let prof; const extras = []; let shaftTop = top;
  switch (style) {
    case 'box': {
      const bw = w * rng.range(0.3, 0.62), bh = Math.max(4 * u, h * rng.range(0.025, 0.05));
      const bx = cx + rng.range(-1, 1) * (w - bw) * 0.3;
      prof = [[x0, top], [bx - bw / 2, top], [bx - bw / 2, top - bh], [bx + bw / 2, top - bh], [bx + bw / 2, top], [x1, top]];
      break;
    }
    case 'stepped': {
      const n = rng.int(1, 3), sym = rng.chance(0.6);
      let lx = x0, rx = x1, y = top;
      const left = [[lx, y]], right = [[rx, y]];
      for (let i = 0; i < n; i++) {
        const dl = w * rng.range(0.07, 0.14), dr = sym ? dl : w * rng.range(0.03, 0.16);
        const sh = Math.max(5 * u, h * rng.range(0.03, 0.06));
        if (rx - dr - (lx + dl) < w * 0.25) break;
        lx += dl; rx -= dr;
        left.push([lx, y], [lx, y - sh]); right.push([rx, y], [rx, y - sh]);
        y -= sh;
      }
      prof = [...left, ...right.reverse()];
      break;
    }
    case 'slant': {
      const sl = w * rng.range(0.18, 0.45);
      prof = rng.chance(0.5) ? [[x0, top - sl], [x1, top]] : [[x0, top], [x1, top - sl]];
      break;
    }
    case 'chamfer': {
      const c = w * rng.range(0.12, 0.24);
      prof = [[x0, top + c], [x0 + c, top], [x1 - c, top], [x1, top + c]];
      shaftTop = top + c;
      break;
    }
    case 'round': {
      const r = w * rng.range(0.25, 0.48), both = rng.chance(0.35), leftSide = rng.chance(0.5);
      const arcL = [], arcR = [];
      for (let i = 0; i <= 8; i++) {
        const th = Math.PI - (Math.PI / 2) * (i / 8);
        arcL.push([x0 + r + r * Math.cos(th), top + r - r * Math.sin(th)]);
      }
      for (let i = 0; i <= 8; i++) {
        const th = Math.PI / 2 - (Math.PI / 2) * (i / 8);
        arcR.push([x1 - r + r * Math.cos(th), top + r - r * Math.sin(th)]);
      }
      if (both) prof = [...arcL, ...arcR];
      else if (leftSide) prof = [...arcL, [x1, top]];
      else prof = [[x0, top], ...arcR];
      shaftTop = top + r * 0.6;
      break;
    }
    case 'mast': {
      prof = [[x0, top], [x1, top]];
      const mw = Math.max(1.8 * u, w * 0.035), mh = h * rng.range(0.08, 0.17);
      const mx = cx + rng.range(-0.25, 0.25) * w;
      extras.push(rectP(mx - mw * 1.6, top - 4 * u, mw * 3.2, 4 * u), rectP(mx - mw / 2, top - mh, mw, mh));
      break;
    }
    case 'pyramid': {
      const ph = w * rng.range(0.22, 0.5);
      prof = [[x0, top], [cx - w * 0.04, top - ph], [cx + w * 0.04, top - ph], [x1, top]];
      break;
    }
    case 'fins': {
      prof = [[x0, top], [x1, top]];
      const n = rng.int(2, 4), fw = Math.max(1.6 * u, w * 0.045);
      for (let i = 0; i < n; i++) {
        const fx = x0 + w * (0.15 + 0.7 * (n === 1 ? 0.5 : i / (n - 1)));
        const fh = h * rng.range(0.035, 0.09);
        extras.push(rectP(fx - fw / 2, top - fh, fw, fh));
      }
      break;
    }
    case 'notch': {
      const nw = w * rng.range(0.12, 0.2), t = Math.max(6 * u, h * rng.range(0.03, 0.06));
      prof = [[x0, top - t], [cx - nw, top - t], [cx - nw, top], [cx + nw, top], [cx + nw, top - t * 0.8], [x1, top - t * 0.8]];
      break;
    }
    default: prof = [[x0, top], [x1, top]];
  }
  return { x0, x1, baseY, prof, extras, shaftTop };
}

// 窗带 / 竖梃：低对比的细线，只为让剪影“读起来像楼”
function windowsD(g, look, rng, u) {
  const inset = Math.max(2 * u, (g.x1 - g.x0) * 0.09);
  const wx0 = g.x0 + inset, wx1 = g.x1 - inset;
  const wy0 = g.shaftTop + 7 * u, wy1 = g.baseY - 3 * u;
  if (wx1 - wx0 < 4 * u || wy1 - wy0 < 10 * u) return '';
  let d = '';
  const type = look.windows;
  if (type === 'floors' || type === 'grid') {
    const fh = look.floorPx * u, lh = Math.max(1.1 * u, fh * 0.22);
    for (let y = wy0; y < wy1; y += fh) if (rng.chance(0.92)) d += rectP(wx0, y, wx1 - wx0, lh);
  }
  if (type === 'mullions' || type === 'grid') {
    const mw = look.mullionPx * u, lw = Math.max(1.1 * u, mw * 0.24);
    for (let x = wx0; x < wx1 - lw; x += mw) d += rectP(x, wy0, lw, wy1 - wy0);
  }
  if (type === 'bands') {
    const fh = look.floorPx * u * 2.1;
    for (let y = wy0; y < wy1; y += fh) d += rectP(wx0, y, wx1 - wx0, fh * 0.4);
  }
  return d;
}

// 画一栋楼（剪影 + 受光面 + 窗带 + 自定义装饰），作为一个图元加入图层
function drawTower(L, z, g, look, drng) {
  const u = L.S.u;
  let s = pathEl(poly(profPoly(g.prof, g.baseY)), look.sil);
  for (const e of g.extras) s += pathEl(e, look.sil);
  if (look.lit && look.litFrac > 0) {
    const xs = g.x0 + (g.x1 - g.x0) * look.litFrac;
    const lp = clipProf(g.prof, g.x0, xs);
    if (lp.length >= 2) s += pathEl(poly(profPoly(lp, g.baseY)), look.lit);
  }
  if (look.windows && look.windows !== 'none') {
    const d = windowsD(g, look, drng, u);
    if (d) s += pathEl(d, look.win, ` fill-opacity="${look.winOpacity}"`);
  }
  if (g.decor) s += g.decor;
  L.add(z, g.x0 - 3 * u, g.x1 + 3 * u, s);
}

// 圆冠树（无花果/桉树的简化）：返回 { d, x0, x1 }
function roundTreeD(rng, cx, groundY, h, w) {
  const tw = Math.max(1.4, w * 0.08);
  let d = rectP(cx - tw / 2, groundY - h * 0.45, tw, h * 0.45 + 1);
  const n = rng.int(2, 4);
  for (let i = 0; i < n; i++) {
    const r = w * rng.range(0.26, 0.4);
    const ox = (i / Math.max(1, n - 1) - 0.5) * (w - 2 * r);
    const oy = groundY - h + r + rng.range(0, h * 0.28);
    d += circleP(cx + ox, oy, r);
  }
  return { d, x0: cx - w / 2 - 2, x1: cx + w / 2 + 2 };
}
// 桉树（gum tree）：细高树干 + 稀疏不规则的几团树冠
function gumTreeD(rng, cx, groundY, h, u) {
  const tw = Math.max(1.3 * u, h * 0.035), lean = rng.range(-0.08, 0.08) * h;
  const tx = cx + lean, ty = groundY - h * 0.58;
  let d = poly([[cx - tw, groundY + 1], [tx - tw * 0.5, ty], [tx + tw * 0.5, ty], [cx + tw, groundY + 1]]);
  const n = rng.int(4, 6);
  let x0 = cx - tw, x1 = cx + tw;
  for (let i = 0; i < n; i++) {
    const a = (i / (n - 1) - 0.5) * 1.7 + rng.range(-0.2, 0.2);
    const px = tx + Math.sin(a) * h * rng.range(0.18, 0.3);
    const py = groundY - h * rng.range(0.66, 0.9) + Math.abs(Math.sin(a)) * h * 0.1;
    const rx = h * rng.range(0.12, 0.19), ry = rx * rng.range(0.55, 0.8);
    d += ellipseP(px, py, rx, ry);
    d += poly([[tx - tw * 0.3, ty + tw], [px - tw * 0.22, py + ry * 0.4], [px + tw * 0.22, py + ry * 0.4], [tx + tw * 0.3, ty + tw]]);
    x0 = Math.min(x0, px - rx); x1 = Math.max(x1, px + rx);
  }
  return { d, x0: x0 - 2, x1: x1 + 2 };
}
// 棕榈树：细弯树干 + 放射状羽叶
function palmD(rng, cx, groundY, h, u) {
  const lean = rng.range(-0.12, 0.12) * h;
  const top = [cx + lean, groundY - h];
  const tw = Math.max(1.3 * u, h * 0.035);
  let d = mv([cx - tw, groundY + 1]) + `Q${fmt(cx + lean * 0.2 - tw)} ${fmt(groundY - h * 0.5)} ${fmt(top[0] - tw * 0.6)} ${fmt(top[1])}` +
    ln([top[0] + tw * 0.6, top[1]]) + `Q${fmt(cx + lean * 0.2 + tw)} ${fmt(groundY - h * 0.5)} ${fmt(cx + tw)} ${fmt(groundY + 1)}Z`;
  const n = rng.int(6, 8), fl = h * rng.range(0.32, 0.42);
  for (let i = 0; i < n; i++) {
    const a = -Math.PI / 2 + (i / (n - 1) - 0.5) * Math.PI * 1.55 + rng.range(-0.12, 0.12);
    const ex = top[0] + Math.cos(a) * fl, ey = top[1] + Math.sin(a) * fl * 0.62 + fl * 0.35 * Math.abs(Math.cos(a));
    const mx = top[0] + Math.cos(a) * fl * 0.55, my = top[1] + Math.sin(a) * fl * 0.55 - fl * 0.12;
    const wv = fl * 0.09;
    d += mv(top) + `Q${fmt(mx - Math.sin(a) * wv)} ${fmt(my + Math.cos(a) * wv - wv)} ${fmt(ex)} ${fmt(ey)}` +
      `Q${fmt(mx + Math.sin(a) * wv)} ${fmt(my - Math.cos(a) * wv + wv)} ${fmt(top[0])} ${fmt(top[1])}Z`;
  }
  return { d, x0: cx - fl - h * 0.15, x1: cx + fl + h * 0.15 };
}

// 黑天鹅（单位坐标：长 1，水面 y=0，朝右）
function swanSvg(x, y, len, dir, pal) {
  const body = 'M0.02 -0.2C0.05 -0.37 0.21 -0.42 0.31 -0.31C0.42 -0.35 0.56 -0.33 0.66 -0.22' +
    'C0.75 -0.14 0.76 -0.02 0.66 0L0.14 0C0.06 -0.02 0.01 -0.1 0.02 -0.2Z';
  const neck = 'M0.645 -0.2C0.6 -0.5 0.86 -0.58 0.785 -0.86';
  const sw = 0.075;
  return `<g transform="translate(${fmt(x)} ${fmt(y)}) scale(${fmt(len * dir)} ${fmt(len)})">` +
    `<path d="M-0.05 0.035h0.82v0.035h-0.82Z" fill="${pal.swan}" fill-opacity="0.28"/>` +
    `<path d="${body}" fill="${pal.swan}"/>` +
    `<path d="${neck}" fill="none" stroke="${pal.swan}" stroke-width="${sw}" stroke-linecap="round"/>` +
    `<path d="${ellipseP(0.8, -0.868, 0.058, 0.042)}" fill="${pal.swan}"/>` +
    `<path d="M0.835 -0.892L0.965 -0.845L0.84 -0.846Z" fill="${pal.beak}"/>` +
    `<path d="M0.14 -0.25C0.24 -0.33 0.36 -0.3 0.45 -0.26" fill="none" stroke="#3a4048" stroke-width="0.03" stroke-opacity="0.7"/>` +
    `</g>`;
}

// =============================================================================
// 7. 场景通用：天空 / 分区楼群 / 外观
// =============================================================================
function buildSky(S) {
  const L = new Layer(S, 'sky');
  const stops = S.p.palette.sky.map(([o, c]) => `<stop offset="${o}" stop-color="${c}"/>`).join('');
  L.defs.set(`${L.id}-g`, `<linearGradient id="${L.id}-g" gradientUnits="userSpaceOnUse" x1="0" y1="0" x2="0" y2="${S.H}">${stops}</linearGradient>`);
  L.addPeriodic(0, () => `<rect x="-4" y="-4" width="${S.W + 8}" height="${S.H + 8}" fill="url(#${L.id}-g)"/>`);
  return L;
}

// 每层的颜色工具：hz(颜色, 额外雾量) → 混合大气透视色
function layerCtx(S, cfg, baseY) {
  const pal = S.p.palette;
  const hz = (c, k = 0) => mix(c, pal.haze, clamp(cfg.haze + k, 0, 1));
  return { pal, hz, cfg, pxPerM: cfg.pxPerM * S.u, baseY };
}

// 一个分区的通用楼群。protect：地标保护区，限制挡在地标前/后的楼高
function placeZone(S, L, C, zone, opts) {
  const { W, u } = S;
  const rng = new Rng(S.seed, `${L.id}.${zone.name}`);
  const drng = new Rng(S.seed, `${L.id}.${zone.name}.detail`);
  const n = Math.max(0, Math.round(zone.count * (opts.density ?? 1)));
  if (!n) return;
  const zx0 = zone.x0 * W, zx1 = zone.x1 * W, slot = (zx1 - zx0) / n;
  const towers = [];
  for (let i = 0; i < n; i++) {
    const cx = zx0 + slot * (i + 0.5) + rng.range(-0.45, 0.45) * slot;
    const wPx = rng.range(zone.w[0], zone.w[1]) * C.pxPerM;
    const d = wrapDelta(cx, zone.peak * W, W) / W;
    const env = Math.exp(-Math.pow(d / zone.spread, 2));
    const t = clamp(env * 0.8 + 0.2, 0, 1) * rng.range(0.5, 1.0);
    let hPx = lerp(zone.h[0], zone.h[1], t) * C.pxPerM;
    const style = rng.weighted(opts.styles || (zone.tone === 'light' ? RESIDENTIAL_STYLES : TOWER_STYLES));
    // 地标保护
    let ok = true;
    for (const p of opts.protect || []) {
      const ox0 = cx - wPx / 2, ox1 = cx + wPx / 2;
      if (wrapDelta(ox1, p.x0, W) > -p.pad && wrapDelta(ox0, p.x1, W) < p.pad) {
        const lim = zone.row === 'front' ? p.front : p.back;
        if (lim <= 0) { ok = false; break; }
        if (hPx > lim) hPx = lim * rng.range(0.82, 1.0);
      }
    }
    if (!ok || hPx < zone.h[0] * C.pxPerM * 0.45) continue;
    towers.push({ cx, wPx, hPx, style, rs: rng.f() });
  }
  // 同一排里高的先画（在后），矮的后画（在前）
  towers.sort((a, b) => b.hPx - a.hPx);
  for (const t of towers) {
    const g = towerGeom(new Rng(S.seed, `${L.id}.${zone.name}.${Math.round(t.cx)}`), t.cx, C.baseY, t.wPx, t.hPx, t.style, u);
    const look = opts.look(t, drng);
    drawTower(L, opts.z, g, look, drng);
  }
}

// =============================================================================
// 8. Perth 地标
// =============================================================================

// ---- Central Park：最高、细长、阶梯式楼冠 + 天线；暗面有 X 形外露钢结构 ----
function lmCentralPark(S, C, spec, look) {
  const s = C.pxPerM * spec.scale, F = frame(spec.x * S.W, C.baseY, s), u = S.u;
  const hw = 22;
  const prof = F.P([[-hw, 200], [-18, 200], [-18, 210], [-13, 210], [-13, 219], [-8, 219], [-8, 226],
    [8, 226], [8, 219], [13, 219], [13, 210], [18, 210], [18, 200], [hw, 200]]);
  const extras = [poly(F.P([[-2.4, 226], [-2.4, 231], [2.4, 231], [2.4, 226]])), poly(F.P([[-0.75, 231], [-0.75, 253], [0.75, 253], [0.75, 231]]))];
  const xs = -hw + 2 * hw * look.litFrac + 1.2, xr = hw - 1.4;
  let br = '';
  for (let y0 = 10; y0 + 30 <= 198; y0 += 31) {
    br += polyline(F.P([[xs, y0], [xr, y0 + 30]])) + polyline(F.P([[xr, y0], [xs, y0 + 30]]));
  }
  const decor = strokeEl(br, look.brace, 1.7 * u, ' stroke-opacity="0.5"');
  return { x0: F.X(-hw), x1: F.X(hw), baseY: C.baseY, prof, extras, shaftTop: F.Y(198), decor,
    protect: { back: 186 * s, front: 92 * s } };
}

// ---- Brookfield Place：宽厚的玻璃塔，顶部一侧斜切 + 一片细高的竖向“鳍” + 旁边矮一点的副楼 ----
function lmBrookfield(S, C, spec) {
  const s = C.pxPerM * spec.scale, F = frame(spec.x * S.W, C.baseY, s);
  const main = { x0: F.X(-27), x1: F.X(27), baseY: C.baseY, extras: [], shaftTop: F.Y(196),
    prof: F.P([[-27, 215], [-23.2, 215], [-23.2, 206], [16, 206], [27, 195.5]]) };
  const annex = { x0: F.X(25), x1: F.X(54), baseY: C.baseY, extras: [rectP(F.X(33), F.Y(102.5), 12 * s, 4.5 * s)], shaftTop: F.Y(98),
    prof: F.P([[25, 98], [54, 98]]) };
  return { main, annex, x0: F.X(-27), x1: F.X(54), protect: { back: 182 * s, front: 88 * s } };
}

// ---- QV1：一侧圆肩的楼顶 + 另一侧稍高的核心筒；立面密集水平遮阳 ----
function lmQV1(S, C, spec) {
  const s = C.pxPerM * spec.scale, F = frame(spec.x * S.W, C.baseY, s);
  const r = 15, arc = [];
  for (let i = 0; i <= 10; i++) { const th = Math.PI - (Math.PI / 2) * (i / 10); arc.push([-23 + r + r * Math.cos(th), 158 - r + r * Math.sin(th)]); }
  const prof = F.P([...arc, [11, 158], [11, 166.5], [23, 166.5]]);
  return { x0: F.X(-23), x1: F.X(23), baseY: C.baseY, prof, extras: [], shaftTop: F.Y(150),
    decor: '', protect: { back: 138 * s, front: 68 * s } };
}

// ---- Perth Arena：多面体折板造型，锯齿状屋顶 ----
function lmPerthArena(S, C, spec) {
  const s = C.pxPerM * spec.scale, F = frame(spec.x * S.W, C.baseY, s);
  const w = 128, h = 46;
  const N = (x, y) => [x * w, y * h];
  const O = [N(-0.5, 0), N(-0.53, 0.42), N(-0.4, 0.6), N(-0.25, 0.5), N(-0.1, 0.82), N(0.06, 0.66), N(0.24, 1.0), N(0.4, 0.74), N(0.55, 0.47), N(0.5, 0)];
  const Q = [N(-0.37, 0.2), N(-0.13, 0.32), N(0.1, 0.26), N(0.3, 0.42), N(0.45, 0.18)];
  const tris = [
    [O[0], O[1], Q[0], 1], [O[1], O[2], Q[0], 2], [O[2], O[3], Q[0], 3], [O[3], Q[1], Q[0], 1],
    [O[3], O[4], Q[1], 2], [O[4], O[5], Q[1], 0], [O[5], Q[2], Q[1], 2], [O[5], O[6], Q[2], 3],
    [O[6], Q[3], Q[2], 1], [O[6], O[7], Q[3], 2], [O[7], O[8], Q[3], 0], [O[8], Q[4], Q[3], 1],
    [O[8], O[9], Q[4], 2], [O[9], O[0], Q[4], 0], [O[0], Q[0], Q[1], 0], [O[0], Q[1], Q[2], 1],
    [O[0], Q[2], Q[4], 0], [Q[2], Q[3], Q[4], 2],
  ];
  return { x0: F.X(-0.55 * w), x1: F.X(0.56 * w), F, O, tris, protect: { back: 28 * s, front: 0 } };
}

// ---- Optus Stadium：椭圆碗形，古铜色竖向鳍片的节奏 + 白色弧形屋檐 ----
function lmOptus(S, C, spec) {
  const s = C.pxPerM * spec.scale, F = frame(spec.x * S.W, C.baseY, s);
  return { x0: F.X(-114), x1: F.X(114), F, a: 108, b: 43, protect: { back: 26 * s, front: 0 } };
}

// ---- Matagarup Bridge：两道高高的波浪形拱（像两只相对的天鹅脖子）+ 斜拉索 ----
function lmMatagarup(S, C, spec) {
  const s = C.pxPerM * spec.scale, F = frame(spec.x * S.W, C.baseY, s);
  return { x0: F.X(-124), x1: F.X(124), F, A: 118, protect: { back: 52 * s, front: 0 } };
}

// =============================================================================
// 9. Perth 各层
// =============================================================================
function buildPerth(S) {
  return { sky: buildSky(S), far: buildPerthFar(S), mid: buildPerthMid(S), near: buildPerthNear(S) };
}

// ---------------- 远景 ----------------
function buildPerthFar(S) {
  const L = new Layer(S, 'far'), P = S.p.far, { W, H, u } = S;
  const horizonY = S.p.horizon * H;
  const C = layerCtx(S, P, horizonY + 2 * u), pal = C.pal;

  // --- Darling Range 远山：整幅连续，东侧（右）更高 ---
  const D = P.darling;
  const nz = periodicNoise(new Rng(S.seed, 'far.darling'), W, D.kMin, D.kMax, 1.15);
  const nzf = periodicNoise(new Rng(S.seed, 'far.darling.fine'), W, 24, 70, 0.8);
  const baseD = horizonY + D.base * u;
  const fD = (x) => {
    const e = (1 - D.eastBoost) + D.eastBoost * (0.5 + 0.5 * Math.cos(2 * Math.PI * (x / W - D.eastX)));
    return baseD - D.height * u * (e * (0.58 + 0.42 * nz(x))) - 2.2 * u * nzf(x);
  };
  const matD = L.mat('darling', C.hz(pal.hills, 0.16), C.hz(pal.hills, 0.16 + P.groundHaze), baseD - D.height * u, baseD);
  L.addPeriodic(0, (sh) => pathEl(terrainPath(S, fD, sh, H + 4), matD));

  // --- 远处楼群 ---
  const farTop = horizonY - 340 * u;
  const lookFar = (t) => ({
    sil: L.mat('tw', C.hz(pal.building, 0.02), C.hz(pal.building, 0.02 + P.groundHaze), farTop, horizonY),
    lit: L.mat('twlit', C.hz(pal.buildingLit, -0.02), C.hz(pal.buildingLit, P.groundHaze), farTop, horizonY),
    litFrac: 0.38 + t.rs * 0.14, windows: 'none',
  });
  for (const zone of P.zones) placeZone(S, L, C, zone, { z: 1, look: lookFar, density: P.density, protect: [], styles: FAR_STYLES });

  // --- Kings Park（Mount Eliza）山脊 + 树冠 ---
  const K = P.kingsPark;
  const span = mod(K.x3 - K.x0, 1), t1 = mod(K.x1 - K.x0, 1), t2 = mod(K.x2 - K.x0, 1);
  const kn = periodicNoise(new Rng(S.seed, 'far.kp'), W, 8, 34, 0.9);
  const ridgeH = (x) => {
    const t = mod(x / W - K.x0, 1);
    if (t > span) return 0;
    return smoothstep(0, t1, t) * (1 - smoothstep(t2, span, t)) * K.height * u * (0.93 + 0.07 * kn(x));
  };
  const kBase = horizonY + 3 * u;
  const ridgeY = (x) => kBase - ridgeH(x);
  const keepClear = [[P.warMemorial.x * W, 15 * u], [P.dnaTower.x * W, 9 * u]];
  const trng = new Rng(S.seed, 'far.kp.trees');
  const trees = [];
  for (let x = K.x0 * W; x < K.x0 * W + span * W; x += trng.range(5, 11) * u) {
    const xx = mod(x, W), hh = ridgeH(xx);
    if (hh < 6 * u) continue;
    if (keepClear.some(([cx, r]) => Math.abs(wrapDelta(xx, cx, W)) < r)) continue;
    const big = trng.chance(0.16);
    const r = trng.range(4.5, 9.5) * u * K.canopy * (big ? 1.55 : 1);
    trees.push({ x: xx, cy: ridgeY(xx) + r * (big ? 0.2 : 0.5), r });
  }
  const kpY = (x) => {
    let y = ridgeY(x);
    for (const c of trees) { const dx = wrapDelta(x, c.x, W); if (Math.abs(dx) < c.r) y = Math.min(y, c.cy - Math.sqrt(c.r * c.r - dx * dx)); }
    return y;
  };
  const kpTop = kBase - K.height * u - 20 * u;
  const matKP = L.mat('kp', C.hz(pal.park, K.haze), C.hz(pal.park, K.haze + P.groundHaze), kpTop, kBase);
  L.addPeriodic(2, (sh) => pathEl(terrainPath(S, kpY, sh, H + 4), matKP));

  // --- Mount Eliza 朝城市一侧的陡坡：浅色石灰岩崖面 + 零星灌木（让山体不只是一团） ---
  {
    const X2 = (K.x0 + t2) * W, X3 = (K.x0 + span) * W, xa = X2 - 26 * u;
    const top = [], bot = [];
    for (let x = xa; x <= X3; x += 3 * u) {
      const ry = ridgeY(mod(x, W)), hh = kBase - ry;
      const f = smoothstep(xa, X2 + 10 * u, x) * (1 - smoothstep(X3 - 14 * u, X3, x));
      top.push([x, ry + 9 * u]); bot.push([x, ry + 9 * u + hh * 0.5 * f]);
    }
    const face = C.hz(mix(pal.park, '#d9c8a2', 0.55), K.haze + 0.04);
    let d = poly([...top, ...bot.reverse()]);
    const brng = new Rng(S.seed, 'far.kp.face');
    let bush = '';
    for (let i = 0; i < 26; i++) {
      const x = brng.range(xa + 10 * u, X3 - 6 * u), ry = ridgeY(mod(x, W));
      bush += ellipseP(x, ry + brng.range(12 * u, Math.max(14 * u, 0.5 * (kBase - ry))), brng.range(3, 7) * u, brng.range(2, 4) * u);
    }
    L.add(2.2, xa, X3, pathEl(d, face, ' fill-opacity="0.75"') + pathEl(bush, matKP));
  }

  // --- 州战争纪念碑（方尖碑）---
  {
    const m = P.warMemorial, cx = m.x * W, by = ridgeY(cx) + 2 * u, k = m.scale * u;
    const d = rectP(cx - 15 * k, by - 4 * k, 30 * k, 6 * k) + rectP(cx - 10 * k, by - 8 * k, 20 * k, 5 * k) +
      poly([[cx - 4.6 * k, by - 8 * k], [cx - 3.2 * k, by - 62 * k], [cx, by - 69 * k], [cx + 3.2 * k, by - 62 * k], [cx + 4.6 * k, by - 8 * k]]);
    L.add(3, cx - 16 * k, cx + 16 * k, pathEl(d, matKP));
  }
  // --- DNA 塔：双螺旋楼梯塔，锯齿边 + 顶部平台 ---
  {
    const m = P.dnaTower, cx = m.x * W, by = ridgeY(cx) + 2 * u, k = m.scale * u;
    const h = 44 * k, hw = 5.2 * k, n = 9, left = [], right = [];
    for (let i = 0; i <= n; i++) {
      const y = by - (h - 6 * k) * (i / n), z = (i % 2 ? 1.6 : 0) * k;
      left.push([cx - hw - z, y]); right.push([cx + hw + (1.6 * k - z), y]);
    }
    const d = poly([...left, [cx - hw - 2.4 * k, by - h + 6 * k], [cx - hw - 2.4 * k, by - h + 2.5 * k], [cx - 1.2 * k, by - h],
      [cx + 1.2 * k, by - h], [cx + hw + 2.4 * k, by - h + 2.5 * k], [cx + hw + 2.4 * k, by - h + 6 * k], ...right.reverse()]);
    L.add(3, cx - 10 * k, cx + 10 * k, pathEl(d, matKP));
  }
  return L;
}

// ---------------- 中景 ----------------
function buildPerthMid(S) {
  const L = new Layer(S, 'mid'), P = S.p.mid, { W, H, u } = S;
  const baseY = P.baseY * H, C = layerCtx(S, P, baseY), pal = C.pal;
  const topY = baseY - 600 * u;
  const G = P.groundHaze;
  const looks = {
    sil: L.mat('sil', C.hz(pal.building), C.hz(pal.building, G), topY, baseY),
    lit: L.mat('lit', C.hz(pal.buildingLit), C.hz(pal.buildingLit, G), topY, baseY),
    silBack: L.mat('silb', C.hz(pal.building, 0.12), C.hz(pal.building, 0.12 + G), topY, baseY),
    litBack: L.mat('litb', C.hz(pal.buildingLit, 0.1), C.hz(pal.buildingLit, 0.1 + G), topY, baseY),
    silLight: L.mat('sill', C.hz(mix(pal.building, pal.buildingLit, 0.4), 0.06), C.hz(mix(pal.building, pal.buildingLit, 0.4), 0.06 + G), topY, baseY),
    litLight: L.mat('litl', C.hz(mix(pal.buildingLit, '#ffffff', 0.25), 0.04), C.hz(mix(pal.buildingLit, '#ffffff', 0.25), 0.04 + G), topY, baseY),
    win: C.hz(pal.window, 0.05),
  };
  const winType = (rng) => rng.weighted([['floors', 5], ['mullions', 1.4], ['grid', 0.5], ['bands', 2], ['none', 1]]);
  const lookFor = (row, tone) => (t, drng) => ({
    sil: tone === 'light' ? looks.silLight : row === 'back' ? looks.silBack : looks.sil,
    lit: tone === 'light' ? looks.litLight : row === 'back' ? looks.litBack : looks.lit,
    litFrac: 0.3 + t.rs * 0.18,
    windows: tone === 'light' ? 'bands' : winType(drng),
    floorPx: drng.range(8, 11.5), mullionPx: drng.range(7, 11),
    win: looks.win, winOpacity: fmt(tone === 'light' ? 0.18 : drng.range(0.09, 0.15)),
  });

  // --- 地标几何（先算，用于保护区） ---
  const LM = P.landmarks;
  const cp = lmCentralPark(S, C, LM.centralPark, { litFrac: 0.42, brace: C.hz(pal.window, 0.1) });
  const bp = lmBrookfield(S, C, LM.brookfield);
  const qv = lmQV1(S, C, LM.qv1);
  const pa = lmPerthArena(S, C, LM.perthArena);
  const os = lmOptus(S, C, LM.optusStadium);
  const mb = lmMatagarup(S, C, LM.matagarup);
  const protect = [cp, bp, qv, pa, os, mb].map(g => ({ x0: g.x0, x1: g.x1, pad: 6 * u, ...g.protect }));

  // --- 地面 + 低矮街区 + 河面（Matagarup 桥下） ---
  const matGround = L.mat('ground', C.hz(pal.ground, 0.08), C.hz(pal.ground, 0.08 + G), baseY - 60 * u, baseY);
  L.addPeriodic(0, () => `<rect x="-4" y="${fmt(baseY - 3 * u)}" width="${W + 8}" height="${fmt(H - baseY + 8 * u)}" fill="${matGround}"/>`);
  const riverX0 = mb.F.X(-mb.A - 2), riverX1 = mb.F.X(mb.A + 2);
  const waterFar = C.hz(pal.water, 0.28);
  L.add(0.2, riverX0, riverX1, pathEl(rectP(riverX0, baseY - 4 * u, riverX1 - riverX0, H - baseY + 6 * u), waterFar));
  {
    // 低矮街区：小楼（双色）+ 行道树，只露出近景岸线上方一点点
    const rng = new Rng(S.seed, 'mid.fabric'), F = P.fabric;
    const fabLit = C.hz(mix(pal.ground, pal.buildingLit, 0.45), 0.12);
    const treeC = C.hz(pal.tree, 0.1);
    for (let x = 0; x < W;) {
      const w = rng.range(F.w[0], F.w[1]) * C.pxPerM, h = rng.range(F.h[0], F.h[1]) * C.pxPerM;
      const cx = x + w / 2;
      const inRiver = Math.abs(wrapDelta(cx, (riverX0 + riverX1) / 2, W)) < (riverX1 - riverX0) / 2 + w;
      if (!inRiver) {
        if (rng.chance(F.treeChance)) { const t = roundTreeD(rng, cx, baseY + 2 * u, h * 1.25, w * 0.9); L.add(0.5, t.x0, t.x1, pathEl(t.d, treeC)); }
        else {
          const bw = w * 0.9;
          L.add(0.5, x, x + bw, pathEl(rectP(x, baseY - h, bw, h + 4 * u), matGround) + pathEl(rectP(x, baseY - h, bw * 0.4, h + 4 * u), fabLit, ' fill-opacity="0.55"'));
        }
      }
      x += w * rng.range(0.75, 1.1);
    }
  }

  // --- 后排楼群 / 前排楼群 ---
  for (const zone of P.zones) {
    placeZone(S, L, C, zone, { z: zone.row === 'front' ? 3 : 1, look: lookFor(zone.row, zone.tone), density: P.density, protect });
  }

  // --- 地标绘制 ---
  const mainLook = { sil: looks.sil, lit: looks.lit, litFrac: 0.42, win: looks.win };
  drawTower(L, 2, cp, { ...mainLook, windows: 'none' }, null);
  drawTower(L, 1.9, bp.annex, { ...mainLook, sil: looks.silBack, lit: looks.litBack, litFrac: 0.4, windows: 'floors', floorPx: 9, winOpacity: '0.16' }, new Rng(S.seed, 'bp.annex'));
  drawTower(L, 2, bp.main, { ...mainLook, litFrac: 0.4, windows: 'mullions', mullionPx: 5.2, winOpacity: '0.2' }, new Rng(S.seed, 'bp.main'));
  drawTower(L, 2, qv, { ...mainLook, litFrac: 0.46, windows: 'floors', floorPx: 6.4, winOpacity: '0.24' }, new Rng(S.seed, 'qv1'));

  // Perth Arena
  {
    const tones = pal.arena.map((c, i) => C.hz(c, 0.06 + i * 0.01));
    let s = pathEl(poly(pa.F.P(pa.O)), tones[0]);
    for (const [a, b, c, k] of pa.tris) s += pathEl(poly(pa.F.P([a, b, c])), tones[k]);
    s += strokeEl(polyline(pa.F.P(pa.O.slice(1, 9))), C.hz(pal.white, 0.15), 1.4 * u, ' stroke-opacity="0.6" stroke-linejoin="round"');
    L.add(2, pa.x0 - 3 * u, pa.x1 + 3 * u, s);
  }
  // Optus Stadium
  {
    const { F, a, b } = os, s = F.s;
    const se = (x, A, B, n) => B * Math.pow(Math.max(0, 1 - Math.pow(Math.abs(x / A), n)), 1 / n); // 超椭圆
    const yf = (x) => se(x, a, b, 3.4);
    const yr = (x) => se(x, a * 1.03, b + 4.2, 3.4);
    const roofPts = [], facPts = [], shPts = [];
    for (let i = 0; i <= 96; i++) { const x = -a * 1.03 + (2 * a * 1.03) * i / 96; roofPts.push([x, yr(x)]); }
    for (let i = 0; i <= 96; i++) { const x = -a + 2 * a * i / 96; facPts.push([x, yf(x)]); shPts.push([x, yf(x) + 1.6]); }
    let g = pathEl(poly(F.P([[-a * 1.03, 0], ...roofPts, [a * 1.03, 0]])), C.hz(pal.white, 0.1));
    g += pathEl(poly(F.P([[-a, 0], ...shPts, [a, 0]])), C.hz(mix(pal.bronze, '#000000', 0.35), 0.06));
    g += pathEl(poly(F.P([[-a, 0], ...facPts, [a, 0]])), C.hz(pal.bronze, 0.04));
    let fins = '';
    const step = 3.3, fw = 1.45;
    for (let x = -a + 2.5; x < a - 1.5; x += step) {
      const top = Math.min(yf(x - fw / 2), yf(x + fw / 2)) - 1.2;
      if (top < 6) continue;
      fins += poly(F.P([[x - fw / 2, 1], [x - fw / 2, top], [x + fw / 2, top + 0.6], [x + fw / 2, 1]]));
    }
    const finMat = `url(#${L.id}-fin)`;
    L.defs.set(`${L.id}-fin`, `<linearGradient id="${L.id}-fin" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="${C.hz(pal.bronzeLit, 0.02)}"/>` +
      `<stop offset="0.55" stop-color="${C.hz(mix(pal.bronzeLit, pal.bronze, 0.45), 0.04)}"/><stop offset="1" stop-color="${C.hz(pal.bronze, 0.06)}"/></linearGradient>`);
    g += pathEl(fins, finMat);
    g += pathEl(poly(F.P([[-a * 0.99, 0], [-a * 0.99, 7.5], [a * 0.99, 7.5], [a * 0.99, 0]])), C.hz(pal.building, 0.08));
    L.add(2, os.x0, os.x1, g);
  }
  // Matagarup Bridge
  {
    const { F, A } = mb, s = F.s;
    const deckY = (x) => 8 + 4.5 * (1 - Math.pow(x / A, 2));
    const steel = C.hz(pal.white, 0.06);
    const deckTop = [], deckBot = [];
    for (let i = 0; i <= 40; i++) { const x = -A + 2 * A * i / 40; deckTop.push([x, deckY(x) + 1.3]); deckBot.push([x, deckY(x) - 1.3]); }
    let g = pathEl(poly(F.P([...deckTop, ...deckBot.reverse()])), C.hz(pal.building, 0.04));
    g += pathEl(rectP(F.X(-A - 4), F.Y(deckY(-A) + 1.3), 8 * s, (deckY(-A) + 4) * s) + rectP(F.X(A - 4), F.Y(deckY(A) + 1.3), 8 * s, (deckY(A) + 4) * s), C.hz(pal.building, 0.04));
    const archL = [[-104, deckY(-104)], [-108, 70], [-46, 112], [-5, deckY(-5)]];
    const archR = archL.map(([x, y]) => [-x, y]);
    let cables = '';
    for (const arch of [archL, archR]) {
      const pts = [];
      for (let i = 0; i <= 48; i++) pts.push(bez(...arch, i / 48));
      g += pathEl(ribbon(F.P(pts), (t) => (4.6 - 2.2 * Math.sin(Math.PI * t)) * s), steel);
      for (let i = 4; i <= 44; i += 3) {
        const p = bez(...arch, i / 48);
        if (p[1] - deckY(p[0]) < 4) continue;
        const xd = p[0] * 0.92;
        cables += polyline(F.P([[p[0], p[1] - 1], [xd, deckY(xd) + 1]]));
      }
    }
    g += strokeEl(cables, steel, 0.55 * s, ' stroke-opacity="0.9"');
    g += pathEl(rectP(F.X(-1.6), F.Y(deckY(0) - 1), 3.2 * s, (deckY(0) + 2) * s), C.hz(pal.building, 0.04));
    L.add(2.5, mb.x0, mb.x1, g);
  }
  return L;
}

// ---------------- 近景 ----------------
function buildPerthNear(S) {
  const L = new Layer(S, 'near'), P = S.p.near, { W, H, u } = S;
  const yW = S.p.horizon * H, C = layerCtx(S, P, yW), pal = C.pal;
  const s = C.pxPerM;

  // --- 水面 ---
  const matWater = L.mat('water', mix(pal.water, pal.haze, 0.3), pal.waterDeep, yW, H);
  L.addPeriodic(0, () => `<rect x="-4" y="${fmt(yW)}" width="${W + 8}" height="${fmt(H - yW + 4)}" fill="${matWater}"/>` +
    `<rect x="-4" y="${fmt(yW)}" width="${W + 8}" height="${fmt(2.2 * u)}" fill="${pal.waterHi}" fill-opacity="0.5"/>`);
  {
    const rng = new Rng(S.seed, 'near.shimmer');
    let dl = '', dd = '';
    for (let i = 0; i < P.shimmer.count; i++) {
      const t = Math.pow(rng.f(), 1.25), y = yW + 7 * u + (H - yW - 10 * u) * t;
      const len = lerp(10, 80, t) * u * rng.range(0.45, 1.3), th = lerp(1.2, 3.4, t) * u;
      const x = rng.range(0, W);
      const dpath = rectP(x - len / 2, y, len, th);
      if (rng.chance(0.72)) L.add(1, x - len / 2 - 2, x + len / 2 + 2, pathEl(dpath, pal.waterHi, ` fill-opacity="${fmt(rng.range(0.16, 0.42))}"`));
      else L.add(1, x - len / 2 - 2, x + len / 2 + 2, pathEl(dpath, pal.waterDeep, ` fill-opacity="${fmt(rng.range(0.25, 0.45))}"`));
    }
  }

  // --- 岸线（gaps 处为开阔水面） ---
  const gaps = P.shore.gaps.map(([a, b]) => [a * W, b * W]).sort((a, b) => a[0] - b[0]);
  const shoreTop = yW - P.shore.height * u;
  const matShore = L.mat('shore', C.hz(pal.foreshore), C.hz(pal.foreshore, P.groundHaze), shoreTop - 30 * u, yW);
  const segs = [];
  for (let i = 0; i < gaps.length; i++) {
    const a = gaps[i][1], b = gaps[(i + 1) % gaps.length][0] + (i + 1 === gaps.length ? W : 0);
    segs.push([a, b]);
  }
  const srng = new Rng(S.seed, 'near.shore');
  for (const [a, b] of segs) {
    const pts = [[a - 6 * u, yW + 1.5 * u], [a, shoreTop + 3 * u]];
    for (let x = a + 8 * u; x < b - 8 * u; x += srng.range(14, 40) * u) pts.push([x, shoreTop + srng.range(-1.5, 2.5) * u]);
    pts.push([b, shoreTop + 3 * u], [b + 6 * u, yW + 1.5 * u]);
    L.add(2, a - 8 * u, b + 8 * u, pathEl(poly(pts), matShore));
  }
  const onShore = (x) => segs.some(([a, b]) => { const d = mod(x - a, W); return d > 6 * u && d < (b - a) - 6 * u; });

  // --- 地标几何 ---
  const bt = P.bellTower, eq = P.eqBridge, sp = P.spanda;
  const btX = bt.x * W, eqX = eq.x * W, spX = sp.x * W;
  const eqHalf = 55 * 1.3 * eq.scale * s;
  // [x, 半径, 允许的最大树高(0=不许种树)]
  const keepOut = [[btX, 21 * s * bt.scale, 6 * s], [spX, 12 * s * sp.scale, 0], [eqX - eqHalf, 8 * s, 0], [eqX + eqHalf, 8 * s, 0]];

  // --- 岸边树木 ---
  {
    const rng = new Rng(S.seed, 'near.trees'), T = P.trees;
    const dens = periodicNoise(new Rng(S.seed, 'near.trees.density'), W, 6, 22, 0.6); // 成团 / 留空
    const matTree = L.mat('tree', C.hz(pal.tree), C.hz(pal.tree, P.groundHaze), shoreTop - 60 * u, yW);
    const matTree2 = L.mat('tree2', C.hz(mix(pal.tree, pal.haze, 0.12)), C.hz(mix(pal.tree, pal.haze, 0.12), P.groundHaze), shoreTop - 60 * u, yW);
    let x = 0;
    while (x < W) {
      const dv = dens(x);
      x += rng.range(9, 30) * u / T.density / (0.45 + 0.75 * Math.max(0, dv + 0.35));
      if (!onShore(x) || dv < -0.55) continue;
      let hMax = T.h[1] * u;
      let blocked = false;
      for (const [kx, kr, kh] of keepOut) if (Math.abs(wrapDelta(x, kx, W)) < kr) { if (kh <= 0) blocked = true; else hMax = Math.min(hMax, kh); }
      if (blocked || hMax < T.h[0] * u * 0.5) continue;
      const h = Math.min(hMax, rng.range(T.h[0], T.h[1]) * u);
      const back = rng.chance(0.3); // 后面一排稍淡，增加层次
      const mat = back ? matTree2 : matTree;
      const r = rng.f();
      let t;
      if (r < T.palmChance) t = palmD(rng, x, shoreTop + 2 * u, h * 1.2, u);
      else if (r < T.palmChance + T.gumChance && h > 22 * u) t = gumTreeD(rng, x, shoreTop + 2 * u, h * 1.15, u);
      else t = roundTreeD(rng, x, shoreTop + 2 * u, h * 0.8, h * rng.range(0.9, 1.5));
      L.add(back ? 2.5 : 3, t.x0, t.x1, pathEl(t.d, mat));
    }
  }

  // --- Spanda：一组白色细长椭圆环，底部共点，逐级放大 ---
  {
    const k = s * sp.scale, H0 = 29 * 1.45, n = 6, white = C.hz(pal.white);
    let d = '';
    for (let i = 0; i < n; i++) {
      const hh = H0 * (0.42 + 0.58 * i / (n - 1)), ww = hh * 0.27;
      const cx = spX + (hh - H0 * 0.42) * 0.11 * k;
      d += ellipseP(cx, shoreTop - hh / 2 * k, ww / 2 * k, hh / 2 * k);
    }
    let g = strokeEl(d, white, 1.25 * k);
    g += pathEl(rectP(spX - 6 * k, shoreTop - 1.2 * k, 14 * k, 2 * k), C.hz(pal.foreshore));
    L.add(4, spX - 12 * k, spX + 20 * k, g);
    // 倒影
    let r = '';
    const rr = new Rng(S.seed, 'near.spanda.refl');
    for (let y = yW + 4 * u; y < yW + 60 * u; y += rr.range(4, 7) * u) { const w = rr.range(4, 14) * u; r += rectP(spX + rr.range(-3, 6) * u - w / 2, y, w, 1.6 * u); }
    L.add(1.5, spX - 20 * u, spX + 20 * u, pathEl(r, pal.waterHi, ' fill-opacity="0.35"'));
  }

  // --- Elizabeth Quay 步行桥：S 形桥面 + 两道交错的白色拱 + 吊索 ---
  {
    const k = s * eq.scale * 1.3, F = frame(eqX, shoreTop + 1 * u, k), Lh = 55;
    const deckY = (x) => 3.2 + 8.2 * (1 - Math.pow(x / Lh, 2));
    const white = C.hz(pal.white);
    const top = [], bot = [];
    for (let i = 0; i <= 40; i++) { const x = -Lh + 2 * Lh * i / 40; top.push([x, deckY(x) + 0.8]); bot.push([x, deckY(x) - 0.8]); }
    let g = pathEl(poly(F.P([...top, ...bot.reverse()])), white);
    g += strokeEl(polyline(F.P(top.map(([x, y]) => [x, y + 1.4]))), white, 0.3 * k, ' stroke-opacity="0.85"');
    const arches = [{ a: -1.04 * Lh, b: 0.38 * Lh, hgt: 22.5 }, { a: -0.38 * Lh, b: 1.04 * Lh, hgt: 19.5 }];
    let hang = '';
    for (const ar of arches) {
      const m = (ar.a + ar.b) / 2, hw = (ar.b - ar.a) / 2, pts = [];
      const yA = (x) => ar.hgt * (1 - Math.pow((x - m) / hw, 2));
      for (let i = 0; i <= 40; i++) { const x = ar.a + (ar.b - ar.a) * i / 40; pts.push([x, yA(x)]); }
      g += strokeEl(polyline(F.P(pts)), white, 1.3 * k, ' stroke-linecap="butt"');
      for (let x = ar.a + 4; x < ar.b - 3; x += 4.6) {
        if (Math.abs(x) > Lh * 0.96) continue;
        const ya = yA(x), yd = deckY(x);
        if (ya - yd > 1.2) hang += polyline(F.P([[x, ya], [x, yd]]));
      }
    }
    g = strokeEl(hang, white, 0.28 * k, ' stroke-opacity="0.9"') + g;
    g += pathEl(rectP(F.X(-Lh - 3), F.Y(deckY(-Lh) + 0.8), 6 * k, (deckY(-Lh) + 1) * k) + rectP(F.X(Lh - 3), F.Y(deckY(Lh) + 0.8), 6 * k, (deckY(Lh) + 1) * k), C.hz(pal.foreshore));
    L.add(4, F.X(-Lh * 1.1), F.X(Lh * 1.1), g);
    let r = '';
    const rr = new Rng(S.seed, 'near.eq.refl');
    for (let y = yW + 5 * u; y < yW + 46 * u; y += rr.range(4, 7) * u) {
      const w = rr.range(30, 120) * u; r += rectP(eqX + rr.range(-60, 60) * u - w / 2, y, w, 1.5 * u);
    }
    L.add(1.5, eqX - 140 * u, eqX + 140 * u, pathEl(r, pal.waterHi, ' fill-opacity="0.3"'));
  }

  // --- Bell Tower（天鹅钟楼）：细高玻璃尖塔 + 两侧铜色“帆翼” ---
  {
    const k = s * bt.scale, F = frame(btX, shoreTop + 1 * u, k);
    // 玻璃尖塔：细长、上部收成针尖；高度 82.5 m
    const hwP = [[0, 3.8], [12, 4.1], [26, 4.5], [36, 4.6], [46, 4.1], [56, 3.1], [65, 2.1], [73, 1.3], [79, 0.6], [82.5, 0]];
    const spireL = hwP.map(([y, w]) => [-w, y]), spireR = hwP.map(([y, w]) => [w, y]).reverse();
    const glassDark = C.hz(mix(pal.glass, pal.building, 0.55));
    const glassLit = C.hz(mix(pal.glass, '#ffffff', 0.45));
    let g = pathEl(poly(F.P([...spireL, ...spireR])), glassDark);
    g += pathEl(poly(F.P([...spireL, ...hwP.map(([y, w]) => [w * 0.12, y]).reverse()])), glassLit);
    let bands = '';
    for (const yy of [16, 26, 36, 46, 56]) { const i = hwP.findIndex(p => p[0] >= yy); const w = hwP[i][1]; bands += polyline(F.P([[-w, yy], [w, yy]])); }
    g += strokeEl(bands, C.hz(pal.building), 0.45 * k, ' stroke-opacity="0.4"');
    // 细描边：视差滑动时，尖塔背后可能是亮色楼面，描边保证轮廓始终可读
    g += strokeEl(poly(F.P([...spireL, ...spireR])), C.hz(mix(pal.glass, pal.building, 0.8)), 0.45 * k, ' stroke-opacity="0.7" stroke-linejoin="round"');
    // 铜帆翼：从塔脚两侧升起，越往上越向外倾斜张开的弧形“翅膀”（整体像 Ψ），
    // 尖端约在塔高的 58%（左=受光，右=背光）
    const inner = [[-3.9, 0], [-4.3, 11], [-5.6, 22], [-8.4, 32], [-12.8, 40.5], [-19.0, 47.5]];
    const outer = [[-19.0, 47.5], [-17.6, 40], [-15.6, 30], [-13.6, 19], [-12.0, 9], [-11.2, 0]];
    const sailD = (sgn) => {
      const pi = F.P(inner.map(([x, y]) => [x * sgn, y])), po = F.P(outer.map(([x, y]) => [x * sgn, y]));
      return mv(pi[0]) + smoothSeg(pi) + smoothSeg(po) + 'Z';
    };
    const copperLit = L.mat('cuL', C.hz(pal.copperLit), C.hz(mix(pal.copperLit, pal.copper, 0.45)), F.Y(48), F.Y(0));
    const copperDk = L.mat('cuD', C.hz(mix(pal.copper, pal.copperLit, 0.15)), C.hz(mix(pal.copper, '#000000', 0.2)), F.Y(48), F.Y(0));
    g += pathEl(sailD(1), copperLit) + pathEl(sailD(-1), copperDk);
    // 帆翼上的一道折棱（受光侧更亮的边）
    const ridgeL = F.P([[-7.6, 1], [-8.4, 14], [-10.4, 27], [-14.4, 40], [-18.2, 46]]);
    g += strokeEl(mv(ridgeL[0]) + smoothSeg(ridgeL), C.hz(mix(pal.copperLit, '#ffffff', 0.3)), 0.5 * k, ' stroke-opacity="0.65"');
    const ridgeR = F.P([[7.6, 1], [8.4, 14], [10.4, 27], [14.4, 40], [18.2, 46]]);
    g += strokeEl(mv(ridgeR[0]) + smoothSeg(ridgeR), C.hz(mix(pal.copper, '#000000', 0.35)), 0.5 * k, ' stroke-opacity="0.5"');
    g += pathEl(rectP(F.X(-17), F.Y(2.2), 34 * k, 3.4 * k), C.hz(pal.foreshore));
    L.add(4, F.X(-21), F.X(21), g);
    // 倒影
    const rr = new Rng(S.seed, 'near.bt.refl');
    let rc = '', rg = '';
    for (let y = yW + 4 * u; y < yW + 110 * u; y += rr.range(4, 7.5) * u) {
      const fade = (y - yW) / (110 * u);
      const w = rr.range(10, 16) * k * (1 - fade * 0.4);
      rc += rectP(btX - w + rr.range(-2, 2) * u, y, w * 0.55, 1.8 * u) + rectP(btX + w * 0.45 + rr.range(-2, 2) * u, y, w * 0.55, 1.8 * u);
      if (rr.chance(0.7)) rg += rectP(btX - 3 * k + rr.range(-2, 2) * u, y, 6 * k, 1.8 * u);
    }
    L.add(1.5, btX - 30 * k, btX + 30 * k, pathEl(rc, pal.copperLit, ' fill-opacity="0.3"') + pathEl(rg, pal.glass, ' fill-opacity="0.35"'));
  }

  // --- 渡轮（无字无标志）---
  {
    const f = P.ferry, k = s * f.scale, cx = f.x * W, wy = yW + 9 * u;
    const F = frame(cx, wy, k);
    let g = pathEl(poly(F.P([[-13, 0], [-14, 2.6], [14.5, 2.6], [12.5, 0]])), C.hz('#e9eef0'));
    g += pathEl(poly(F.P([[-9, 2.6], [-8.4, 5.6], [9.5, 5.6], [10.4, 2.6]])), C.hz('#f4f4ef'));
    g += pathEl(rectP(F.X(-7.6), F.Y(5), 16.6 * k, 1.5 * k), C.hz('#30485a'));
    g += pathEl(rectP(F.X(-13.5), F.Y(1.3), 27.6 * k, 0.7 * k), C.hz('#4b8a7a'));
    g += pathEl(rectP(F.X(-15), F.Y(0.2), 31 * k, 0.9 * k), pal.waterDeep, ' fill-opacity="0.45"');
    L.add(4.5, F.X(-16), F.X(16), g);
  }

  // --- 黑天鹅 ---
  for (const sw of P.swans) {
    const x = sw.x * W, y = yW + sw.dy * u, len = 44 * u * sw.s;
    L.add(5, x - len * 1.1, x + len * 1.1, swanSvg(x, y, len, sw.dir, pal) +
      pathEl(rectP(x - len * 0.6, y + 0.12 * len, len * 1.2, 1.3 * u), pal.waterHi, ' fill-opacity="0.4"'));
  }
  return L;
}

// =============================================================================
// 10. Rockingham 各层
// =============================================================================
function buildRockingham(S) {
  return { sky: buildSky(S), far: buildRockFar(S), mid: buildRockMid(S), near: buildRockNear(S) };
}

// 诺福克松：笔直树干 + 一层层水平伸出的枝叶（略下垂、梢头上翘），树冠窄而高，常有缺层
function norfolkPineD(rng, cx, groundY, h, u) {
  const tw = Math.max(1.6 * u, h * 0.013);
  const trunk = poly([[cx - tw, groundY + 3 * u], [cx - tw * 0.35, groundY - h * 0.95], [cx + tw * 0.35, groundY - h * 0.95], [cx + tw, groundY + 3 * u]]);
  let dark = '', lit = '';
  const crownBase = h * rng.range(0.17, 0.28); // 海边的松树下部枝条多已脱落，露出一段树干
  // 一层层“平台”：越往上越薄、越短、间距越小
  const nT = rng.int(9, 13), wts = [];
  for (let i = 0; i < nT; i++) wts.push(Math.pow(0.9, i) * rng.range(0.8, 1.2));
  const sum = wts.reduce((a, b) => a + b, 0), tiers = [];
  let y = crownBase;
  for (let i = 0; i < nT; i++) { tiers.push(y); y += (h * 0.92 - crownBase) * wts[i] / sum; }
  const lean = rng.range(-1, 1); // 被海风吹得略不对称
  let x0 = cx - tw, x1 = cx + tw;
  for (const ty of tiers) {
    const t = (ty - crownBase) / (h - crownBase);
    const maxW = h * (0.19 * Math.pow(1 - t, 0.85) + 0.03);
    const th = h * 0.05 * (1 - t * 0.55);
    const Y = groundY - ty;
    for (const side of [-1, 1]) {
      if (t > 0.18 && t < 0.88 && rng.chance(0.1)) continue; // 偶尔缺一枝：诺福克松的典型特征
      const len = maxW * rng.range(0.7, 1.05) * (1 + 0.15 * side * lean);
      const up = len * rng.range(0.08, 0.18), tipX = cx + side * len, tipY = Y - up;
      // 上沿：平直略上翘；下沿：一串向下鼓的“流苏”
      let d = mv([cx, Y - th * 0.3]) + `Q${fmt(cx + side * len * 0.6)} ${fmt(Y - th * 0.25)} ${fmt(tipX)} ${fmt(tipY)}`;
      const n = Math.max(2, Math.round(len / (th * 1.1)));
      let px = tipX, py = tipY;
      for (let k = 1; k <= n; k++) {
        const f = 1 - k / n, nx = cx + side * len * f, ny = Y + th * 0.25 - up * f * 0.6;
        d += `Q${fmt((px + nx) / 2)} ${fmt(Math.max(py, ny) + th * rng.range(0.55, 0.95))} ${fmt(nx)} ${fmt(ny)}`;
        px = nx; py = ny;
      }
      dark += d + 'Z';
      if (side < 0) { // 受光面：西侧（左）枝条的上沿
        lit += mv([cx, Y - th * 0.3]) + `Q${fmt(cx - len * 0.6)} ${fmt(Y - th * 0.25)} ${fmt(tipX)} ${fmt(tipY)}` +
          `Q${fmt(cx - len * 0.55)} ${fmt(Y + th * 0.05)} ${fmt(cx)} ${fmt(Y + th * 0.12)}Z`;
      }
      x0 = Math.min(x0, tipX - th); x1 = Math.max(x1, tipX + th);
    }
  }
  dark += poly([[cx - h * 0.011, groundY - h * 0.9], [cx, groundY - h * 1.0], [cx + h * 0.011, groundY - h * 0.9]]);
  return { trunk, dark, lit, x0, x1 };
}

// 胡椒薄荷树（WA 海边常见）：宽而低、略下垂的树冠
function peppermintD(rng, cx, groundY, h, u) {
  const w = h * rng.range(1.3, 1.9), tw = Math.max(1.6 * u, h * 0.05);
  let d = poly([[cx - tw, groundY + 2 * u], [cx - tw * 0.6, groundY - h * 0.5], [cx + tw * 0.6, groundY - h * 0.5], [cx + tw, groundY + 2 * u]]);
  const n = rng.int(4, 6);
  for (let i = 0; i < n; i++) {
    const f = i / (n - 1) - 0.5;
    const r = h * rng.range(0.24, 0.34);
    d += ellipseP(cx + f * (w - 2 * r), groundY - h + r * 1.1 + Math.abs(f) * h * 0.35, r * 1.15, r * 0.85);
  }
  return { d, x0: cx - w / 2 - 4 * u, x1: cx + w / 2 + 4 * u };
}

// 远处的岛 / 岬角：返回轮廓多边形 + 石灰岩崖面
function landformD(S, name, x0, x1, baseY, height, shape, cliff) {
  const u = S.u, rng = new Rng(S.seed, `land.${name}`);
  const span = x1 - x0, nz = periodicNoise(rng, span, 2, 18, 0.9), nf = periodicNoise(rng, span, 30, 90, 0.6);
  const prof = [];
  for (let x = x0; x <= x1 + 0.01; x += 2 * u) {
    const t = (x - x0) / span;
    prof.push([x, baseY - height * u * shape(t) * (0.86 + 0.1 * nz(x - x0) + 0.05 * nf(x - x0))]);
  }
  const d = poly([[x0, baseY + 2 * u], ...prof, [x1, baseY + 2 * u]]);
  let cd = '';
  if (cliff) { // cliff: [t0, t1]，在这段陡坡上画浅色崖面
    const top = prof.filter(([x]) => { const t = (x - x0) / span; return t >= cliff[0] && t <= cliff[1]; });
    if (top.length > 1) {
      // 两端渐收，避免出现竖直的硬边（看起来像个方块）
      const r = (cliff[1] - cliff[0]) * 0.4;
      const lowered = top.map(([x, y]) => {
        const t = (x - x0) / span, f = smoothstep(cliff[0], cliff[0] + r, t) * (1 - smoothstep(cliff[1] - r, cliff[1], t));
        const yy = y + (baseY - y) * 0.15 + 2 * u;
        return [x, baseY - (baseY - yy) * f];
      });
      cd = poly([...lowered, [top[top.length - 1][0], baseY], [top[0][0], baseY]]);
    }
  }
  return { d, cd, prof };
}

function buildRockFar(S) {
  const L = new Layer(S, 'far'), P = S.p.far, { W, H, u } = S;
  const yH = S.p.horizon * H, C = layerCtx(S, P, yH), pal = C.pal;
  // --- 远海：海平线到底部 ---
  const matSea = L.mat('sea', C.hz(pal.seaFar, 0.15), C.hz(pal.sea, -0.1), yH, H);
  L.addPeriodic(0, () => `<rect x="-4" y="${fmt(yH)}" width="${W + 8}" height="${fmt(H - yH + 4)}" fill="${matSea}"/>` +
    `<rect x="-4" y="${fmt(yH)}" width="${W + 8}" height="${fmt(1.6 * u)}" fill="${pal.seaHi}" fill-opacity="0.55"/>`);
  {
    const rng = new Rng(S.seed, 'rk.far.shimmer');
    for (let i = 0; i < P.shimmer.count; i++) {
      const t = rng.f(), y = yH + 4 * u + (H - yH) * 0.5 * t, len = lerp(6, 30, t) * u * rng.range(0.5, 1.3), x = rng.range(0, W);
      L.add(0.5, x - len / 2, x + len / 2, pathEl(rectP(x - len / 2, y, len, lerp(1, 2, t) * u), pal.seaHi, ` fill-opacity="${fmt(rng.range(0.2, 0.45))}"`));
    }
  }
  const landC = C.hz(pal.land, 0.06), cliffC = C.hz(pal.limestone, -0.05), shoreC = C.hz(pal.seaDeep, 0.2);
  const addLand = (name, x0f, x1f, hgt, shape, cliff) => {
    const x0 = x0f * W, x1 = (x1f < x0f ? x1f + 1 : x1f) * W;
    const g = landformD(S, name, x0, x1, yH + 1.5 * u, hgt, shape, cliff);
    L.add(2, x0, x1, pathEl(g.d, landC) + (g.cd ? pathEl(g.cd, cliffC, ' fill-opacity="0.85"') : '') +
      pathEl(rectP(x0, yH - 1 * u, x1 - x0, 2.6 * u), shoreC, ' fill-opacity="0.5"'));
    return g;
  };
  const gi = P.gardenIsland, pp = P.pointPeron, pi = P.penguinIsland, fc = P.farCoast;
  addLand('garden', gi.x0, gi.x1, gi.height, (t) => smoothstep(0, 0.05, t) * (1 - smoothstep(0.92, 1, t)) * (0.6 + 0.4 * (0.5 + 0.5 * Math.sin(2 * Math.PI * (t * 3.3 + 0.15)))));
  addLand('peron', pp.x0, pp.x1, pp.height, (t) => Math.pow(smoothstep(0, 0.45, t), 1.2) * (1 - smoothstep(0.84, 1.0, t)) * (0.85 + 0.15 * Math.sin(Math.PI * t * 2.2)), [0.7, 1.0]);
  addLand('penguin', pi.x - pi.w / 2, pi.x + pi.w / 2, pi.height, (t) => Math.pow(Math.sin(Math.PI * Math.pow(t, 1.35)), 0.55) * (0.9 + 0.1 * Math.sin(t * 9)), [0.0, 0.34]);
  for (const [x, w, h] of P.rocks) addLand(`rock${x}`, x - w / 2, x + w / 2, h, (t) => Math.pow(Math.sin(Math.PI * t), 0.7));
  addLand('coast', fc.x0, fc.x1, fc.height, (t) => smoothstep(0, 0.08, t) * (1 - smoothstep(0.9, 1, t)) * 0.8 + 0.2 * smoothstep(0, 0.08, t) * (1 - smoothstep(0.9, 1, t)));
  // Garden Island 堤道：贴着海面的一条细线，靠岛一端有个拱起的桥
  {
    const cx0 = P.causeway.x0 * W, cx1 = P.causeway.x1 * W, y = yH - 2.2 * u;
    let d = rectP(cx0, y, cx1 - cx0, 2 * u);
    const bx = lerp(cx0, cx1, 0.22);
    d += mv([bx - 26 * u, y + 0.5 * u]) + `Q${fmt(bx)} ${fmt(y - 12 * u)} ${fmt(bx + 26 * u)} ${fmt(y + 0.5 * u)}` + ln([bx + 26 * u, y + 2 * u]) +
      `Q${fmt(bx)} ${fmt(y - 9.5 * u)} ${fmt(bx - 26 * u)} ${fmt(y + 2 * u)}Z`;
    L.add(2, cx0, cx1, pathEl(d, C.hz(pal.ship, 0.15)));
  }
  // 远洋货轮（无字无标志）
  for (const sh of P.ships) {
    const x = sh.x * W, y = yH + 0.5 * u, k = sh.s * u;
    let d = poly([[x - 34 * k, y - 6 * k], [x + 30 * k, y - 6 * k], [x + 36 * k, y - 9 * k], [x + 33 * k, y], [x - 32 * k, y]]);
    for (let i = 0; i < 6; i++) d += rectP(x - 24 * k + i * 8.2 * k, y - (11 + (i % 2) * 2.5) * k, 7.4 * k, (5 + (i % 2) * 2.5) * k);
    d += rectP(x - 32 * k, y - 16 * k, 6 * k, 10 * k) + rectP(x - 30.5 * k, y - 20 * k, 2 * k, 4 * k);
    L.add(3, x - 36 * k, x + 38 * k, pathEl(d, C.hz(pal.ship, 0.12)));
  }
  // 远处帆船：小白三角
  for (const sl of P.sails) {
    const x = sl.x * W, y = yH + 2 * u, k = sl.s * u;
    const d = poly([[x - 1 * k, y - 3 * k], [x - 1 * k, y - 22 * k], [x - 9 * k, y - 3.5 * k]]) + poly([[x + 1 * k, y - 4 * k], [x + 1 * k, y - 18 * k], [x + 7 * k, y - 4.5 * k]]);
    L.add(3, x - 10 * k, x + 10 * k, pathEl(d, C.hz(pal.sail, -0.1)) + pathEl(rectP(x - 8 * k, y - 3 * k, 16 * k, 2.4 * k), C.hz(pal.ship, 0.1)));
  }
  return L;
}

function buildRockMid(S) {
  const L = new Layer(S, 'mid'), P = S.p.mid, { W, H, u } = S;
  const yH = S.p.horizon * H, C = layerCtx(S, P, yH), pal = C.pal;
  // --- 近海：顶部渐隐，和远海自然衔接 ---
  const y0 = yH + 10 * u, y1 = yH + 78 * u;
  const id = `${L.id}-sea`;
  L.defs.set(id, `<linearGradient id="${id}" gradientUnits="userSpaceOnUse" x1="0" y1="${fmt(y0)}" x2="0" y2="${fmt(H)}">` +
    `<stop offset="0" stop-color="${C.hz(pal.sea, 0.05)}" stop-opacity="0"/>` +
    `<stop offset="${fmt((y1 - y0) / (H - y0))}" stop-color="${C.hz(pal.sea)}" stop-opacity="1"/>` +
    `<stop offset="1" stop-color="${C.hz(pal.seaDeep)}"/></linearGradient>`);
  L.addPeriodic(0, () => `<rect x="-4" y="${fmt(y0)}" width="${W + 8}" height="${fmt(H - y0 + 4)}" fill="url(#${id})"/>`);
  {
    const rng = new Rng(S.seed, 'rk.mid.shimmer');
    for (let i = 0; i < P.shimmer.count; i++) {
      const t = rng.f(), y = y1 + (H - y1) * t, len = lerp(14, 70, t) * u * rng.range(0.5, 1.3), x = rng.range(0, W);
      L.add(1, x - len / 2, x + len / 2, pathEl(rectP(x - len / 2, y, len, lerp(1.4, 3, t) * u), pal.seaHi, ` fill-opacity="${fmt(rng.range(0.18, 0.42))}"`));
    }
  }
  // --- 木栈桥：桥面 + 栏杆 + 桩 + 灯柱，右端 T 形平台和小亭子 ---
  {
    const J = P.jetty, x0 = J.x0 * W, x1 = J.x1 * W, dy = J.deckY * H, wy = dy + 20 * u;
    const dark = C.hz(pal.jetty), lit = C.hz(pal.jettyLit);
    let piles = '', rails = '', refl = '';
    for (let x = x0 + 6 * u; x <= x1 - 2 * u; x += 17 * u) {
      piles += rectP(x - 1.6 * u, dy, 3.2 * u, wy - dy + 1 * u);
      refl += rectP(x - 1.4 * u, wy + 3 * u, 2.8 * u, 4 * u) + rectP(x - 1.1 * u, wy + 9 * u, 2.2 * u, 3 * u);
    }
    for (let x = x0 + 4 * u; x <= x1; x += 9 * u) rails += rectP(x - 0.6 * u, dy - 9 * u, 1.2 * u, 9 * u);
    let g = pathEl(refl, pal.seaDeep, ' fill-opacity="0.35"') + pathEl(piles, dark);
    g += pathEl(rectP(x0, dy - 1 * u, x1 - x0, 5 * u), dark) + pathEl(rectP(x0, dy - 1 * u, x1 - x0, 1.8 * u), lit);
    g += pathEl(rails + rectP(x0, dy - 9.5 * u, x1 - x0, 1.4 * u), dark, ' fill-opacity="0.85"');
    // T 形平台 + 小亭子
    const tx = x1 - 30 * u;
    g += pathEl(rectP(tx - 40 * u, dy - 1 * u, 80 * u, 6 * u), dark) + pathEl(rectP(tx - 40 * u, dy - 1 * u, 80 * u, 1.8 * u), lit);
    for (let x = tx - 38 * u; x <= tx + 38 * u; x += 12 * u) g += pathEl(rectP(x - 1.6 * u, dy + 5 * u, 3.2 * u, wy - dy - 4 * u), dark);
    g += pathEl(poly([[tx - 18 * u, dy - 20 * u], [tx, dy - 30 * u], [tx + 18 * u, dy - 20 * u]]), C.hz(pal.roof)) +
      pathEl(rectP(tx - 14 * u, dy - 20 * u, 2 * u, 19 * u) + rectP(tx + 12 * u, dy - 20 * u, 2 * u, 19 * u), dark);
    // 灯柱
    let lamps = '';
    for (let x = x0 + 60 * u; x < x1 - 50 * u; x += 110 * u) lamps += rectP(x - 0.9 * u, dy - 26 * u, 1.8 * u, 26 * u) + rectP(x - 3.4 * u, dy - 28 * u, 6.8 * u, 2.4 * u);
    g += pathEl(lamps, dark);
    L.add(2, x0 - 2 * u, x1 + 30 * u, g);
  }
  // --- 停泊的船 ---
  for (const b of P.boats) {
    const x = b.x * W, k = b.s * u, wl = yH + 70 * u + (b.s - 0.85) * 40 * u;
    let g = '';
    if (b.type === 'yacht') {
      g += pathEl(poly([[x - 22 * k, wl - 7 * k], [x + 24 * k, wl - 7 * k], [x + 18 * k, wl], [x - 18 * k, wl]]), C.hz(pal.hull));
      g += pathEl(rectP(x - 18 * k, wl - 4 * k, 38 * k, 1.4 * k), C.hz(pal.awning));
      g += pathEl(rectP(x - 9 * k, wl - 11 * k, 14 * k, 4 * k), C.hz(pal.hull, 0.05));
      g += pathEl(rectP(x - 0.8 * k, wl - 62 * k, 1.6 * k, 55 * k), C.hz(pal.mast));
      if (b.sail) g += pathEl(poly([[x + 1.5 * k, wl - 60 * k], [x + 1.5 * k, wl - 12 * k], [x + 24 * k, wl - 13 * k]]), C.hz(pal.sail)) +
        pathEl(poly([[x - 1.5 * k, wl - 54 * k], [x - 1.5 * k, wl - 12 * k], [x - 17 * k, wl - 12.5 * k]]), C.hz(mix(pal.sail, pal.wallShade, 0.4)));
      else g += pathEl(rectP(x - 1 * k, wl - 22 * k, 13 * k, 3 * k), C.hz(pal.awning, 0.1));
    } else {
      g += pathEl(poly([[x - 18 * k, wl - 6 * k], [x + 20 * k, wl - 6 * k], [x + 14 * k, wl], [x - 16 * k, wl]]), C.hz(pal.hull));
      g += pathEl(poly([[x - 8 * k, wl - 6 * k], [x - 6 * k, wl - 13 * k], [x + 6 * k, wl - 13 * k], [x + 9 * k, wl - 6 * k]]), C.hz(pal.hull, 0.04)) +
        pathEl(rectP(x - 5 * k, wl - 11.5 * k, 10 * k, 3 * k), C.hz(pal.glass));
    }
    g += pathEl(rectP(x - 20 * k, wl + 1.5 * k, 40 * k, 1.6 * k), pal.seaDeep, ' fill-opacity="0.35"');
    L.add(3, x - 26 * k, x + 28 * k, g);
  }
  return L;
}

function buildRockNear(S) {
  const L = new Layer(S, 'near'), P = S.p.near, { W, H, u } = S;
  const yWl = P.waterline * H, yG = P.grassY * H, C = layerCtx(S, P, yG), pal = C.pal;
  // --- 浅水（顶部渐隐）+ 浪花线 + 沙滩 + 草坪 ---
  const ys0 = yWl - 46 * u;
  const sid = `${L.id}-shallow`;
  L.defs.set(sid, `<linearGradient id="${sid}" gradientUnits="userSpaceOnUse" x1="0" y1="${fmt(ys0)}" x2="0" y2="${fmt(yWl)}">` +
    `<stop offset="0" stop-color="${C.hz(pal.seaShallow)}" stop-opacity="0"/><stop offset="0.6" stop-color="${C.hz(pal.seaShallow)}" stop-opacity="0.85"/>` +
    `<stop offset="1" stop-color="${C.hz(mix(pal.seaShallow, pal.sandWet, 0.35))}" stop-opacity="1"/></linearGradient>`);
  const matSand = L.mat('sand', C.hz(pal.sandWet), C.hz(pal.sandDry), yWl, yG);
  const matGrass = L.mat('grass', C.hz(mix(pal.grass, pal.sandDry, 0.1)), C.hz(mix(pal.grass, '#000000', 0.15)), yG, H);
  const wv = periodicNoise(new Rng(S.seed, 'rk.near.waves'), W, 6, 40, 0.7);
  const wv2 = periodicNoise(new Rng(S.seed, 'rk.near.waves2'), W, 8, 50, 0.7);
  const foam = (x) => yWl + 1.5 * u + 2.2 * u * wv(x);
  const foam2 = (x) => yWl - 13 * u + 2.5 * u * wv2(x);
  L.addPeriodic(0, (sh) => {
    let s = `<rect x="-4" y="${fmt(ys0)}" width="${W + 8}" height="${fmt(yWl - ys0 + 2 * u)}" fill="url(#${sid})"/>`;
    s += pathEl(terrainPath(S, foam, sh, H + 4), matSand);
    // 两道浪花：贴岸一道实、外侧一道淡
    const band = (f, th) => { const step = S.p.sampleStep, top = [], bot = []; for (let x = -2 * step; x <= W + 2 * step; x += step) { top.push([x, f(x - sh) - th / 2]); bot.push([x, f(x - sh) + th / 2]); } return poly([...top, ...bot.reverse()]); };
    s += pathEl(band(foam, 2.6 * u), pal.seaHi, ' fill-opacity="0.9"') + pathEl(band(foam2, 1.8 * u), pal.seaHi, ' fill-opacity="0.45"');
    s += `<rect x="-4" y="${fmt(yG)}" width="${W + 8}" height="${fmt(H - yG + 4)}" fill="${matGrass}"/>`;
    s += `<rect x="-4" y="${fmt(yG - 1.5 * u)}" width="${W + 8}" height="${fmt(3 * u)}" fill="${C.hz(mix(pal.grass, '#000000', 0.25))}" fill-opacity="0.5"/>`;
    s += `<rect x="-4" y="${fmt(yG + 26 * u)}" width="${W + 8}" height="${fmt(9 * u)}" fill="${C.hz(pal.path)}"/>`;
    return s;
  });
  // --- 保留区（不种树，保证远处 Penguin Island / 栈桥头可见） ---
  const keep = P.pines.keepOut.map(([x, hw]) => [x * W, hw * W]);
  const blocked = (x, pad) => keep.some(([kx, hw]) => Math.abs(wrapDelta(x, kx, W)) < hw + pad);
  // --- 海边小楼 ---
  for (const b of P.buildings) {
    const cx = b.x * W, w = b.w * u, h = b.h * u, x0 = cx - w / 2, top = yG - h;
    let g = pathEl(rectP(x0, top, w, h + 3 * u), C.hz(pal.wall));
    g += pathEl(rectP(x0 + w * 0.62, top, w * 0.38, h + 3 * u), C.hz(pal.wallShade), ' fill-opacity="0.6"');
    const fh = h / b.floors;
    let win = '';
    for (let i = 0; i < b.floors; i++) {
      const fy = top + i * fh;
      if (i === b.floors - 1) continue;
      for (let x = x0 + 8 * u; x < x0 + w - 12 * u; x += 16 * u) win += rectP(x, fy + fh * 0.3, 10 * u, fh * 0.42);
      win += rectP(x0, fy + fh - 1.4 * u, w, 1.4 * u);
    }
    g += pathEl(win, C.hz(pal.glass), ' fill-opacity="0.75"');
    g += pathEl(poly([[x0 - 4 * u, yG - fh + 2 * u], [x0 + w + 4 * u, yG - fh + 2 * u], [x0 + w + 6 * u, yG - fh * 0.62], [x0 - 6 * u, yG - fh * 0.62]]), C.hz(pal.awning));
    if (b.roof === 'pitched') g += pathEl(poly([[x0 - 6 * u, top + 1 * u], [cx, top - w * 0.22], [x0 + w + 6 * u, top + 1 * u]]), C.hz(pal.roof));
    else g += pathEl(rectP(x0 - 3 * u, top - 4 * u, w + 6 * u, 5 * u), C.hz(mix(pal.wall, pal.wallShade, 0.7)));
    L.add(2, x0 - 8 * u, x0 + w + 8 * u, g);
  }
  // --- 野餐亭 ---
  for (const s of P.shelters) {
    const cx = s.x * W, k = u;
    const g = pathEl(rectP(cx - 20 * k, yG - 26 * k, 2.4 * k, 26 * k) + rectP(cx + 17.6 * k, yG - 26 * k, 2.4 * k, 26 * k), C.hz(pal.trunk)) +
      pathEl(poly([[cx - 28 * k, yG - 24 * k], [cx, yG - 40 * k], [cx + 28 * k, yG - 24 * k]]), C.hz(pal.roof)) +
      pathEl(rectP(cx - 12 * k, yG - 9 * k, 24 * k, 3 * k), C.hz(pal.trunk));
    L.add(2.2, cx - 30 * k, cx + 30 * k, g);
  }
  // --- 矮树（胡椒薄荷树）---
  {
    const rng = new Rng(S.seed, 'rk.near.trees'), T = P.trees;
    const mat = L.mat('tree', C.hz(pal.tree), C.hz(mix(pal.tree, '#000000', 0.15)), yG - 70 * u, yG);
    for (let i = 0; i < T.count; i++) {
      const x = (i + rng.range(0.1, 0.9)) / T.count * W;
      if (blocked(x, 10 * u)) continue;
      const t = peppermintD(rng, x, yG + 3 * u, rng.range(T.h[0], T.h[1]) * u, u);
      L.add(2.5, t.x0, t.x1, pathEl(t.d, mat));
    }
  }
  // --- 诺福克松 ---
  {
    const rng = new Rng(S.seed, 'rk.near.pines'), T = P.pines;
    const matPine = L.mat('pine', C.hz(pal.pine), C.hz(mix(pal.pine, '#000000', 0.1)), yG - T.h[1] * u, yG);
    const matLit = L.mat('pinelit', C.hz(pal.pineLit), C.hz(pal.pineLit, 0.1), yG - T.h[1] * u, yG);
    const trunkC = C.hz(pal.trunk);
    // 成组种植：每组 1~3 棵，组与组之间留出看海的空当
    const list = [];
    const avgGap = W / Math.max(1, T.count) * 1.9;
    let x = rng.range(0, avgGap);
    while (x < W && list.length < T.count) {
      const n = rng.weighted([[1, 3], [2, 4], [3, 2]]);
      let gx = x;
      for (let i = 0; i < n && list.length < T.count; i++) {
        const h = rng.range(T.h[0], T.h[1]) * u * (i === 0 ? 1 : rng.range(0.75, 0.95));
        if (gx < W && !blocked(gx, 8 * u)) list.push({ x: gx, h });
        gx += rng.range(55, 105) * u;
      }
      x = gx + avgGap * rng.range(0.55, 1.25);
    }
    list.sort((a, b) => b.h - a.h); // 高的在后
    for (const p of list) {
      const t = norfolkPineD(new Rng(S.seed, `rk.pine.${Math.round(p.x)}`), p.x, yG + 4 * u, p.h, u);
      L.add(3, t.x0, t.x1, pathEl(t.trunk, trunkC) + pathEl(t.dark, matPine) + pathEl(t.lit, matLit, ' fill-opacity="0.9"'));
    }
  }
  // --- 海鸥 ---
  for (const g of P.gulls) {
    const x = g.x * W, y = g.y * H, k = 9 * u * g.s;
    const d = mv([x - k, y]) + `Q${fmt(x - k * 0.5)} ${fmt(y - k * 0.55)} ${fmt(x)} ${fmt(y)}` + `Q${fmt(x + k * 0.5)} ${fmt(y - k * 0.55)} ${fmt(x + k)} ${fmt(y)}`;
    L.add(4, x - k - 2 * u, x + k + 2 * u, strokeEl(d, pal.gull, 1.8 * u * g.s, ' stroke-linecap="round" stroke-linejoin="round"'));
  }
  return L;
}

// =============================================================================
// 11. 场景注册
// =============================================================================
const SCENES = {
  perth: { params: PERTH, build: buildPerth },
  rockingham: { params: ROCKINGHAM, build: buildRockingham },
};

// =============================================================================
// 12. 渲染 / PNG 编码 / 输出
// =============================================================================
function makeScene(key, seed, W, H) {
  const def = SCENES[key];
  const S = { key, p: def.params, seed: (seed + hashStr(key)) >>> 0, W, H, u: H / 1024 };
  S.layers = def.build(S);
  return S;
}
const LAYER_ORDER = ['sky', 'far', 'mid', 'near'];

function layerSvg(S, name, shift = 0) {
  const L = S.layers[name];
  return svgDoc(S.W, S.H, L.defsSvg(), L.body(shift), `CityScooter ${S.p.title} — ${name} layer (generated by generator.mjs, seed ${CONFIG.seed})`);
}
function compositeSvg(S, shift = 0) {
  const defs = LAYER_ORDER.map(n => S.layers[n].defsSvg()).join('');
  const body = LAYER_ORDER.map(n => `<g id="${S.layers[n].id}">\n${S.layers[n].body(shift)}\n</g>`).join('\n');
  return svgDoc(S.W, S.H, defs, body, `CityScooter ${S.p.title} — composite (review only)`);
}

function render(svg, width) {
  const r = new Resvg(svg, { fitTo: { mode: 'width', value: width }, font: { loadSystemFonts: false }, shapeRendering: 2 });
  const img = r.render();
  return { w: img.width, h: img.height, px: Buffer.from(img.pixels) }; // 预乘 alpha 的 RGBA
}

// --- 最小 PNG 编码器（zlib 9 + 自适应行滤波）---
const CRC_TABLE = (() => {
  const t = new Uint32Array(256);
  for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = c & 1 ? 0xEDB88320 ^ (c >>> 1) : c >>> 1; t[n] = c >>> 0; }
  return t;
})();
function crc32(buf) { let c = 0xFFFFFFFF; for (let i = 0; i < buf.length; i++) c = CRC_TABLE[(c ^ buf[i]) & 0xFF] ^ (c >>> 8); return (c ^ 0xFFFFFFFF) >>> 0; }
function pngChunk(type, data) {
  const len = Buffer.alloc(4); len.writeUInt32BE(data.length);
  const td = Buffer.concat([Buffer.from(type, 'ascii'), data]);
  const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td));
  return Buffer.concat([len, td, crc]);
}
function encodePNG(px, w, h, ch) {
  const stride = w * ch, raw = Buffer.alloc((stride + 1) * h), zero = new Uint8Array(stride);
  const cand = [0, 1, 2, 3, 4].map(() => new Uint8Array(stride));
  for (let y = 0; y < h; y++) {
    const row = px.subarray(y * stride, (y + 1) * stride), up = y ? px.subarray((y - 1) * stride, y * stride) : zero;
    let best = 0, bestSum = Infinity;
    for (let f = 0; f < 5; f++) {
      const o = cand[f]; let sum = 0;
      for (let i = 0; i < stride; i++) {
        const a = i >= ch ? row[i - ch] : 0, b = up[i], c = i >= ch ? up[i - ch] : 0;
        let pred;
        if (f === 0) pred = 0; else if (f === 1) pred = a; else if (f === 2) pred = b; else if (f === 3) pred = (a + b) >> 1;
        else { const p = a + b - c, pa = Math.abs(p - a), pb = Math.abs(p - b), pc = Math.abs(p - c); pred = pa <= pb && pa <= pc ? a : pb <= pc ? b : c; }
        const v = (row[i] - pred) & 0xFF; o[i] = v; sum += v < 128 ? v : 256 - v;
        if (sum >= bestSum) break;
      }
      if (sum < bestSum) { bestSum = sum; best = f; }
    }
    raw[y * (stride + 1)] = best; raw.set(cand[best], y * (stride + 1) + 1);
  }
  const ihdr = Buffer.alloc(13);
  ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = ch === 4 ? 6 : 2;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), pngChunk('IHDR', ihdr),
    pngChunk('IDAT', zlib.deflateSync(raw, { level: 9, memLevel: 9 })), pngChunk('IEND', Buffer.alloc(0))]);
}
// 预乘 → 直通 alpha；可选：把完全透明像素的 RGB 用同一列最近的不透明颜色填充（防黑边/压缩更友好）
function toStraight(pm, w, h, bleed) {
  const d = Buffer.from(pm);
  for (let i = 0; i < d.length; i += 4) {
    const a = d[i + 3];
    if (a === 0) { d[i] = d[i + 1] = d[i + 2] = 0; }
    else if (a < 255) { for (let c = 0; c < 3; c++) d[i + c] = Math.min(255, Math.round(d[i + c] * 255 / a)); }
  }
  if (bleed) {
    for (let x = 0; x < w; x++) {
      let src = -1;
      for (let y = h - 1; y >= 0; y--) {
        const i = (y * w + x) * 4;
        if (d[i + 3] >= 160) src = i;
        else if (d[i + 3] === 0 && src >= 0) { d[i] = d[src]; d[i + 1] = d[src + 1]; d[i + 2] = d[src + 2]; }
      }
    }
  }
  return d;
}
function toRGB(pm) { const n = pm.length / 4, d = Buffer.alloc(n * 3); for (let i = 0; i < n; i++) { d[i * 3] = pm[i * 4]; d[i * 3 + 1] = pm[i * 4 + 1]; d[i * 3 + 2] = pm[i * 4 + 2]; } return d; }

// 天空渐变在 t(0~1) 处的颜色（用来给 Unity 推荐雾颜色）
function skyAt(stops, t) {
  for (let i = 0; i < stops.length - 1; i++) {
    const [o0, c0] = stops[i], [o1, c1] = stops[i + 1];
    if (t >= o0 && t <= o1) return mix(c0, c1, (t - o0) / Math.max(1e-6, o1 - o0));
  }
  return stops[t < stops[0][0] ? 0 : stops.length - 1][1];
}
// 推荐的视差系数（相对近景 = 1）
const PARALLAX = { sky: 0, far: 0.15, mid: 0.4, near: 1 };
function sceneMeta(S) {
  const pal = S.p.palette;
  return {
    title: S.p.title, horizon: S.p.horizon, haze: pal.haze,
    skyTop: pal.sky[0][1], fog: skyAt(pal.sky, S.p.horizon - 0.02), skyStops: pal.sky,
    parallax: PARALLAX, canvas: [S.W, S.H],
  };
}

function writeFile(rel, data) {
  const p = path.resolve(HERE, CONFIG.outDir, rel);
  fs.writeFileSync(p, data);
  return { rel: path.relative(HERE, p), bytes: fs.statSync(p).size };
}

function exportScene(S, { svgOnly }) {
  const files = [];
  for (const name of ['far', 'mid', 'near']) files.push(writeFile(`${S.key}_${name}.svg`, layerSvg(S, name)));
  files.push(writeFile(`${S.key}_composite.svg`, compositeSvg(S)));
  if (svgOnly) return files;
  let tiledSrc = null;
  for (const ex of CONFIG.exports) {
    for (const name of LAYER_ORDER) {
      const r = render(layerSvg(S, name), ex.width);
      const png = name === 'sky' ? encodePNG(toRGB(r.px), r.w, r.h, 3) : encodePNG(toStraight(r.px, r.w, r.h, CONFIG.bleedAlpha), r.w, r.h, 4);
      files.push(writeFile(`${S.key}_${name}${ex.suffix}.png`, png));
    }
    const rc = render(compositeSvg(S), ex.width);
    files.push(writeFile(`${S.key}_composite${ex.suffix}.png`, encodePNG(toRGB(rc.px), rc.w, rc.h, 3)));
    if (ex.width === CONFIG.tiledCheckWidth) tiledSrc = rc;
  }
  if (tiledSrc) { // 两张合成图并排：检查左右接缝
    const { w, h } = tiledSrc, rgb = toRGB(tiledSrc.px), out = Buffer.alloc(w * 2 * h * 3);
    for (let y = 0; y < h; y++) { const row = rgb.subarray(y * w * 3, (y + 1) * w * 3); out.set(row, y * w * 6); out.set(row, y * w * 6 + w * 3); }
    files.push(writeFile(`${S.key}_composite_tiled-check.png`, encodePNG(out, w * 2, h, 3)));
  }
  return files;
}

// 无缝自检：把整幅内容平移半个画布重新生成，与“原图循环移位”逐像素比较
function verifyScene(S) {
  const width = 2048, half = width / 2, results = [];
  for (const name of ['far', 'mid', 'near', 'composite']) {
    const svgA = name === 'composite' ? compositeSvg(S, 0) : layerSvg(S, name, 0);
    const svgB = name === 'composite' ? compositeSvg(S, S.W / 2) : layerSvg(S, name, S.W / 2);
    const A = render(svgA, width), B = render(svgB, width);
    // 说明：同一个形状放在不同的绝对位置，抗锯齿边缘会有 1~20 级的浮点舍入差（肉眼不可见）；
    // 真正的接缝错误（漏画了跨边缘的副本）会造成成片的大差异。所以分两档统计。
    let maxd = 0, nd = 0, nBig = 0, seamD = 0;
    const where = [];
    for (let y = 0; y < A.h; y++) for (let x = 0; x < width; x++) {
      const xa = (x - half + width) % width;
      const ia = (y * width + xa) * 4, ib = (y * width + x) * 4;
      let pd = 0;
      for (let c = 0; c < 4; c++) { const d = Math.abs(A.px[ia + c] - B.px[ib + c]); if (d > pd) pd = d; }
      if (pd > maxd) maxd = pd;
      if (pd > 2) nd++;
      if (pd > 40) { nBig++; if (where.length < 12) where.push(`(${xa},${y})`); }
      if (x === half) { // 原图左右边缘在 B 中相邻的位置：衡量“接缝处的跳变”与普通相邻列的差异
        const i0 = (y * width + half - 1) * 4, i1 = ib;
        for (let c = 0; c < 3; c++) seamD += Math.abs(B.px[i0 + c] - B.px[i1 + c]);
      }
    }
    results.push({ layer: name, maxDiff: maxd, aaNoise: nd, structural: nBig, seamColumnDelta: +(seamD / A.h / 3).toFixed(2), where });
  }
  return results;
}

// =============================================================================
// 13. 命令行
// =============================================================================
function main() {
  const args = process.argv.slice(2);
  const get = (k) => { const i = args.indexOf(k); return i >= 0 ? args[i + 1] : undefined; };
  const seed = get('--seed') != null ? Number(get('--seed')) >>> 0 : CONFIG.seed;
  CONFIG.seed = seed;
  if (get('--out')) CONFIG.outDir = path.resolve(process.cwd(), get('--out')); // 试种子时输出到别处，不覆盖 out/
  const sceneArg = get('--scene') || 'all';
  const keys = sceneArg === 'all' ? Object.keys(SCENES) : sceneArg.split(',');
  for (const k of keys) if (!SCENES[k]) { console.error(`未知场景: ${k}（可选：${Object.keys(SCENES).join(', ')}）`); process.exit(1); }
  const { width: W, height: H } = CONFIG.canvas;
  if (W % 4 || H % 4) { console.error('画布宽高必须是 4 的倍数'); process.exit(1); }
  fs.mkdirSync(path.resolve(HERE, CONFIG.outDir), { recursive: true });
  const t0 = Date.now();
  // preview.html 通过 <script> 读取这个文件（file:// 下不能 fetch JSON），内容只依赖参数，保持确定性
  const metaPath = path.resolve(HERE, CONFIG.outDir, 'skyline-meta.js');
  const meta = { scenes: {} };
  try { const old = fs.readFileSync(metaPath, 'utf8').match(/=\s*(\{[\s\S]*\});/); if (old) meta.scenes = JSON.parse(old[1]).scenes || {}; } catch { /* 第一次生成 */ }
  for (const k of keys) {
    const S = makeScene(k, seed, W, H);
    meta.scenes[k] = { ...sceneMeta(S), seed };
    if (get('--debug-shift')) { // 调试：把平移半幅后的各层 SVG 写到指定目录
      const dir = get('--debug-shift'); fs.mkdirSync(dir, { recursive: true });
      for (const n of ['far', 'mid', 'near']) {
        fs.writeFileSync(path.join(dir, `${k}_${n}_A.svg`), layerSvg(S, n, 0));
        fs.writeFileSync(path.join(dir, `${k}_${n}_B.svg`), layerSvg(S, n, W / 2));
      }
    }
    const files = exportScene(S, { svgOnly: args.includes('--svg-only') });
    console.log(`\n[${k}] seed=${seed}`);
    for (const f of files) console.log(`  ${f.rel.padEnd(42)} ${(f.bytes / 1024).toFixed(1).padStart(8)} KB`);
    if (args.includes('--verify')) {
      console.log(`  无缝自检（平移半幅重新生成 vs 原图循环移位，2048 宽）：`);
      let allOk = true;
      for (const r of verifyScene(S)) {
        // 漏画一个跨边缘的副本 → 至少几十个像素大幅不同；个位数的强差异只是近水平曲线边缘的抗锯齿舍入
        const ok = r.structural <= 8 && r.aaNoise < 1000;
        allOk &&= ok;
        console.log(`    ${ok ? 'OK  ' : 'FAIL'} ${r.layer.padEnd(10)} 结构性差异像素=${r.structural}  抗锯齿舍入差像素=${r.aaNoise}  maxDiff=${r.maxDiff}  接缝列平均跳变=${r.seamColumnDelta}` +
          (r.where.length ? `  位置(原图 2k 坐标)：${r.where.join(' ')}` : ''));
      }
      if (!allOk) process.exitCode = 2;
    }
  }
  const ordered = { scenes: Object.fromEntries(Object.keys(SCENES).filter(k => meta.scenes[k]).map(k => [k, meta.scenes[k]])) };
  fs.writeFileSync(metaPath, `// 由 generator.mjs 自动生成，请勿手改。preview.html 用它显示场景列表、地平线颜色等。\nwindow.SKYLINE_META = ${JSON.stringify(ordered, null, 2)};\n`);
  console.log(`\n完成，用时 ${((Date.now() - t0) / 1000).toFixed(1)} s`);
}

main();
