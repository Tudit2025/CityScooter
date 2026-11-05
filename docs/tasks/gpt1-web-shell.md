# 任务书：GPT Pro #1 — 网页外壳（Web shell）

> 把这整份文件交给 GPT。它需要能读写 GitHub 仓库 `Tudit2025/CityScooter`，从 `sprint/day1-skeleton` 分支（或者已经合并的 `main`）开始工作。

## 背景

CityScooter 是一款 Unity 6 做的城市滑板车无尽跑酷游戏，给 WA 学校 Year 9 学生上 micro:bit + MicroPython 课用。游戏打成 Web 包放在 Vercel，学生在 Windows 机房用 Chrome 或 Edge 打开。学生在 micro:bit 上用 MicroPython 写"控制器"：用 USB 把转向、跳、刹车这些命令发给游戏；游戏把撞车、捡到礼物、当前速度这些事件发回板子，由学生的代码决定板子怎么显示、怎么响。

板子不够每人一块，所以页面里还要嵌入官方 MicroPython 模拟器。没有板子的学生也能用自己的代码玩。

**第一次上课是周一 11 月 9 日。**

你负责的是 Unity 画布外面那一层 HTML/JS，也就是"网页外壳"。

## 先读这些

1. `docs/bridge.md`：外壳和 Unity 之间的接口，以及模拟器嵌入的全部规则。**这是硬契约，函数名和参数格式都不能改**；需要新接口先提出来。
2. `docs/protocol.md`：学生程序和游戏之间的消息格式。
3. `Assets/WebGLTemplates/CityScooter/`：Claude 写的最小可用版 v0，只支持 USB 板子，可以参考，也可以整个重写。
4. `tools/labcheck/labcheck.js`：机房体检页，里面的 Web Serial 和模拟器嵌入代码已经在真实环境验证过，可以直接借用。

## 交付物

1. 完整外壳，替换 `Assets/WebGLTemplates/CityScooter/index.html` 和 `TemplateData/` 下的文件。
2. `tools/shell-dev/`：一个不依赖 Unity 的开发页。里面放一个假的 `createUnityInstance`，返回的对象有 `SendMessage(obj, method, arg)`，调用内容打印到页面日志上；1 秒后自动调用 `CityScooterShell.onUnityReady("0")`。页面上加几个按钮，模拟 Unity 调用 `sendToDevices("*", "crash" / "gift" / "spd:N" / "go")`。用 `python -m http.server` 在 localhost 打开就能开发（localhost 算安全上下文，Web Serial 可以用）。

## 硬约束

- 纯原生 JS（ES2020），不用框架，不用 npm 构建，**不引用任何 CDN 或外部字体**。学校网络不可控，而且 Unity 会把 TemplateData 原样复制进构建。
- `index.html` 里的 Unity 模板变量必须保留，包括 `{{{ LOADER_FILENAME }}}`、`{{{ DATA_FILENAME }}}`、`#if USE_WASM` 等，照 v0 的写法。
- 学生看到的界面全部用英文（WA 学校），代码注释也用英文。
- 不提交 `.meta` 文件（Unity 生成，Claude 统一提交）；不改 Unity C#、场景、ProjectSettings。
- 目标浏览器：Windows 上的 Chrome 和 Edge；屏幕按 1366×768 和 1920×1080 设计。

## 功能（按优先级）

### P0：周五 11/6 18:00（珀斯时间）前合入

当天晚上要打第一个完整 Web 包。

1. **布局**：顶部状态栏、游戏画布（尽量大）、侧边或底部面板（串口监视器、模拟器），再加一个全屏按钮。
2. **USB 模式**：
   - 按钮"Connect micro:bit"，按 bridge.md 用 VID 0x0D28 过滤、115200 打开；最多 4 块板。
   - 设备 ID 用 `usb1`、`usb2`…，重连时沿用原 ID。
   - 页面加载时用 `getPorts()` 自动打开已授权的板子；拔插、重新刷程序后自动重连。
   - 收到 `hi:` 行后显示 V1 或 V2。
   - 每块板的写操作要排队。
   - 这几种错误要有清楚的英文提示：浏览器不支持、端口被占用（"another tab may be using this board, e.g. python.microbit.org — click Disconnect there"）、用户取消、列表是空的（"try another cable — some only charge"）。
   - **连上了但不发数据**：某块板连上后连续 3 秒一行都没收到，就在这块板旁边提示 "Connected, but this micro:bit is not sending anything. Flash a program that prints, like the starter. Code you only ran in the REPL is gone after unplugging."；收到数据后提示自动消失。11/5 实测遇到过：拔插后板子跑回闪存里旧的 `main.py`，它不发协议，游戏里看起来就像没连上。
3. **转发**：所有设备发来的行按 bridge.md 合并，每帧发一次给 Unity；`sendToDevices` 写回目标设备。
4. **串口监视器**（上课时很多讲解点都靠它）：
   - `st:` 不逐行刷屏，显示"当前转向值 + 每秒条数"。
   - `jp` 显示**最近 1 秒的次数，用大字**。第 4 课要用它对比 `was_pressed()` 和 `is_pressed()`。
   - `br` 显示是否正在刹车。
   - 游戏发出的消息（go/crash/gift/spd）用另一种颜色显示。
   - 不认识的行（学生自己的 print）原样显示。
   - 含 `Traceback` 或 `Error` 的行显示成红色，附一句英文提示，例如 NameError 提示 "check the spelling of a variable name"。
   - 格式不对的命令（`st:abc`、`st:`）显示成黄色，提示 "st: needs a number, e.g. st:-35"。
   - 支持暂停、清空，最多保留约 300 行。

### P1：周六 11/7 中午前

5. **模拟器模式**，严格按 bridge.md 的"官方模拟器嵌入"一节：
   - 代码编辑区：用等宽字体的 textarea 就行；Tab 键插入 4 个空格。
   - 预填 `TemplateData/starter.py`（GPT #2 会交付这个文件，交付前先用 `tools/labcheck/labcheck.py` 的内容代替）。
   - 提供"Download .py"按钮；草稿存到 localStorage，读写都要包在 try/catch 里。
   - **▶ 规则**：第一次运行时要明确提示学生点模拟器里的 ▶。收到 `request_flash` 之后回 `flash`；从那以后"Run"按钮可以直接 flash。在第一次 `request_flash` 之前绝对不能 flash。另外提供一个"Reset simulator"按钮，用来重新加载 iframe。
   - 键盘映射只在模拟器模式下、并且焦点不在输入框时生效：
     - ← / → 把 `accelerometerX` 平滑地推到 ±700，松开回到 0
     - A、B 对应 `buttonA`、`buttonB`
     - 按住 M 时 `soundLevel` 为 200，松开回到 10
     - 按住 L 对应 `pinLogo`

     屏幕上要有一张小卡片说明这些按键。
   - 进入模拟器模式时调用 `SetKeyboardEnabled("0")`，离开时调用 `"1"`。
   - 设备 ID 用 `sim`。`serial_output` 要自己分行后再转给 Unity；游戏消息用 `serial_input` 发给模拟器，末尾加 `\n`。
6. **模式切换**：Keyboard / micro:bit (USB) / Simulator 三个按钮，当前模式和连接状态要一眼能看出来。
7. **帮助面板**：
   - "How to connect"，分 3 步说明。
   - 故障排查：只能用 Chrome/Edge；板子被占用时，到 python.microbit.org 点 Disconnect，或者关掉那个标签页；重新插线；换一根线。

### P2：有时间再做

8. 每块板最近 10 秒的 `jp` 次数柱状图。
9. 用 localStorage 记住上次用的模式。

## 验收

- 在 Chrome 和 Edge 上，打开 `tools/shell-dev/` 页面连真板子（板子刷 `tools/labcheck/labcheck.py`，V1、V2 都行）：
  - 能看到转向值；按 A 时 jp 次数会变。
  - 点"crash"以后，板子回 `echo:crash`，并显示哭脸。
- 模拟器模式：粘贴代码，点一次 ▶，用 ←/→ 能看到转向变化；"crash"能送到模拟器（它回 `echo:crash`）。
- 两块板同时连接时互不干扰；刷新页面后，已授权的板子不弹窗就自动重连。
- 控制台没有报错；所有 localStorage 读写都包在 try/catch 里。

## 交付方式

- 新建分支 `feat/web-shell`，小步提交，提 PR，标题写清楚是 P0 还是 P1。
- 合并之前 Claude 会打 Web 包实测，有问题会在 PR 里留言。
