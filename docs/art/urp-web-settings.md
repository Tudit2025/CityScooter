# URP Web 渲染设置（技术美术用）

目标：机房电脑（集成显卡）上的 Chrome / Edge，WebGL2，窗口约 1080p，稳定 60 fps，画面干净明亮。

- Web 构建用质量等级 0 **Mobile** → `Assets/Settings/Mobile_RPAsset` + `Mobile_Renderer`（WebGL 的默认等级）。
- 编辑器和 Windows 用质量等级 1 **PC** → `PC_RPAsset` + `PC_Renderer`。比 Web 高一档，但颜色管线和光照完全一致，编辑器里看到的就是 Web 上的样子，只是更清晰。

下面所有数值都写在 `Assets/_Project/Scripts/Editor/LookDev/WebRenderSettings.cs` 里：

- **CityScooter → Look Dev → Check Web URP Settings**：逐项检查，有偏差就在 Console 里列出来（`[CityScooter] Web URP settings check`）。**每次出 Web 包前跑一次。**
- **CityScooter → Look Dev → Apply Web URP Settings**：把本文档的值写回去。
- 批处理：`-executeMethod CityScooter.EditorTools.LookDev.WebRenderSettings.CheckBatch`（有偏差时退出码 2）。

要改某个值，先改 `WebRenderSettings.cs` 和本文档，再点 Apply，避免两边对不上。

## Mobile_RPAsset（Web）

| 设置 | 之前 | 之后 | 原因 |
|---|---|---|---|
| HDR | 开 | **关** | 没有 Bloom / Tonemapping，HDR 没有用处；LDR 缓冲省一半显存带宽，WebGL2 上的 HDR 格式支持也不统一 |
| MSAA | 关 | **2x** | 见下面「为什么 MSAA 2x」 |
| Render Scale | 0.8 | **1.0** | 0.8 在 1080p 屏幕上会把车道线、天际线的细线糊掉。降分辨率只交给 QualityAutoTuner 在慢机器上做 |
| Upscaling Filter | FSR | **Bilinear** | 只在自动降档（0.85 / 0.7）时用到。FSR 多两次全屏 pass（EASU + RCAS），在本来就慢的集显上得不偿失 |
| 主光源阴影 | 开，1024 | 开，1024（不变） | 阴影能让角色"贴地"，障碍物也更有立体感 |
| 阴影距离 | 50 m | **40 m** | 相机看前方约 20 m，障碍物有意义的范围在 40 m 以内；距离越短，同样 1024 分辨率阴影越清楚 |
| 级联数 | 1 | 1（不变） | 每多一级就多画一遍投影物体 |
| 软阴影 | 关 | 关（不变） | 软阴影要多次采样；硬阴影配卡通画风也合适 |
| 附加光源 | 逐像素，每物体 4 个 | **逐顶点，每物体 2 个** | 白天场景基本不用附加光；隧道灯、礼物光晕之类以后要用时，逐顶点几乎不花钱 |
| 附加光源阴影 | 关 | 关（不变） | |
| Depth Texture / Opaque Texture | 关 / 关 | 关 / 关（不变） | 没有软粒子、扭曲、水面折射，不需要额外复制一遍深度和颜色 |
| SRP Batcher | 开 | 开（不变） | 同一 shader 的物体批量提交，减少 CPU 开销（WebGL 只有一个线程，CPU 很宝贵） |
| Dynamic Batching | 关 | 关（不变） | 有 SRP Batcher 就不需要 |
| Terrain Holes | 开 | **关** | 不用 Terrain，少编译一批 shader 变体 |
| LOD Cross-Fade | 开 | **关** | Kenney 资源不用 LOD；关掉少一组变体，也省去抖动淡入的开销 |
| Mixed Lighting | 开 | **关** | 无尽跑酷全是实时光，不烘焙光照贴图 |
| Light Cookies | 开 | **关** | 不用，少一组变体 |
| Light Layers | 开 | **关** | 不用，少一组变体 |
| 反射探针混合 / Box Projection | 开 / 开 | **关 / 关** | 没有反射探针，环境反射直接用天空 |
| Lens Flare（数据驱动 / 屏幕空间） | 开 / 开 | **关 / 关** | 不用，构建里就不会带这些 shader |
| Adaptive Performance | 开 | **关** | 这个包只在移动端设备上起作用 |
| SH Evaluation（环境光） | Auto | **Per Vertex** | WebGL 上 Auto 通常本来就是逐顶点；写死以后编辑器预览和 Web 结果一致。平面着色的低多边形模型看不出区别 |

### Mobile_Renderer（Web）

| 设置 | 之前 | 之后 | 原因 |
|---|---|---|---|
| Rendering Path | Forward+ | **Forward** | 逐顶点附加光只在 Forward 下生效。Forward+ 每帧要在主线程做光源分簇（WebGL 没有工作线程），还会多一批 `_FORWARD_PLUS` 变体 |
| Renderer Features | 无 | 无（不变） | 每个 Feature 通常就是一次全屏 pass。不要加 SSAO / Decal |
| Depth Priming | 关 | 关（不变） | URP 在 WebGL 上本来就不开 |

### 为什么 MSAA 2x

- 画风是平面着色的低多边形，深色障碍物和白色车道线压在明亮的天空、路面上，锯齿和移动时的"爬行"很显眼，学生一眼就能看到。
- 2x MSAA 在集显上很便宜：多一份 2 倍采样的颜色和深度缓冲，再加一次 resolve。我们场景的重叠绘制很少（不透明物体为主），带宽压力不大。
- FXAA 需要打开相机后处理，这样每帧多一次 Color Grading LUT 和一次全屏后处理 pass，还会把车道线、天际线的细线和 UI 文字弄糊。4x MSAA 的带宽翻倍，不划算。
- 渲染方式：URP 在 WebGL 上画到一张 2x MSAA 的中间贴图，resolve 后再拷到网页画布。
- 如果机器太慢，`QualityAutoTuner` 的 Low 档会把 MSAA 关掉。
- **第一次出 Web 包时要确认一件事**：URP 会把质量等级里的 Anti Aliasing 自动同步成 URP 资源的 MSAA（Mobile = 2x），而 WebGL 画布是在 URP 启动之前按这个值创建的。如果画布也被创建成多重采样，就成了双重 MSAA（多一份全屏缓冲和一次 resolve）。检查方法：在页面的浏览器 Console 里运行
  `document.querySelector("#unity-canvas").getContext("webgl2").getContextAttributes().antialias`。
  结果是 `true` 的话，请网页外壳负责人在 `createUnityInstance` 的 config 里加 `webglContextAttributes: { antialias: false }`，然后再查一次（网页模板不归美术改）。

## PC_RPAsset / PC_Renderer（编辑器 + Windows）

| 设置 | 之前 | 之后 | 原因 |
|---|---|---|---|
| HDR | 开 | **关** | 和 Web 用同一套颜色管线，编辑器里调的颜色就是 Web 上的颜色 |
| MSAA | 关 | **4x** | 比 Web 高一档 |
| Depth / Opaque Texture | 开 / 开（带降采样） | **关 / 关** | 模板默认值，没有东西用到 |
| 阴影距离 | 50 m | **60 m** | 高一档 |
| 级联数 | 4 | **2**（分割 0.3） | 60 m 用 2048 贴图两级就够了，4 级要把投影物体画 4 遍 |
| 阴影贴图 | 2048 | 2048（不变） | |
| 软阴影 | 开（High） | 开（**Medium**） | 高一档，但不过分 |
| 附加光源阴影 | 开 | **关** | 不用 |
| 附加光源 | 逐像素 | 逐像素（不变）；Rendering Path 仍是 Forward+ | 高一档 |
| PC_Renderer 的 SSAO | 启用 | **停用**（保留组件，勾回 Active 就能恢复） | Web 没有 SSAO，编辑器开着会让角落发暗，和 Web 对不上 |

## 项目设置

| 设置 | 之前 | 之后 | 原因 |
|---|---|---|---|
| Graphics → Shader Stripping → Fog Modes | Automatic | **Custom：只保留 Linear** | 雾是 `ApplySceneLook` 在运行时打开的。Automatic 会看场景文件里保存的雾设置，如果 Main.unity 存的是"雾关"，构建时会把线性雾的变体剥掉，Web 上就没有雾了。Exp / Exp2 用不到，一并剥掉 |
| Quality → 等级 PC 的排除平台 | Android, iPhone | **Android, iPhone, WebGL** | WebGL 构建会把所有没被排除的质量等级的 URP 资源都算进去，PC 资源的 Forward+、软阴影、SSAO 变体会进 Web 包（包更大、首帧编译更久）。排除后 Web 只带 Mobile 一套 |
| Quality → Mobile / PC 的 Anti Aliasing | 0 / 0 | 2 / 4 | **URP 自动同步**的，不是手改的：URP 启动时会把质量等级的值改成 URP 资源里的 MSAA。Mobile 的 LOD Cross Fade 也被同步成关 |
| URPProjectSettings 的 Last Material Version | 9 | 10 | URP 17.0.4 的材质升级器打开项目时自动写入 |

没有改动：URP Global Settings（Render Graph 已开启）、Volume Profile（默认的空效果）、`Main.unity`（不归我管，见下一节建议）。

## 给 Main.unity / 游戏相机的建议（未改动，请负责人确认）

1. **相机 Post Processing 关掉**。只要勾着，URP 每帧都会跑 Color Grading LUT 和最终后处理 pass，就算 Volume 里没有任何效果。
2. 相机 Anti-aliasing = None（MSAA 由 URP 资源控制），HDR / Depth / Opaque Texture 用 "Use Pipeline Settings"。
3. 相机 Far Clip ≥ 650 m（天际线最远一层半径 600 m）。Near Clip 保持 0.3。
4. 放入 `Prefabs/Environment/Lighting_PerthAfternoon.prefab` 后，**删掉场景里原来的 Directional Light**。两盏平行光时，多出来的那盏会当作附加光再算一遍；`ApplySceneLook` 发现时会在 Console 警告。
5. 道路块的生成距离放在雾的结束距离附近（Perth 外观：雾 40 → 190 m），新块在雾里出现，不会"凭空蹦出来"。

## 在编辑器里看 Web 画质

- Edit → Project Settings → Quality，点 **Mobile** 那一行，编辑器就用 Web 的 URP 资源（Mobile 只排除了 Standalone 的构建，编辑器里可以切换）。看完点回 PC。
- 或者菜单 **CityScooter → Look Dev → Render Preview PNGs**，会用 Mobile 等级把 LookDev 场景渲染成 PNG（Temp/LookDevPreviews）。

## 运行时自动降档（QualityAutoTuner）

`Assets/_Project/Scripts/Runtime/Rendering/QualityAutoTuner.cs`，挂在游戏场景里任意物体上即可。

| 档位 | 条件（前 2 s 不算，再取 5 s 平均） | 做什么 |
|---|---|---|
| Full | ≥ 45 fps | 不变 |
| Reduced | < 45 fps | 关主光源阴影（阴影距离 = 0），Render Scale 0.85（Bilinear） |
| Low | < 30 fps | 同上，Render Scale 0.7，MSAA 关，VFX 密度 0.5（`QualityAutoTuner.VfxDensity`） |

- 从不改硬盘上的 URP 资源：运行时复制一份当前资源，改副本，再赋给 `QualitySettings.renderPipeline`。退出播放模式或关闭页面时换回原资源。
- 降档后 `BlobShadow` 会自动出现，代替真实阴影。
- 浏览器 Console 里会打印 `[CityScooter] QualityAutoTuner tier ...`，写明平均帧率和做了什么。
- 只降不升。整个会话只决定一次（重新加载场景不会再测）；`Remeasure()` 可以重新测。
- 想在编辑器里预览某个档位：勾选组件上的 Force Tier。
