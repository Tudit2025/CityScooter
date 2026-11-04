# Web 性能预算（技术美术 / 关卡美术用）

目标机器：学校机房 PC，Intel UHD 620 一级的集成显卡，Chrome / Edge，WebGL2，浏览器窗口约 1920×1080。
目标：**稳定 60 fps**（每帧 16.6 ms）。WebGL 只有一个线程，脚本、物理、渲染提交都在主线程上，所以 CPU 和 GPU 两边都要省。
保底：`QualityAutoTuner` 在帧率不够时自动降档（见 `urp-web-settings.md`）。它是安全网，不是预算的一部分：**按 Full 档做到预算以内。**

## 预算表（游戏最忙的时刻：撞车 + 吃礼物 + 速度线同时出现）

| 项目 | 目标 | 上限 | 说明 |
|---|---|---|---|
| 每帧 Batches（Stats 窗口） | ≤ 120 | 200 | 包含阴影 pass。SRP Batcher 让同一 shader 的 draw call 很便宜，但 shader / 关键字组合越多，SetPass 越多 |
| SetPass Calls | ≤ 30 | 50 | 同一种 shader + 同一组关键字算一种。尽量共用材质（Kenney 的调色板贴图：大部分道具一个材质） |
| 屏幕上的三角形 | ≤ 150k | 300k | 含阴影 pass（投影物体会再画一遍） |
| 单个道路块（RoadChunk，40 m） | ≤ 15k 三角形，≤ 15 个渲染器 | 25k | 两侧建筑合并成少量网格；看不到的背面和底面删掉 |
| 单个障碍物 / 礼物 | ≤ 1.5k 三角形，1 个材质 | 3k | |
| 角色 + 滑板车 | ≤ 5k 三角形，≤ 2 个材质 | 8k | |
| 实时光源 | 1 盏平行光（太阳） | 1 | 附加光源是逐顶点的，每物体最多 2 盏；能用自发光 / 发光贴片假装就别用灯 |
| 阴影 | 1 级级联，1024，40 m，硬阴影 | — | 投影物体只要：角色、障碍物、礼物、路边建筑。路面、车道线、小装饰、远处建筑一律关掉 **Cast Shadows** |
| 材质种类（游戏场景） | ≤ 25 | 40 | 只用 URP **Simple Lit**（关高光）或 **Lit**（Smoothness ≤ 0.3）；不用 Complex Lit、Baked Lit 以外的特殊 shader |
| 透明材质 | 只给 VFX、天际线、UI | — | 不要做大面积透明玻璃、半透明树叶；树叶用不透明低模 |
| 粒子（同时存活） | ≤ 80 | 150 | 单个特效 ≤ 30。现有：撞车 14 + 5 星星，礼物 16 + 1 闪光，速度线最多 60（满速时约 25 个存活） |
| 粒子 shader | Particles/Unlit | — | 不用 Lit 粒子；不开 Soft Particles（要深度图）、不开 Distortion（要颜色图） |
| 透明叠加（overdraw） | 天际线 3 层 ≈ 0.5 屏 | 1.5 屏 | 天际线 shader 很便宜，主要被近处不透明物体挡住。避免全屏半透明面片和大片半透明 UI |
| 后处理 | 无 | — | 相机 Post Processing 关掉（见 `urp-web-settings.md`） |
| 贴图尺寸 | 道具 ≤ 512，建筑 ≤ 1024 | 2048 | 天际线：每层 2048×512（DXT5 约 1 MB + mip）；天空渐变条 256×64 不压缩 |
| 贴图压缩 | RGB 用 DXT1/BC1，带 alpha 用 DXT5/BC3 | — | 桌面浏览器都支持 DXT。只有极小的贴图（≤ 64）或渐变条才不压缩。需要减小下载体积时可开 Crunch，开完检查细线（拉索、窗带）有没有脏块 |
| 贴图显存 | ≤ 64 MB | 128 MB | Profiler → Memory 里看 Texture2D |
| 网格 / 贴图的 Read/Write | 关 | — | 开了内存翻倍 |
| 构建下载体积（Brotli） | ≤ 25 MB | 40 MB | 看构建日志里 `[CityScooter] Web build ... MB` |
| Shader 变体 | 只用上面列的 shader | — | 每多一种 shader 或关键字，首次出现时都会卡一下（WebGL 现场编译）。新 shader 先找技术美术 |

帧时间分配（参考）：GPU ≤ 10 ms（1080p，Full 档）；主线程脚本 + 物理 ≤ 4 ms；渲染提交 ≤ 3 ms。

## 现有资源的开销参考

| 资源 | 开销 |
|---|---|
| 天空 `Sky_*.mat`（CityScooter/Sky Gradient） | 只画在没有物体的天空像素上；每像素 1 次纹理采样 + 少量数学 |
| 天际线 `SkylineRig.prefab` | 3 个 draw call（每层 66 个三角形），透明混合，不受雾、不投影、不接收阴影 |
| `Lighting_PerthAfternoon.prefab` | 1 盏平行光 + 阴影；`ApplySceneLook` 只在启用和换外观时运行 |
| `VFX_CrashPuff` / `VFX_GiftSparkle` | 一次性爆发，< 1 秒，播完自动销毁 |
| `VFX_SpeedLines` | 1 个粒子系统，发射率随速度变化，最多 60 个粒子 |
| `BlobShadow.prefab` | 1 个四边形，乘法混合；默认只在真实阴影关闭时显示 |

## 每次出 Web 包前的检查清单

1. 菜单 **CityScooter → Look Dev → Check Web URP Settings**，Console 显示 `OK`（0 个问题）。
2. `Main.unity` 的游戏相机：Post Processing 关，Anti-aliasing = None，Far Clip ≥ 650 m。
3. 场景里只有 1 盏平行光（`Lighting_PerthAfternoon` 里的 Sun），没有出现 `[CityScooter] ... directional lights are enabled` 警告。
4. Quality 切到 **Mobile**，进入播放，在最忙的时刻（撞车 + 礼物 + 满速速度线）看 Stats 窗口：Batches、SetPass、三角形都在上表的"目标"以内。
5. 新加的贴图：已压缩（不是 RGBA32），尺寸不超过上表；天际线贴图 Wrap = U Repeat / V Clamp。
6. 新加的材质：只用了 Simple Lit / Lit / Unlit / Particles Unlit；透明材质只出现在特效、天际线和 UI 上；没有勾 Soft Particles / Distortion。
7. 新加的模型：Read/Write 关；不需要投影的物体已关掉 Cast Shadows；没有 LOD Group（Web 关了 LOD 淡入）。
8. 新加的特效 prefab：根节点挂了 `VfxDensityScaler`；粒子数在预算内；Max Particle Size ≤ 0.6。
9. 雾和生成距离：道路块在雾结束距离附近生成（Perth 外观 190 m），远处没有东西"突然出现"。
10. 外观：打开 `Scenes/LookDev.unity` 或运行 **Render Preview PNGs**，看障碍物在天际线、天空前面是否依然醒目，天际线底部和路面远处的雾有没有对齐。
11. 出一个 Development 包，在一台机房电脑上打开：浏览器 Console 里有 `[CityScooter] QualityAutoTuner tier ...`，记录它选了哪一档。如果是 Reduced 或 Low，回到第 4 步找原因。
12. 构建日志里的包大小在预算内。
