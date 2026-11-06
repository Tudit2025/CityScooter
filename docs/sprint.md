# 冲刺：周一 11/9 第一节课（v0.1）

## 周一必须有（缺一个就不能上课）

1. 机房 Chrome/Edge 打开 Vercel 网址就能玩，帧率不低于 30。
2. 键盘能玩。
3. V1、V2 板子 USB 直连，跑起步程序能转向、跳、刹车。
4. 撞车、礼物、速度能发回板子。
5. 串口监视器能显示学生 print 的内容和 Python 报错。

最好也有：模拟器模式（没板子的学生用自己的代码玩）、栏杆（必须跳过去）、两块板合作、远景珀斯地标。

## 谁负责什么（目录归属，避免合并冲突）

| 谁 | 负责 | 目录 |
|---|---|---|
| 你（老师） | 机房测试、MicroPython 代码和教材定稿、Vercel、合并 PR、上课 | `docs/lessons/` 最终审核 |
| 技美 | 主场景、美术、灯光、UI 视觉、Web 性能；审核下面三个技美智能体的产出 | `Assets/_Project/Scenes/Main.unity`、`Art/`、`Prefabs/` |
| Claude | 项目骨架、协议、设备桥、玩法核心、构建部署；每次合并前跑编译和测试 | `Scripts/Protocol`、`Scripts/Runtime/{Devices,Controls,Game,Track,DevTools}`、`Scripts/Editor/BuildScript.cs`、`Plugins/WebGL`、`docs/protocol.md`、`docs/bridge.md` |
| GPT #1 | 网页外壳（`docs/tasks/gpt1-web-shell.md`） | `Assets/WebGLTemplates/CityScooter/`、`tools/shell-dev/` |
| GPT #2 | MicroPython 代码和第一课教材（`docs/tasks/gpt2-micropython-lesson1.md`） | `microbit/`、`docs/lessons/` |
| 技美智能体：天际线 | 珀斯天际线剪影分层图和生成器 | `art-src/skyline/` |
| 技美智能体：资源管线 | Kenney CC0 素材导入、带碰撞体的预制体、Street 路段、预算检查 | `Art/ThirdParty/`、`Prefabs/{Kit,Road,Obstacles,Pickups}/`、`Scripts/Editor/Art/` |
| 技美智能体：画面与性能 | Web 版 URP 设置、天空和光照、天际线视差架子、特效、自动降画质 | `Assets/Settings/`、`Art/{Shaders,Materials,Looks,Textures/Generated}/`、`Prefabs/{Environment,VFX}/`、`Scripts/Runtime/Rendering/`、`Scripts/Editor/LookDev/`、`Scenes/LookDev.unity`、`docs/art/` |

规则：

- **`Main.unity` 只有技美能改。** 设备总线不在任何场景里，由 `Bootstrap.cs` 在加载场景前创建。
- 改别人负责的目录前，先在群里说一声。
- 美术和玩法之间靠 `Scripts/Runtime/Track/` 的四个组件对接：路段 `RoadChunk`、刷怪点 `SpawnSlot`、障碍 `Obstacle`、礼物 `Gift`。约定是 1 单位 = 1 米，+Z 朝前，路面 y = 0，路段的轴心在起点、路中线上。
- Unity 生成的 `.meta` 文件必须和资源一起提交，不要手写。只用 Unity 6000.3.28f1（Unity 6.3 LTS），装的时候勾上 Web Build Support。

## 分支

- **11/6 起 `main` 就是基础**，包含骨架、玩法、设备桥、街景美术和 Unity 6.3 升级。大家都从 `main` 拉分支：`feat/...`、`art/...`。`sprint/day1-skeleton`、`art/lookdev`、`art/pipeline`、`art/skyline` 都已合进 `main`，不要再在上面开发。
- 线上 https://cityscooter.vercel.app 从 `main` 打包部署（`node tools/deploy-web.mjs --build`）。
- PR 要小，每天至少合两次。Claude 合并前在本机用批处理模式跑编译和 EditMode 测试。

## 每天的节点

| 日期 | 交付 |
|---|---|
| 周四 11/5 | 骨架和契约合入；机房体检（https://cityscooter-labcheck.vercel.app）结果出来。**实际完成**：骨架、玩法、街景美术合进 `main`；升级到 Unity 6.3；街景版网页包已上线，键盘和 V2 真板子都测过 |
| 周五 11/6 | GPT #2 中午交 starter 和答案，老师在机房用真板子测；GPT #1 18:00 前交外壳 P0；**晚上第一个完整 Web 包上 Vercel**，键盘和真板子都能玩一局 |
| 周六 11/7 | 功能做完：模拟器模式、栏杆、特效、开始/结束界面、远景地标；晚上用 V1、V2、模拟器三种方式彩排 |
| 周日 11/8 | 中午功能冻结，之后只修 bug；调性能、正式部署、打印教材、准备 Plan B（GitHub Pages 镜像；如果机房允许运行 exe，再打一个 Windows 版） |
| 周一 11/9 | 上课 |

## 常用操作

- **打开项目**：Unity Hub → Add → 选 `CityScooter` 文件夹 → 用 6000.3.28f1 打开。**不要用 6000.0.x 或其他版本打开**，否则 Unity 会把项目降级或重新序列化，提交后会和别人冲突。
- **有板子时在编辑器里测**：编辑器本身不读 USB 串口（网页版才用浏览器的 Web Serial），要靠串口桥中转。
  1. 板子用 python.microbit.org 刷一个会发协议的程序：`tools/labcheck/labcheck.py` 或第一课的 starter。
  2. 双击 `tools/serial_bridge.cmd`，它会自动找到 micro:bit 的 COM 口，拔插、重刷程序后会自动重连。
  3. 在 Unity 里点 Play，按板子上的 A（或空格）开始一局；倾斜转向，A 跳，按住 B 刹车。

  同一块板同时只能被一个程序占用，用串口桥之前要先断开浏览器里的连接。
- **没有板子时在编辑器里测**：点 Play，按 F1 打开设备面板；另开终端运行 `python tools/fake_microbit.py`，用 `--boards 2` 可以测合作，用 `--spam-jump` 可以看轮询写法的效果。面板上的 go/crash/gift/spd 按钮会把事件发回假板子，终端里能看到。
- **打 Web 包**：Unity Hub 里的 6000.3.28f1 要带 **Web Build Support 模块**（Installs → 齿轮 → Add modules），然后在菜单选 `CityScooter/Build Web`，输出到 `Builds/Web`。也可以用批处理模式：`Unity.exe -batchmode -quit -projectPath . -executeMethod CityScooter.EditorTools.BuildScript.BuildWebBatch`。
- **部署**：`node tools/deploy-web.mjs --build` 一条命令完成打包和上线到 `https://cityscooter.vercel.app`。打包前先关掉这个项目的 Unity 编辑器；如果已经用菜单打好了包，就不加 `--build`。想先发预览版用 `--preview`。脚本会给 `Build/*.unityweb` 加上 `Content-Encoding: br` 响应头，让浏览器原生解压，加载更快。没有这个头的托管（比如 GitHub Pages 备用）也能跑，因为构建开了解压兜底。
- **玩法数据**：所有手感参数都在 `Assets/_Project/Data/GameTuning.asset`。美术素材挂到 `Theme_PerthStreet.asset`，里面有路段、障碍、栏杆、礼物四个列表；列表为空时游戏自动用灰盒占位。`Main.unity` 里已经放了 `Prefabs/Game/GameSystems.prefab`。
- **Unity Smart Merge**（可选，减少场景合并冲突）：在 `git config` 里把 `merge.unityyamlmerge.driver` 设成 `"<Unity 安装目录>/Editor/Data/Tools/UnityYAMLMerge.exe" merge -p %O %B %A %A`。

## 已经验证过的结论（2026-11-05）

- 官方 MicroPython 模拟器可以嵌进我们的 vercel.app 页面。**第一次必须由学生点模拟器里的 ▶**，否则模拟器会卡死，细节见 `docs/bridge.md`。
- MicroPython 的 `uart.readline()` 会读到半行，学生代码必须先攒到换行再处理，见 `docs/protocol.md`。
- **11/5 晚升级到 Unity 6000.3.28f1（6.3 LTS）+ URP 17.3。** 每个人都要在 Unity Hub 装 6000.3.28f1 并勾上 Web Build Support；拉代码后第一次打开会重新导入整个项目，要几分钟。升级前的 6.0 版本打了 git 标签 `pre-unity-6.3`，需要时可以退回。
- Web 打包模块已装好。第一个 Web 包（灰盒版）已经上线 https://cityscooter.vercel.app ，无头 Chrome 验证过能加载、能和外壳通信、能收发板子消息。
- Unity 6 Web 平台的两个坑，编辑器里看不出来，只有打包后才暴露：
  - 6000.0 打包时，脚本里直接写 `WebGLInput` 会报 CS0103（编译脚本时没引用 `UnityEngine.WebGLModule`；6000.3 已经正常）。我们用反射设置，两个版本都能编译，`Assets/_Project/link.xml` 防止它被裁掉。
  - **升级 URP 后，URP 设置资源必须用新版 Unity 保存一次再打包。** 11/5 `art/lookdev` 的 Mobile_RPAsset 还是旧格式，打包时把 URP/Lit 的变体全裁掉了，网页里只剩天空和天际线，路和楼都看不见，打包却显示成功。现在 `BuildScript` 遇到旧格式的 URP 资源会直接停止打包；修复方法是菜单 `CityScooter/Tools/Upgrade URP assets`，然后提交改动的资源。
  - 运行时 `CreatePrimitive` 生成的物体自带的默认材质，打包后是**洋红色**（URP 17 的默认材质只在编辑器里有）。运行时新建的材质要基于一个会被打进包的材质资源，比如放在 `Resources/` 里，或者被场景、预制体引用。
