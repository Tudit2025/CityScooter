# CityScooter 远景天际线（Perth CBD / Rockingham）

无限跑酷背景用的**扁平剪影**天际线：天空 + 远景 / 中景 / 近景三层，每层都能**左右无缝平铺**，在游戏里按不同速度滚动形成视差。

- **Perth CBD**（主场景）：站在南岸 South Perth 隔着天鹅河往北看。左（西）是 Kings Park，中间是 CBD，右（东）是 Matagarup 桥和 Optus 体育场。
- **Rockingham**（扩展）：学校所在地。前滩草坪 + 沙滩 + 诺福克松，海上有木栈桥和帆船，海平线上是 Garden Island、Point Peron、Penguin Island。

所有图都由 `generator.mjs` 从参数生成（SVG → PNG），**同样的参数 + 同样的种子 = 逐字节相同的输出**。技术美术可以直接改参数重出，也可以把 SVG 拿到 Illustrator / Figma / Inkscape 里手修。

> 本目录不放进 `Assets/`。导入 Unity 时请把需要的 PNG 复制到例如 `Assets/Art/Skyline/`，让 Unity 自己生成 `.meta`。

---

## 1. 文件一览

| 文件 | 尺寸 | 说明 |
|---|---|---|
| `out/<场景>_sky.png` / `-2k.png` | 4096×1024 / 2048×512 | 天空竖直渐变，**不透明**（RGB） |
| `out/<场景>_far.png` / `-2k.png` | 同上 | 远景层，透明 PNG（RGBA） |
| `out/<场景>_mid.png` / `-2k.png` | 同上 | 中景层，透明 PNG |
| `out/<场景>_near.png` / `-2k.png` | 同上 | 近景层，透明 PNG |
| `out/<场景>_composite.png` / `-2k.png` | 同上 | 四层叠好的合成图，**只用于审图** |
| `out/<场景>_composite_tiled-check.png` | 4096×512 | 2k 合成图左右并排两张，正中间就是接缝，用来肉眼检查无缝 |
| `out/<场景>_far/mid/near/composite.svg` | 4096×1024 | 矢量源文件，可在设计软件里手修 |
| `out/skyline-meta.js` | — | 生成器写出的颜色/地平线信息，`preview.html` 会读取 |
| `generator.mjs` | — | 生成器（参数都在文件最上面） |
| `preview.html` | — | 本地预览：双击打开，看动态视差效果 |

`<场景>` = `perth` 或 `rockingham`。**Web 版只需要导入 `-2k` 的 PNG**；4k 母版留作桌面版 / 以后放大用。

### 每层内容

| 层 | Perth | Rockingham |
|---|---|---|
| sky | 晴空蓝 → 地平线暖米色 | 同色系，海边略更通透 |
| far | Darling Range 远山（东侧更高）、远处楼群、Kings Park 山脊（树冠 + 朝城市一侧的石灰岩崖面）、州战争纪念碑方尖碑、DNA 塔 | 远海、Garden Island（长条低岛）+ 堤道、Point Peron 岬角、Penguin Island、小礁石、远洋货轮、小帆船 |
| mid | CBD 楼群（前后两排）、Central Park、Brookfield Place、QV1、Perth Arena、Matagarup 桥、Optus 体育场、低矮街区 | 近海（顶部渐隐，与远海自然衔接）、木栈桥（T 形平台 + 小亭子）、停泊的帆船 / 小艇 |
| near | 天鹅河水面（波光、倒影）、岸线与树（圆冠树 / 桉树 / 棕榈）、Elizabeth Quay 步行桥、Spanda 雕塑、天鹅钟楼 Bell Tower、渡轮、黑天鹅 | 浅水与浪花线、沙滩、草坪与步道、诺福克松、胡椒薄荷树、海边小楼、野餐亭、海鸥 |

每层的**底部一直填到贴图下边缘（不透明）**，所以视差滑动时层与层之间不会露出空隙。

---

## 2. 重新生成

需要 Node.js 22（已在 Node 22.18 上测试）。在本目录下：

```bash
npm ci                         # 第一次：按 package-lock.json 安装 @resvg/resvg-js 2.6.2（版本已锁死）
node generator.mjs             # 生成全部场景到 out/（约 4 秒）
node generator.mjs --scene perth            # 只生成 Perth（rockingham 同理）
node generator.mjs --seed 42                # 换随机种子：楼的高矮/位置、树、波光会变；地标位置不变
node generator.mjs --svg-only               # 只出 SVG，调参时更快
node generator.mjs --verify                 # 额外做无缝自检（见第 4 节）
node generator.mjs --seed 7 --out .tmp/seed7  # 试种子时输出到别处，不覆盖 out/（.tmp 已被 gitignore）
```

`npm run build` / `npm run perth` / `npm run rockingham` / `npm run svg` / `npm run verify` 是上面命令的简写。

**改完参数的工作流**：改 `generator.mjs` 顶部参数 → `node generator.mjs` → 打开 `preview.html` 看效果（刷新即可）→ 满意后重新导入 Unity。

### 预览页 `preview.html`

直接双击用浏览器打开（不需要服务器、不联网）。可以切换场景 / 2k·4k、调速度和每层的视差系数、放大到接近游戏视野、显示每层的接缝线、叠一条假路面看前后景对比。页面下方有静态合成图、拼接检查图和雾颜色参考。
状态可以写在网址 `#` 后面分享，例如 `preview.html#scene=rockingham&zoom=2&seams=1&road=1&res=4k&speed=300`。空格键暂停/播放。

---

## 3. 参数说明（`generator.mjs` 顶部）

约定：
- 横向位置 `x` 都是 **0~1 的比例**（0 = 贴图左边缘 = 西，1 = 右边缘 = 东）。
- “像素”类参数都以 **1024 高的画布**为基准；改 `canvas` 尺寸会自动等比缩放。
- 楼宇 / 地标的尺寸用**米**（乘以该层的 `pxPerM` 变成像素），方便对照真实建筑。
- 颜色都是 `#rrggbb`。

### 3.1 全局 `CONFIG`

| 参数 | 默认 | 作用 |
|---|---|---|
| `seed` | `20261109` | 随机种子（首次上课日期）。影响楼群/树/波光等随机细节 |
| `canvas` | `4096×1024` | 母版尺寸。宽高须为 4 的倍数；保持 4:1 构图最佳 |
| `exports` | 4096 与 2048 | 每张 PNG 导出哪些宽度；`suffix` 是文件名后缀 |
| `outDir` | `out` | 输出目录 |
| `bleedAlpha` | `true` | 把完全透明像素的 RGB 填成同一列最近的不透明颜色。**防止引擎里出现黑边**，也让压缩更友好 |
| `tiledCheckWidth` | `2048` | 拼接检查图用哪档分辨率 |

### 3.2 场景通用（`PERTH` / `ROCKINGHAM`）

| 参数 | 作用 |
|---|---|
| `title` | 预览页里显示的名字 |
| `horizon` | 地平线 / 水面线的高度（占画布高度比例）。Perth = 0.845（对岸岸线），Rockingham = 0.705（海平线）。**在 Unity 里要把这条线对齐到游戏的地平线** |
| `sampleStep` | 连续地形的采样步长（像素），必须能整除画布宽的一半 |
| `palette.sky` | 天空渐变色标 `[位置, 颜色]`，0 = 顶，1 = 底 |
| `palette.haze` | **大气透视色**。各层颜色会按 `haze` 比例往它混合，越远越淡越蓝 |
| `palette.*` | 其它材质色（楼宇暗面/受光面、水、铜、玻璃、树……），见代码注释 |
| `far/mid/near.haze` | 该层整体往大气透视色混合的比例（0 = 原色，1 = 全变成雾色）。远景 0.5~0.6，中景 0.15~0.25，近景 ≈0.05 |
| `far/mid/near.groundHaze` | 该层底部额外加的雾（模拟地面雾气，越往下越淡） |
| `far/mid/near.pxPerM` | 该层的比例尺：每米多少像素。数值越大，该层的东西越大 |

### 3.3 Perth 专用

**远景 `PERTH.far`**

| 参数 | 作用 |
|---|---|
| `darling` | Darling Range 远山：`height` 最高高度(px)，`eastX` 最高处位置，`eastBoost` 东高西低的程度，`kMin/kMax` 起伏的频率范围 |
| `kingsPark` | Kings Park 山脊：`x0` 西侧坡脚 → `x1` 台地起点 → `x2` 悬崖边 → `x3` 东侧坡脚（可跨越 0/1 边界）；`height` 高度(px)；`canopy` 树冠大小倍数；`haze` 相对本层的雾量偏移（负值 = 更清楚、更绿） |
| `warMemorial` / `dnaTower` | 州战争纪念碑方尖碑 / DNA 塔：`x` 位置，`scale` 大小 |
| `zones` | 远处楼群分区：`x0/x1` 范围，`count` 楼数，`h/w` 高/宽范围（米），`peak/spread` 高楼集中的位置和范围 |
| `density` | 远处楼群总密度倍数 |

**中景 `PERTH.mid`**

| 参数 | 作用 |
|---|---|
| `baseY` | 楼宇地面线（会被近景岸线挡住） |
| `landmarks.*` | 六个地标的 `x`（位置）和 `scale`（大小）：`perthArena`、`qv1`、`brookfield`、`centralPark`、`matagarup`、`optusStadium` |
| `zones` | 通用楼群分区。`row: 'back'` 在地标后面（颜色更淡），`'front'` 在前面；`tone: 'light'` 是浅色住宅楼（East Perth）。其它字段同远景 |
| `density` | **楼群总密度倍数**（1.2 = 多 20% 的楼） |
| `fabric` | 楼脚的低矮街区：`h/w` 高宽范围（米），`treeChance` 其中种树的比例 |

> 地标有“保护区”：通用楼群若与地标横向重叠，后排楼会被压低到不挡住地标楼冠，前排楼会被压低到地标的下半部分；体育场、Perth Arena、Matagarup 桥前面不放楼。所以换种子不会把地标挡掉。

**近景 `PERTH.near`**

| 参数 | 作用 |
|---|---|
| `shore.height` | 对岸岸线的厚度(px) |
| `shore.gaps` | 岸线断开处（开阔水面）：EQ 内湾、东边河面（Matagarup 桥下） |
| `trees` | 岸边树：`density` 密度，`h` 高度范围(px)，`palmChance` 棕榈比例，`gumChance` 桉树比例 |
| `spanda` / `eqBridge` / `bellTower` / `ferry` | 位置 `x` 与大小 `scale` |
| `swans` | 黑天鹅列表：`x` 位置，`dy` 离水面线的像素距离，`s` 大小，`dir` 朝向（1 右 / -1 左） |
| `shimmer.count` | 水面波光条数 |

### 3.4 Rockingham 专用

| 参数 | 作用 |
|---|---|
| `far.gardenIsland` / `pointPeron` | 横向范围 `x0/x1` 和高度 `height`(px) |
| `far.causeway` | Garden Island 堤道的范围 |
| `far.penguinIsland` | 中心 `x`、宽 `w`（比例）、高度 |
| `far.rocks` | 小礁石 `[x, 宽, 高px]` |
| `far.farCoast` | 远处海湾东岸（很淡） |
| `far.ships` / `far.sails` | 远洋货轮 / 远处帆船的位置和大小 |
| `mid.jetty` | 木栈桥：`x0/x1` 范围，`deckY` 桥面高度（比例） |
| `mid.boats` | 停泊的船：`type` = `yacht` / `motor`，`sail: true` 表示升帆 |
| `near.waterline` / `grassY` | 沙滩上的水线 / 草坪起点（比例） |
| `near.pines` | 诺福克松：`count` 棵数，`h` 高度范围(px)，`keepOut` 不种松树的区域 `[x, 半宽]`（保证 Penguin Island、栈桥头能被看到） |
| `near.trees` | 胡椒薄荷树数量与高度 |
| `near.buildings` | 海边小楼：`x`、`w/h`(px)、`floors` 层数、`roof` = `flat` / `pitched` |
| `near.shelters` / `near.gulls` | 野餐亭 / 海鸥位置 |

### 3.5 想改形状？

地标都是独立函数，坐标单位是**米**、原点在建筑底部中心、y 向上，比较好改：
`lmCentralPark`、`lmBrookfield`、`lmQV1`、`lmPerthArena`、`lmOptus`、`lmMatagarup`（中景），以及 `buildPerthNear()` 里的 Spanda / EQ 桥 / Bell Tower 段落，`buildPerthFar()` 里的纪念碑 / DNA 塔段落。
通用楼顶样式见 `towerGeom()`，样式权重表 `TOWER_STYLES`（例如想要更多天线就调高 `mast`）。

---

## 4. 无缝平铺：原理与自检

- 连续的东西（远山、山脊轮廓、水面）用**整数周期**的正弦叠加生成，在 x=0 和 x=宽 处严格相等、连斜率都连续。
- 离散的东西（楼、树、波光……）如果跨过左右边缘，会自动在另一侧再画一份（`Layer.body()`）。
- 竖直渐变只沿 y 变化，天然无缝。

`node generator.mjs --verify` 会把整幅内容**平移半个画布重新生成**，再和原图“循环移位”后的结果逐像素比较。若有物体漏画了跨边缘的副本，会出现成片的大差异并报 `FAIL`（进程返回码 2）。
报告里“抗锯齿舍入差像素”是同一形状放在不同绝对坐标时边缘抗锯齿的浮点误差（几十个像素、1~20 级），肉眼不可见，属正常。

肉眼检查：看 `out/<场景>_composite_tiled-check.png` 的正中间，或在 `preview.html` 里勾选“显示接缝线”看彩色竖线经过时有没有断痕。

---

## 5. 在 Unity（URP）里使用

### 5.1 导入设置（每张 PNG）

| 设置 | 远 / 中 / 近景层 | 天空 |
|---|---|---|
| Texture Type | **Default**（贴到 Quad / 圆柱的材质上）。若用 SpriteRenderer 则选 Sprite (2D and UI)，Mesh Type = Full Rect | Default |
| sRGB (Color Texture) | ✔ | ✔ |
| Alpha Source | Input Texture Alpha | None |
| **Alpha Is Transparency** | ✔（我们的 PNG 已做过透明边缘颜色填充，开着更保险） | — |
| Non-Power of 2 | None（尺寸本来就是 2 的幂） | None |
| **Wrap Mode** | **Per-axis：U = Repeat，V = Clamp**（V 不能用 Repeat，否则双线性过滤会把底部的水色渗到顶边，出现一条细线） | Clamp |
| Filter Mode | Bilinear | Bilinear |
| Generate Mip Maps | 面片在屏幕上约等于或大于贴图分辨率时**关掉**（省 33% 内存、更锐利）；如果会被缩得很小（例如很远的圆柱）再打开 | 关 |
| Read/Write | 关 | 关 |
| **Max Size（Web 平台覆盖）** | **2048**（直接导入 `-2k` 文件即可） | **256**（纯竖直渐变，256×64 双线性放大依然平滑；或者干脆用渐变着色器 / 相机纯色背景） |
| Compression（Web） | 桌面浏览器：RGBA Compressed **DXT5/BC3**（每张 2048×512 ≈ 1 MB）；移动浏览器：**ASTC 6×6**（≈ 0.46 MB）；可勾 Use Crunch Compression 减小下载体积。压缩后检查剪影边缘有没有脏块，不满意就换 High Quality / BC7（需目标浏览器支持） | **None**（渐变压缩容易出色带，反正只有 256×64） |

内存参考（2048×512，不含 Mip）：RGBA32 = 4 MB，DXT5/BC3 = 1 MB，ETC2 RGBA8 = 1 MB，ASTC 6×6 ≈ 0.46 MB。三层 + 天空在 DXT5 下约 3 MB。

### 5.2 摆放方式 A（推荐）：独立背景相机（URP Camera Stacking）+ UV 滚动

背景用一台单独的相机画，游戏相机叠在上面。这样天际线永远在道路和障碍物后面、不会被跑近、不受远裁剪面限制，也不会被场景雾冲淡。

1. 新建一个 Layer，叫 `Skyline`。
2. 新建相机 `SkylineCamera`：Render Type = **Base**，Projection = **Orthographic**（尺寸好算），Environment → Background Type = **Solid Color**（颜色用下面表里的“天空顶部”），Culling Mask **只勾 `Skyline`**，Post Processing 关。
3. 游戏主相机：Render Type 改为 **Overlay**，Culling Mask **去掉 `Skyline`**，然后把它加到 `SkylineCamera` 的 Stack 列表里（Overlay 相机默认会清深度、保留颜色）。
4. 在 `SkylineCamera` 前方放 4 个 Quad（Layer = `Skyline`）：`Sky` z=10.3、`Far` z=10.2、`Mid` z=10.1、`Near` z=10.0（离得近 → 雾的 Start 设在 10 以外就完全不受雾影响；z 的微小差别让透明物体按远近正确排序）。
   每个 Quad 保持 **宽:高 = 4:1**（一张贴图一个周期）；若材质 Tiling X = N，则 Quad 宽:高 = 4N:1。宽度至少盖满正交相机的视野宽度（= 2 × Size × 屏幕宽高比）。
5. 竖直方向：把贴图的地平线（Perth 在从上往下 **84.5%** 处，Rockingham **70.5%**）对齐到游戏画面里的地平线（路面消失线）。
6. 材质：**Universal Render Pipeline/Unlit**，Surface Type = Transparent，Blending Mode = Alpha，Render Face = Front。Sky 可以用 Opaque。
7. 每层挂一个滚动脚本，按视差系数滚动 UV（Sky 不用滚）：

```csharp
using UnityEngine;

// 挂在每一层天际线 Quad 上。贴图 U 方向必须是 Repeat。
public class SkylineScroller : MonoBehaviour
{
    // 游戏逻辑每帧写入当前速度（0 = 停下），所有层共用
    public static float WorldSpeed = 1f;

    [Tooltip("相对视差：Far 0.15 / Mid 0.4 / Near 1（Sky 0，不需要挂）")]
    [Range(0f, 1.5f)] public float parallax = 0.4f;

    [Tooltip("视差 = 1 的层，在 WorldSpeed = 1 时每秒滚过多少张贴图宽度")]
    public float tilesPerSecond = 0.02f;

    Material _mat;
    float _u;

    void Awake() => _mat = GetComponent<Renderer>().material; // 每层一个材质实例

    void Update()
    {
        _u = Mathf.Repeat(_u + WorldSpeed * tilesPerSecond * parallax * Time.deltaTime, 1f);
        _mat.mainTextureOffset = new Vector2(_u, 0f); // URP Unlit 的 _BaseMap 标了 [MainTexture]，可直接用
    }
}
```

> 想让城市朝反方向流动就把 `tilesPerSecond` 设为负数。学生做 micro:bit 控制速度时，只要改 `SkylineScroller.WorldSpeed` 即可。
> Camera Stacking 在 Web 平台可用；多一台相机的开销很小（只画 4 个 Quad）。

### 5.3 摆放方式 B（最简单）：单相机 + 远处的大面片

- 4 个 Quad 直接放在场景远处（例如 z = 300 / 320 / 340 / 360，Near 最近、Sky 最远），作为相机的子物体或每帧跟随相机的 x/z，避免被跑近；相机 Far Clip 要大于最远的面片。
- 材质同上（URP Unlit，Transparent，Alpha）。透明物体按距离从远到近排序，距离拉开就不会乱序；若仍有闪烁，用材质 Advanced Options 里的 **Sorting Priority** 固定顺序。
- **雾**：URP Unlit 会受场景雾影响。线性雾在 End 之外是 100% 雾色，远处的面片会被整片冲白——要么关掉雾，要么把雾的 End 设到面片之外并接受一点额外的雾（雾色用下表的地平线色，效果还算自然）。
- 视差同样用 5.2 的脚本滚 UV。

### 5.4 摆放方式 C：内表面圆柱（适合相机会左右转的玩法）

- 用法线朝内的圆柱（或在 DCC 里翻转法线），U 沿圆周展开，三层用三个不同半径（near < mid < far），天空最外层。
- 一圈放几个周期由材质 Tiling X 决定：1 = 一圈只有一个珀斯（地标只出现一次，但贴图被拉得很宽）；**2 更推荐**（每张贴图覆盖 180°）。
- 为了不变形，每个周期的弧长 : 圆柱高度 = 4 : 1，即 高度 = 2πr / (4 × TilingX)。
- 视差来自相机转动 + 半径差，也可以叠加 5.2 的 UV 滚动。雾的注意事项同方式 B；也可以把圆柱放到方式 A 的背景相机里。

### 5.5 视差系数建议

| 层 | 相对视差（近景 = 1） | 说明 |
|---|---|---|
| Sky | 0 | 不动（或 0.02 的极慢漂移） |
| Far | **0.15** | 远山、Kings Park、远处楼群 |
| Mid | **0.4** | CBD 楼群与地标 |
| Near | **1.0** | 岸线、Bell Tower、EQ、黑天鹅 |

近景本身建议只有道路速度的约 3%~8%（屏幕空间），整体要“慢”，否则远景会抢戏、也容易让人晕。`preview.html` 里的默认值就是这一组，可以拖滑条直观比较。

### 5.6 雾与颜色协调

贴图里已经画好了**大气透视**（越远越淡越偏蓝，各层底部还有地面雾），所以：

- 天际线最好不要再吃场景雾（方式 A 天然做到），避免“雾上加雾”把远景冲白。
- 场景雾（Lighting → Environment → Fog，建议 Linear）的颜色用**地平线颜色**，这样路面远端的雾会和天际线底部自然融合：

| 场景 | 雾颜色 / 地平线 | 天空顶部（相机背景色） | 大气透视色 |
|---|---|---|---|
| Perth | `#f4e3c5` | `#77b2dc` | `#d0e0e8` |
| Rockingham | `#ebecdc` | `#6aacdd` | `#d4e4ec` |

（改了调色板后以 `out/skyline-meta.js` 或 `preview.html` 里显示的数值为准。）

- 前景（道路、障碍物、角色）的饱和度和对比度要**高于近景层**，背景才会“退后”。近景层最暗的颜色约 `#1d3343`（Perth 岸线），前景描边/阴影可以比它更暗。
- 道具或 UI 的点缀色可以借用 Bell Tower 的铜橙色（`#e0945b` / `#a2552d`），和背景呼应。
- 后处理：贴图是按显示用 sRGB 画的。若开了 Tonemapping，建议用 **Neutral** 或关掉（ACES 会让暖色偏暗、偏饱和）。Bloom 阈值别太低，否则白色的 Spanda / 桥会发光。

---

## 6. 地标说明（风格化，非测绘精度）

为了构图，真实距离做了压缩，建筑高度相对水平距离做了夸张（这是天际线插画的常规做法）。**没有任何文字、标志或商标。**

| 地标 | 做法 | 把握 |
|---|---|---|
| Central Park | 全景最高；细长塔身 + 三级阶梯楼冠 + 天线；暗面有 X 形外露钢结构 | 高 |
| Bell Tower（天鹅钟楼） | 细高玻璃尖塔（82.5 m）+ 两侧铜色帆翼，帆翼约到塔高 58%，越往上越向外张开（Ψ 形）；尖塔加了细描边，视差滑到亮色楼面前也能看清 | 中高（帆翼的实际弧度请对照照片） |
| Elizabeth Quay 步行桥 | 白色微拱桥面 + 两道交错的白拱 + 吊索 | 中 |
| Spanda | 6 个白色细长椭圆环，底部共点、逐级放大并略微扇开 | 中 |
| Matagarup Bridge | 两道向中间“探头”的不对称高拱（像两只相对的天鹅脖子）+ 拉索 | 中 |
| Optus Stadium | 超椭圆碗形、古铜色竖向鳍片的节奏、白色屋檐 + 檐下阴影线 | 中高 |
| Perth Arena | 锯齿屋顶的多面体，几种蓝灰明暗的三角面 | 中 |
| Kings Park | 台地山脊 + 树冠 + 朝城市一侧的浅色崖面；战争纪念碑方尖碑在崖边，DNA 塔在西侧 | 中高 |
| Brookfield Place | 宽厚玻璃塔 + 一侧斜切楼顶 + 竖向鳍片 + 矮副楼 | **低**（细节凭印象，属“合理的风格化”） |
| QV1 | 一侧圆肩楼顶 + 稍高的核心筒 + 密集水平遮阳 | **低**（同上） |
| Rockingham：诺福克松 / Point Peron / Penguin Island / 栈桥 | 松树为分层“平台 + 流苏”；岛屿为低矮轮廓 + 浅色石灰岩崖面；栈桥为通用木栈桥 + T 形平台 | 松树高；岛形与栈桥中低 |

---

## 7. 需要技术美术确认 / 手动处理的地方

1. **Brookfield Place、QV1 的楼冠**：对照实景照片修 `lmBrookfield()` / `lmQV1()`（单位是米，很好改），或直接在 SVG 里手修。
2. **Bell Tower 帆翼曲线**、**EQ 桥拱的数量与走向**、**Matagarup 拱形**：剪影已经可读，但细节是风格化近似，建议对照照片微调。
3. **地平线对齐**：贴图里地平线在 84.5%（Perth）/ 70.5%（Rockingham），需要和游戏相机的地平线对齐，否则会感觉城市“浮着”或“沉下去”。
4. **黑天鹅在 2k 下只有约 23 像素长**，若游戏里看不清，可把 `PERTH.near.swans[].s` 调到 1.4~1.6，或去掉。
5. **压缩检查**：DXT/ASTC 压缩后看一下细线（吊索、窗带、X 形钢结构）有没有闪烁或脏块；若有，可调低 `windows` 相关不透明度（`winOpacity`）或在 Unity 打开 Mip Maps。
6. **天空**目前是纯渐变；如果想要云，建议单独做一层慢速云（不要画进 sky，sky 最好保持可以缩到 256 的纯渐变）。
7. Rockingham 是扩展目标的第一版：栈桥是通用造型；没有画 Kwinana 工业区和 Garden Island 上的海军基地；松树高度做了夸张。
8. 换种子后请用 `--verify` 和 `preview.html` 复查一遍（地标有保护区，但通用楼群的节奏会变）。
