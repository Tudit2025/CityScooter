# 外观、天际线和特效：使用说明

给开发组和技术美术。渲染设置见 `urp-web-settings.md`，性能预算和出包检查清单见 `perf-budget.md`。

## 放进游戏场景（Main.unity）只需要三步

1. 拖入 `Assets/_Project/Prefabs/Environment/Lighting_PerthAfternoon.prefab`，删掉场景里原来的 Directional Light。
2. 拖入 `Assets/_Project/Prefabs/Environment/SkylineRig.prefab`（放在哪里都行，运行时会自己跟着相机）。相机 Far Clip ≥ 650 m。
3. 任意物体上加 `QualityAutoTuner` 组件（Web 慢机器保底）。

特效 prefab 在 `Assets/_Project/Prefabs/VFX/`，用法见下文。

## 文件在哪里

| 路径 | 内容 |
|---|---|
| `Scripts/Runtime/Rendering/` | 运行时代码，命名空间 `CityScooter.Rendering`，在 `CityScooter.Runtime` 程序集里 |
| `Scripts/Editor/LookDev/` | 编辑器工具（单独的 `CityScooter.Editor.LookDev` 程序集）：生成资源、渲染预览、检查 Web 设置 |
| `Art/Shaders/` | `SkyGradient.shader`（天空盒）、`SkylineLayer.shader`（天际线层） |
| `Art/Materials/` | `Sky_PerthAfternoon.mat`、`Sky_RockinghamBeach.mat`、`Skyline_Layer.mat`；`VFX/` 下是特效材质 |
| `Art/Looks/` | `Look_PerthAfternoon.asset`、`Look_RockinghamBeach.asset`（SceneLook） |
| `Art/Textures/Skyline/` | 天际线美术图（从 `art-src/skyline/out` 复制的 `-2k` 文件） |
| `Art/Textures/Generated/` | 代码生成的特效小图（软圆点、星星、闪光、圆形阴影） |
| `Art/LookDev/` | 只给 LookDev 场景用的占位材质和交通锥网格，**不要在游戏里引用** |
| `Scenes/LookDev.unity` | 看外观的测试场景（不在 Build Settings 里） |

## 外观（SceneLook）

一个 SceneLook 资源 = 一个地点 + 一种氛围：天空材质、环境光渐变（天 / 地平线 / 地面三色）、线性雾、太阳（颜色、强度、角度、阴影浓度）、天际线三层贴图和贴图里的地平线位置。

- `ApplySceneLook`（在 Lighting prefab 上）把它应用到场景，编辑模式下也生效，改资源时场景视图实时更新。
- **换地点只改一个字段**：`ApplySceneLook.look` 换成 `Look_RockinghamBeach`，天空、雾、太阳、天际线一起换。
- 雾的颜色就是天际线美术的地平线颜色（Perth `#F4E3C5`，Rockingham `#EBECDC`），远处路面会融进天际线底部。
- 太阳角度：x = 向下的角度，y = 朝向（0 = 光沿 +Z 也就是骑行方向照）。现在是从左后上方照过来，障碍物朝向玩家的一面是亮的。
- Web 没有 HDR：太阳强度保持在 1.2～1.5，否则白色表面会过曝发平。

## 天际线（SkylineRig + SkylineParallax）

- 三层贴在以相机为中心的圆弧上（远 600 m / 中 500 m / 近 400 m），不额外加相机，不受雾影响，不投影。
- 三层用同一套竖直映射，和美术合成图完全一样叠在一起。贴图里的地平线（Perth 从上往下 84.5% 处的水岸线，Rockingham 70.5% 处的海平线）放在离视平线 `Horizon Angle`（默认 -0.4°）的位置，也就是道路消失在雾里的地方。
- `Degrees Per Repeat` = 90：一张贴图宽度占 90°，16:9 画面里正好约一屏宽，和合成图的构图一致。数值越小天际线越大。
- `Center U` = 0.47：开局时正前方对着 Perth CBD。
- 滚动：每层按 `走过的米数 × Scroll Per Metre × Parallax` 横向滚动。Parallax 用美术建议值：远 0.15 / 中 0.4 / 近 1。默认主速率 0.0015：15 m/s 时近景每秒约滚 0.02 张贴图宽度，整体很慢，不抢戏。想反方向就把主速率设成负数。
- 行驶距离默认从相机位移自动算（一帧跳超过 25 m 视为传送，不算）。如果游戏是"世界往后移、相机不动"，把 `Distance Source` 改成 Manual，每帧调用 `AddDistance(米数)`。
- 天空盒的渐变条用同样的角度映射（`SkylineParallax` 会发一个全局参数给天空 shader），天空颜色和天际线始终对齐。
- 美术图已经画好了大气透视和底部地面雾，所以各层的 Haze / Bottom Haze 默认是 0。只有某一层太抢眼时才调高一点。

### 换上新的天际线美术

- **美术更新了同名文件**：菜单 **CityScooter → Look Dev → Import Skyline Art**，会把 `art-src/skyline/out/` 下 `perth_*` / `rockingham_*` 的 `-2k` PNG 复制到 `Art/Textures/Skyline/` 覆盖旧文件。GUID 和导入设置不变，所有引用自动更新。
- **新地点或新文件**：把 PNG 放进 `Art/Textures/Skyline/`（导入设置会自动设好：U Repeat / V Clamp、Alpha Is Transparency、Mip Maps、最大 2048、DXT5；文件名带 `_sky` 的按天空渐变条处理：256、不压缩、无 Mip），然后在 SceneLook 的 Skyline 区块拖入三张贴图，填好地平线比例。不想走 SceneLook 时，也可以直接拖到 SkylineRig 每层的 `Texture`（只在 SceneLook 没给贴图时生效）。
- 新的天空渐变条拖到天空材质的 `Sky Strip`，勾 `Use Sky Strip`，填 `Strip Horizon`。
- 换完看 LookDev 场景或预览图：天际线有没有"浮着"或"沉下去"（调 `Horizon Angle`），剪影边缘压缩后有没有脏边或黑边。
- 占位剪影生成器还在：菜单 **Generate Placeholder Skyline Textures** 会写到 `Art/Textures/Generated/`。占位图没有大气透视，用的话要把那几层 Haze 调到 0.3～0.55。

## 特效

| Prefab | 用法 |
|---|---|
| `VFX_CrashPuff` | 撞车时在骑手位置 `Instantiate`：尘土 14 个 + 卡通星星 5 个，约 1 秒后自动销毁 |
| `VFX_GiftSparkle` | 捡到礼物时在礼物位置 `Instantiate`：金色闪光 16 个 + 1 个光晕，约 0.6 秒后自动销毁。可直接填到 `Gift.pickupEffect` |
| `VFX_SpeedLines` | 场景里放一个即可，自动贴在主相机前方。默认听 `GameEvents.SpeedLevelChanged`（0～9）调发射量，不用接线；也可以调用 `SpeedLines.SetSpeed01(0..1)` |
| `BlobShadow` | 放在骑手下面当子物体（目标默认是父物体）。默认只在真实阴影关闭时显示（自动降档后），跳起来会变小变淡 |

- 一次性特效根节点上的 `VfxDensityScaler` 会按 `QualityAutoTuner.VfxDensity` 缩减粒子数量。新做的特效 prefab 也挂上它。
- 自己写的特效脚本读 `QualityAutoTuner.VfxDensity`（0～1）即可。
- 特效 prefab 由代码生成：**CityScooter → Look Dev → Rebuild VFX Prefabs (overwrite)** 会按代码重建，手动调过的参数会丢。想手调就别再点这个菜单，或者把改动同步回 `LookDevPrefabs.cs`。

## 自动降档（QualityAutoTuner）

见 `urp-web-settings.md` 最后一节。常用接口：

- `QualityAutoTuner.CurrentTier`、`QualityAutoTuner.VfxDensity`、事件 `QualityAutoTuner.TierChanged`
- `Wait For Run Started`：勾上后从 `GameEvents.RunStarted` 开始测（菜单比游戏轻，测菜单会偏乐观）
- 组件上的 `Force Tier`：编辑器里预览 Reduced / Low 的效果

## LookDev 场景

`Scenes/LookDev.unity`：一条直的珀斯街道（全是基础几何体）、车道里的障碍物和礼物、滑板车上的胶囊骑手、游戏相机（后 6 m、高 3.5 m、看前方 20 m）、Lighting prefab、天际线、特效。

- 播放后整组骑手和相机会沿 +Z 前进并循环（`LookDevAutoRide`），可以看天际线滚动、雾里出现的物体和速度线；撞车和礼物特效每隔几秒自动播放（`VfxPreviewSpawner`）。
- 想看自动降档：启用场景里的 `QualityAutoTuner (enable to test tiers)`，勾 Force Tier。
- **CityScooter → Look Dev → Render Preview PNGs**：用 Web 画质（Mobile 等级）渲染 8 张图到 `Temp/LookDevPreviews`：游戏视角、高视角、天际线特写、太阳方向、降档效果、PC 画质、Rockingham 游戏视角和特写。

## 重新生成

| 菜单（CityScooter → Look Dev） | 作用 |
|---|---|
| Build Missing Assets | 只生成还不存在的资源，不动已有的（手调过的不会丢） |
| Rebuild VFX Prefabs (overwrite) | 只重建特效贴图和 prefab |
| Rebuild Everything (overwrites tuning) | 全部按代码重建，包括材质、SceneLook、prefab、LookDev 场景 |
| Import Skyline Art | 从 `art-src/skyline/out` 复制天际线 PNG |
| Check / Apply Web URP Settings | 见 `urp-web-settings.md` |

批处理示例（不要加 `-nographics` 渲染预览，因为它需要显卡）：

```
Unity.exe -batchmode -projectPath <项目> -executeMethod CityScooter.EditorTools.LookDev.LookDevBuilder.BuildBatch [-overwrite] [-skylineSrc <art-src/skyline/out>]
Unity.exe -batchmode -projectPath <项目> -executeMethod CityScooter.EditorTools.LookDev.LookDevPreview.RenderBatch -lookdevOut <文件夹>
Unity.exe -batchmode -nographics -projectPath <项目> -executeMethod CityScooter.EditorTools.LookDev.WebRenderSettings.CheckBatch
```
