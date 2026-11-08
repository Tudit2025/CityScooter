# Comic City 镜头、速度进度与人行道骑行

本次设置已保存到 Main 和 ComicCityPreview。主场景使用限角转向镜头，并允许在道路与两侧人行道之间自由滑行。

## 相机

镜头沿街道中心线前进，左右位置固定，角色横移通过平滑偏航跟随。偏航最多 **±22°**，响应系数 **6/s**。俯仰和高度继续参考道路坡面，跳跃和跨路缘不会直接抬动镜头。

调节入口是 `ComicCityRecipe.asset` 的 `cameraDirectionalFollow / cameraYawLimit / cameraRotationResponse`，重建并 Apply Main 后生效。`FollowCamera.followX` 仅供原有平移模式使用。FOV 继续保持原 76° 基础、82° 上限。

[右侧人行道镜头](../../art-src/comic-city/previews/09-sidewalk-right.png) · [左侧人行道镜头](../../art-src/comic-city/previews/10-sidewalk-left.png)

## 速度与音乐

`Tuning_ComicDownhill.asset` 中的 `maxSpeed` 是一局开始时的速度上限，从原 22 提高到 **44 m/s**。实际起步速度仍是 9 m/s，再按原来的加速、距离难度与刹车规则接近当前上限。HUD 现在显示 `Speed 当前速度 / 当前上限 m/s`。

进度按本局正在运行的游戏时间计算，包含刹车时间；`Time.timeScale = 0` 暂停、撞车或 Rider.Stop 时停止累计。重开清零。

加速度上限 `acceleration` 从 3 提高到 **9 m/s²（3 倍）**，同时用于刹车后的速度恢复。实际提速仍受当前目标速度约束：目标由距离难度和当前速度上限共同计算。

| 本局运行时间 | 速度上限 | 音乐播放倍率 |
|---|---:|---:|
| 0 分钟 | 44 m/s | 1.00× |
| 5 分钟 | 132 m/s | 1.25× |
| 10 分钟 | 220 m/s | 1.50× |
| 11 分钟 | 224.4 m/s | 1.50× |
| 20 分钟 | 264 m/s | 1.50× |

前 600 秒线性增长到初始上限的 5 倍；之后按用户选择继续增长，每分钟增加初始上限的 0.1 倍，即 **4.4 m/s 每分钟**，是前十分钟增长率的四分之一。后续上限不封顶。

参数为 `maxSpeed=44`、`maxSpeedMultiplier=5`、`speedRampSeconds=600`、`postRampMultiplierPerMinute=0.1`。旧主题默认倍率 1、后续增长 0，沿用固定上限。

音乐根据速度上限的增长进度平滑加速，刹车不会把它拉回慢速；十分钟后保持最高 1.5×。上限入口是 `CityAudioBank.asset / musicMaxPlaybackRate`，运行时也限制在 1–1.5。重开立即恢复 1× 并从音乐起点播放。

播放倍率使用 Unity AudioSource.pitch，因此节奏和音高一起变化：原曲 160 BPM 到最高约 240 BPM，48 秒源循环在最高倍率下约 32 秒播放一遍。当前没有保音高时间拉伸。滚动、风噪和设备 `spd:0..9` 使用实际速度相对**当前**上限的比例，避免上限增大后一直显示固定最高档。

## 人行道与碰撞

- 道路半宽 4 m，两侧各开放 4 m 人行道；骑手中心限制为 `±(8 - radius)`，当前约 **±7.65 m**，对应门市或前庭边缘。
- 人行道表面比道路高 **0.2 m**。跨路缘使用宽 **0.3 m** 的平滑高度过渡，可直接驶上驶下；腾空跨过路缘不会瞬间被额外抬高 0.2 m。
- `RoadChunk` 新增 `sidewalkWidth / sidewalkHeight / curbTransitionWidth`。默认人行道宽度为 0，兼容旧主题。`HalfWidthAt(z)` 仍返回道路宽度；骑行范围使用 `RideableHalfWidthAt(z)`，角色高度使用 `SurfaceHeightAt(x,z)`；相机与城市卷曲使用原道路高度查询。
- 随机摆件使用原单体的简单碰撞盒，每套街边布局带对应的 `collisionLayouts` 数据。`StreetDressing` 切换合并网格时同步移动、启停并复用碰撞盒。不会因循环路段持续增加对象。
- 高度不超过 1.1 m 的街边摆件标记为可跳过；较高摆件、候车亭和停放车辆不可跳过。碰撞沿用 Obstacle 撞车规则。建筑边界由骑行范围限制。
- 新生成或重新摆放的碰撞会在查询前同步到物理场景；骑手继续使用跨越本帧移动的扫掠体积，覆盖高速运动。

## 跳台、滞空、缺口和特技（v0.3，系统 B）

代码：`RiderController`（地面、飞行、缺口）、`RiderActions`（特技、SPLASH!）、`TrackGenerator` 的 "Ramps and air" 区域（放置）、`FollowCamera`（镜头抬高）、`Ramp` / `RoadGap`（数据契约，加了运行时登记表 `Ramp.Active` / `RoadGap.Active`，启用时自动登记，路段回收时自动退出）。数值都在 `GameTuning` 的 "Ramps and air"。

- **地面** = 路面（含人行道）+ 骑手脚下最高的跳台 `Ramp.HeightAt`（在跳台自己的局部坐标里算，跳台跟着下坡路面倾斜也没问题）。从正面骑上去平滑升高；从侧面撞进去不会被抬上去，也不算撞车。
- **起飞**：过了台边（或者地面突然比骑手掉得快），竖直速度 = 速度 × 坡度（height/rise）× `Ramp.launch` × `rampLaunch`（1.6），最多 `rampMaxLaunchVelocity`（8 m/s）；空中重力 = 重力 × `Ramp.hangGravity` × `rampHangGravity`（0.8）。标准小跳台（rise 5、height 1.4、默认 launch 1、hangGravity 0.5）在 15 m/s 时飞约 1.87 秒、28 m，最高离路面约 4.2 m；9 m/s 时约 1.3 秒、11 m；22 m/s 时约 2.2 秒、47 m。在跳台顶上按跳也会按跳台的滞空重力飘。普通跳（空格 / `jp`）不变。
- **空中**：转向是平时的 60%（`airSteerFactor`）。障碍物：能跳过的照旧；其它障碍只要整帧脚都在它碰撞盒顶上方就飞过去（按 `Collider.bounds`），落在车顶上就是撞车。空中礼物照常拿。
- **镜头**：在跳台上和跳台飞行中，镜头按骑手离路面高度的 55% 抬高、瞄准点抬 50%，抬起 2.5/s、落回 3.5/s 平滑；普通跳不动。落地轻微震一下，SKETCHY! 落地震得重一点。
- **缺口** `RoadGap`：在地上进入缺口（或者落在缺口里）就往下掉，`gapFallSeconds`（0.9 秒）后 `GameController.EndRun("SPLASH!")`，结算标题就是缺口的 `word` 加感叹号；水面缺口加一朵水花。飞过去没事。生成器从不放缺口，只有美术做的路段里才有。
- **特技**：只在"大飞行"里有效（从跳台起飞，或预计飞 0.8 秒以上）。转体 / 后空翻轮流，各 0.6 秒（`trickSeconds`），每个 50 分（`trickPoints`），一次飞行 2 个以上安全落地每个再加 25 分（`trickComboPointsPerTrick`，HUD 显示 COMBO）；落地时还在转：SKETCHY!，不算撞车、这一个不加分、稍微减速。防狂发：3 秒内 6 条以上就头晕 3 秒（`trickSpam*`，和按铃一样的思路）。结算页显示特技数。
- **生成器放置**：第一个跳台在 `rampFirstDistance`（220 m）之后，之后每隔 `rampSpacing`（260～420 m）÷ 场景频率，到期时 `rampChance`（0.6）的概率真的放。放置时机是"整段飞行都已经铺好路"的时候：从助跑到按预计速度算出的落地点再加 `rampLandingMargin`（10 m）。不放在大马路的过渡路段上、不和路段事件（路政、警察拦路等）重叠、离别的跳台和缺口至少 `rampMinGap`（30 m）；跳台上和落地区整条路上的随机障碍会清掉；之后的路段事件也会让开跳台的落地区（程序发的 `do:car` 这类事件也会排到跳台后面）。车道按 `Ramp.lanes`（-1/0/1 → x = -2.6/0/2.6）。空中礼物 `rampAirGifts`（5 个）沿预计轨迹摆，只摆在脚离路面 2.5 m 以上的位置（普通跳只能到 1.1 m）。
- **路段里自带的跳台和缺口**（例如 `CHN_Uwa_PondJump`、`CHN_Matilda_Inlet`）：作为路段的子物体，路段生成或从对象池取出时自动登记，回收时自动退出，不用另外注册。`Ramp` 根节点可以不跟路面一起倾斜（放在路面上、不旋转），高度按骑手所在点的路面往上算；缺口在自己的局部坐标里判断，可以偏在一边（UWA 的池塘只盖住右车道和人行道，左车道照常骑）。测试按 4 个路段的数值复刻过（Matilda 入水口、UWA 池塘、Kings Park 高架步道、Mounts Bay 入水口）：9 m/s 起步速度都能飞过（Matilda 的 6 m 入水口还剩 5 m 左右），Mounts Bay 刹到 4 m/s 会掉进水里（设计如此）。生成器自己的跳台不会放在自带跳台或缺口的路段上（按路段预制体判断），离这些跳台、缺口也至少 30 m。
- **台面往下走（桥拱、坡道下行）**：`Ramp` 只描述往上的坡。台面过了顶点要往下走时，再放一个**转过 180° 的 `Ramp`**（yaw 180，轴心放在下坡的**最低端**，rise = 下坡长度，height = 顶点高度，top 0）。骑手过了上坡的台边照样起飞；速度慢的落在下行台面上并顺着滑下来，速度快的直接飞过去。转过来的 Ramp 没有台边，不会让人起飞。骑手在地面上会跟着平缓下降的台面走，只有从侧面掉下去（每帧下降超过 速度 × 0.7 × dt + 5 cm）才算腾空。例子：`CHN_Quay_Bridge` 的 BridgeHump（z 6 起、rise 10、height 2.2、top 8、台边在 z 24）后面加一个 yaw 180、放在 z 32、rise 8、height 2.2、宽 16 的 Ramp，慢速时就不会卡进下行的桥面里（测试 `BridgeHump_Slow_...` / `BridgeHump_Fast_...`）。
- **场景接口**：`TrackGenerator.RampChoices()`（现在返回 `theme.ramps`）和 `RampFrequency()`（现在返回 1）。合并场景系统时要改成当前场景的 `District.ramps`（空就用 `theme.ramps`）和 `District.rampFrequency`，代码里有 TODO。
- **通用跳台**：`RampKitBuilder`（菜单 CityScooter/Comic City/4 - Build ramps，批处理 `CityScooter.EditorTools.RampKitBuilder.BuildBatch`，在 HazardKitBuilder、BoulevardBuilder 之后跑）生成 `Prefabs/ComicCity/Ramps/RMP_Kicker`（木板加钢架小跳台，宽 3 m）、`RMP_Platform`（宽 3.4 m、高 2 m、7 m 助跑、6 m 平台，launch 0.8）和弹出字 SPLASH!、WHOOPS!、SPIN!、FLIP!、SKETCHY!、LANDED!、水花，写进主题的 `ramps` 和 "Ramps and air" 字段。预览图在 `art-src/ramps/`（`RampAirTests.Screenshots_RampFlight_FromTheChaseCamera`）。

## 验证

Unity 6000.3.28f1：**25/25 PlayMode、39/39 EditMode 通过**。

新增验证覆盖：两侧人行道和路缘高度、横向极限时的相机位置/转角/角色画面范围、跳跃落地、重开复位；用缩短的测试时钟验证实际音乐和限速的联动、刹车独立性、暂停以及十分钟后的继续增长；220 m/s 侧道碰撞；反复切换街边布局后碰撞对应和对象池上限。数值测试核对上表五个时间节点。

测试代码位于 `RideProgressionTests.cs` 和 `SpeedCurveTests.cs`；结果位于 `Logs/comic-city-PlayMode-results.xml`、`Logs/comic-city-EditMode-results.xml`。十分钟曲线采用数值与加速时钟测试验证，未声称完成十分钟连续人工驾驶；本轮也未执行 Web 构建与目标设备性能测试。
