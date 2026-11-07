# Comic City 音频标准 v1

本套音频已接入 `Main` 和 `ComicCityPreview`。方向是节奏清晰的街机城市滑行：城市声音退到背景，轮子、风和金属反馈跟随动作，160 BPM 电子音乐提供持续速度感。

## 文件与入口

- 游戏资源：`Assets/_Project/Audio/ComicCity/`，21 个 WAV，按 `Ambience / SFX / Music` 分组。
- 调音入口：同目录 `CityAudioBank.asset`。场景对象为 `GameSystems/ComicCityAudio`，包含 `CityAudioDirector` 和 `CityAudioControls`。
- 生成配方：`tools/prepare-comic-audio.py`；来源恢复工具：`tools/fetch-comic-audio.py`。
- 源文件、下载来源、校验值及处理记录：`audio-src/comic-city/`；外部原始录音留在 Assets 之外。
- 来源记录仅保存 `sources.json` 和 `originals/*-source.txt` 中的名称、作者、来源/下载链接、许可、日期及 SHA-256；不保存完整网页 HTML 或网站脚本，避免将第三方凭据带入仓库。下载工具按固定字段生成记录，原始 HTML 已加入 Git 忽略规则。
- [30 秒综合试听](../../audio-src/comic-city/previews/CityScooter_Audio_Demo.ogg)与[运行时控制面板截图](../../audio-src/comic-city/previews/Audio_Controls.png)。试听是离线参考混音，实际游戏混音随速度、状态和玩家设置变化。
- [外部作品与署名](../../audio-src/comic-city/CREDITS.md)。六个外部源均为 CC0；Freesound 使用公开的有损 HQ MP3 预览，音乐使用无损 FLAC。滚动声来自滑板录音再处理；城市底噪录于布里斯班。

## 素材清单

| ID | 数量 / 时长 | 用途与触发 |
|---|---|---|
| `AMB_City_Loop` | 1 × 24 s | 立体声远处交通；跑动时略降，留出动作空间 |
| `VEH_Horn_01/02` | 2 × 1.60 / 3.05 s | 左右街区远处喇叭，随机间隔 12–24 s |
| `VEH_Pass_01/02` | 2 × 3.50 / 3.90 s | 遮挡后车辆经过，随机间隔 8–17 s；音源沿街移动 |
| `SCO_Roll_Loop` | 1 × 1.468 s | 接地轮声，速度控制音量和音高，跳起淡出 |
| `SCO_Frame_Loop` | 1 × 3.90 s | 车架细小震动，速度提高时增强 |
| `SCO_Wind_Loop` | 1 × 7.75 s | 风噪，音量随归一化速度平方增强 |
| `SCO_Brake_Loop` | 1 × 2.80 s | 接地且刹车时响；松开或离地淡出 |
| `SCO_Scrape_01/02` | 2 × 1.22 / 1.45 s | 骑手贴近横向行驶边界时刮擦，间隔至少 1.2 s |
| `SCO_Jump` | 1 × 0.27 s | 实际进入腾空时的短促掠风 |
| `SCO_Land_01/02` | 2 × 0.45 / 0.53 s | 从腾空落地；腾空时间调整冲击强度 |
| `SCO_Crash_01/02` | 2 × 1.30 s | 实际碰撞事件，播放优先级最高；音乐短暂压低 |
| `UI_Pickup_01/02` | 2 × 0.48 s | 实际礼物拾取事件 |
| `UI_Start` | 1 × 0.80 s | 开始与重开提示，同时音乐从头开始 |
| `UI_Click` | 1 × 0.07 s | 音量面板开关与静音按钮反馈 |
| `MUS_HotRoadway_160_Loop` | 1 × 48 s | MintoDog《Hot Roadway》160 BPM，4/4 拍、32 小节循环 |

合计 6 个循环和 15 个单次声音。金属与碰撞采用偏夸张的短促层次，支持漫画风的触感。风、车架、刹车、经过车辆、落地和提示音是离线合成；刮擦和喇叭使用录音。声音变体避免紧接着重复同一条，随机序列独立于关卡随机数。车辆声用于街区环境，目前不绑定可见汽车实体。

## 制作与命名标准

- ID 使用稳定的 `AMB_ / VEH_ / SCO_ / UI_ / MUS_` 前缀，循环以 `_Loop` 结尾，变体使用 `_01 / _02`。替换素材保留 ID 与 `.meta`。
- 交付 WAV：48 kHz、16-bit PCM；城市底噪和音乐为双声道，其余为单声道。单声道经过声由 Unity 空间化，近身动作声使用 2D 混音保持反馈清楚。
- 所有文件去直流、处理首尾淡入淡出；峰值不高于 −3 dBFS。按用途设 RMS 目标：音乐 −20、轮声 −23、风 −25、城市 −26、车架 −27 dBFS。瞬态声音以峰值上限优先，不为了达到 RMS 强行压扁。此处 RMS 不等于 LUFS。
- 普通循环用交叉淡化接缝与 2 ms 边界桥接；音乐保持完整 48 s，避免交叉淡化缩短小节。源 PCM 循环首尾采样差小于 0.00015。这个检查保证波形边界接近，不能代替对节奏、压缩解码接缝的听感验收。
- 小幅随机音高仅用于单次效果。轮声、车架、刹车、风按实际速度相对当前上限平滑变化；音乐按速度上限增长进度，从 1× 升至十分钟时的 1.5×，以后封顶。使用 AudioSource.pitch，节奏与音高同步变化，约为 160→240 BPM；重开恢复 1×。详见 [骑行进度标准](../gameplay/comic-city-riding.md)。
- 每条素材的精确时长、来源 ID、声道、音量、优先级、冷却、峰值、RMS 和 SHA-256 存在 `catalog.json`。测量表为 `audio-src/comic-city/measurements.csv`。
- 当前 30 秒参考混音测得约 −24.1 LUFS、−7.63 dBTP，预留了较保守的动态空间；这不是所有游戏状态的固定响度。

## Unity 接入与调音

默认分组音量为总音量 0.75、音乐 0.45、城市 0.50、效果 0.85。每条声音还有独立 `volume`。优先级数字越小越重要；碰撞为 10，喇叭为 170，经过声为 190。

运行时固定创建 6 个循环 AudioSource 和 8 个单次 AudioSource。单次池满时允许高优先级替换较低优先级；不会随重复碰撞或拾取无限增加音源。总源增益上限默认为 1.1，超出时等比例降低，为素材峰值留余量。环境与交通归 `Ambience`，音乐归 `Music`，其余归 `Effects`。

城市循环在桌面准备界面即播放，音乐在第一次开跑后进入。撞车时轮声、风和刹车淡出，音乐压低；重开清理单次声音并从音乐起点重播。`Time.timeScale == 0`、应用暂停或失焦时暂停声音；恢复后续播。关闭再开启组件不会叠加事件订阅或音源。

右下角 **Sound** 打开四组滑杆；**M** 静音/恢复。设置存入 `CityScooter.Audio.*` PlayerPrefs，关闭面板、应用暂停或退出时保存。浏览器禁用存储时，本次会话仍可调音。已有玩家设置会覆盖 Bank 默认值；美术调音时应先留意这层覆盖。

金属刮擦目前按包含人行道的最外侧骑行边界接触判断，不代表完整的沿栏杆滑杆玩法。以后增加真实 grind/surface 系统时，可复用相同 ID 与声音池，把触发条件替换为实际接触事件。

导入标准由 `ComicCityAudioBuilder` 维护：

| 资源 | 桌面加载 | 压缩 | Web 覆盖 |
|---|---|---|---|
| 音乐 | Streaming | Vorbis，quality 0.8 | DecompressOnLoad |
| 城市长循环 | DecompressOnLoad | Vorbis，quality 0.8 | DecompressOnLoad |
| 其余音效 | DecompressOnLoad | PCM | DecompressOnLoad |

所有采样率覆盖为 48 kHz。Web 最终由 Unity 转为浏览器音频格式；运行时仅使用基础 AudioSource 播放、音量、音高及位置控制，没有原生 DSP 插件依赖。

## 再生成与替换

通常只需在 Bank 调音，不必重新生成 WAV。需要修改音色时：

1. 修改 `tools/prepare-comic-audio.py` 的裁切、滤波、合成或母带参数。替换外部作品时同步更新 `sources.json`、来源证据及 CREDITS；保留明确的许可和原始文件校验值。
2. 使用 Python、numpy 与 FFmpeg 运行 `python tools/prepare-comic-audio.py`。脚本离线工作、固定随机种子，输出 WAV、catalog、测量表及试听。源文件缺失时才使用 `python tools/fetch-comic-audio.py` 恢复六个已选来源。
3. Unity 菜单 `CityScooter > Comic City > 4 - Import and Validate Audio` 更新导入与 Bank。完整 Comic City Build 也会执行这一步。已经调过的 Bank 音量、音高、优先级等设置会保留，素材引用会刷新；新 ID 从 catalog 取默认值。若需让旧 ID 使用新的配方参数，应在 Bank 同步调整该条设置。
4. 当前两张场景已经接入。新场景可通过现有 Apply to Main 流程安装音频，或给 GameController 的子对象添加 Director/Controls，关联 Bank、GameController、Rider；主摄像机仅保留一个 AudioListener。
5. 运行 `powershell -NoProfile -ExecutionPolicy Bypass -File tools/run-comic-city.ps1 -Mode Build` 做完整生成与导入；`-Mode PlayMode` 验证运行逻辑。这些命令使用项目内独立临时 Unity 工程。

## 验证记录与平台边界

- Unity 6000.3.28f1：21/21 条素材导入有效，声道、时长及引用通过校验，记录在 `audio-src/comic-city/unity-validation.txt`。
- 当前 PlayMode 25/25 通过。音频回归覆盖滚动/刹车/跳跃/落地、实际拾取/碰撞/重开、声音池/静音/暂停/重复启用、面板，以及随限速进度加速到 1.5× 并在重开时恢复。测试结果位于 `Logs/comic-city-PlayMode-results.xml`。
- 已查看运行时音量面板截图；源 PCM 的格式、峰值和循环端点已自动检查。这些检查不等于在最终扬声器、耳机或浏览器上完成听感验收。
- 尚未执行本次音频的 Web 构建和目标设备试听。浏览器首次播放通常需要点击或按键；若仅用 micro:bit 消息开跑，应先点击 **Sound** 解锁。Unity Web 的 AAC 解码可能影响循环接缝，需在 Chrome/Edge 的最终构建听完整的 48 s 循环、反复跳跃/刹车并切换页面检验。参见 [Unity 官方 Web 音频说明](https://docs.unity.com/en-us/engine/6000.0/manual/platform-specific/webgl/develop/audio)。
