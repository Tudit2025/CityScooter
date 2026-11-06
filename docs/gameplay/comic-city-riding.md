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

## 验证

Unity 6000.3.28f1：**25/25 PlayMode、39/39 EditMode 通过**。

新增验证覆盖：两侧人行道和路缘高度、横向极限时的相机位置/转角/角色画面范围、跳跃落地、重开复位；用缩短的测试时钟验证实际音乐和限速的联动、刹车独立性、暂停以及十分钟后的继续增长；220 m/s 侧道碰撞；反复切换街边布局后碰撞对应和对象池上限。数值测试核对上表五个时间节点。

测试代码位于 `RideProgressionTests.cs` 和 `SpeedCurveTests.cs`；结果位于 `Logs/comic-city-PlayMode-results.xml`、`Logs/comic-city-EditMode-results.xml`。十分钟曲线采用数值与加速时钟测试验证，未声称完成十分钟连续人工驾驶；本轮也未执行 Web 构建与目标设备性能测试。
