# 网页外壳 ↔ Unity 接口（bridge v0）

Unity 的 Web 构建外面有一层网页外壳（HTML/JS），负责：用 Web Serial 连 USB micro:bit、嵌入官方 MicroPython 模拟器、显示串口监视器。
外壳和 Unity 之间只走下面这些调用。改动这里要同时改两边，先在群里说一声。

- Unity 侧：`Assets/_Project/Scripts/Runtime/Devices/DeviceHub.cs`、`WebShellBridge.cs`、`Assets/_Project/Plugins/WebGL/CityScooterBridge.jslib`
- 外壳侧：`Assets/WebGLTemplates/CityScooter/`（`index.html` + `TemplateData/`）。11/6 起是完整版，由 Claude 按 `docs/tasks/gpt1-web-shell.md` 完成，包括 USB、模拟器模式、串口监视器和帮助面板。11/7 起加了积木编程（积木生成 MicroPython），并换成漫画风，见 `docs/blocks.md`。页面结构由 `TemplateData/shell.js` 生成；模拟器模式预填 `TemplateData/starter.py`，它是 `microbit/starter.py` 的副本，改 starter 时两边要一起改。
- 不用 Unity 也能开发外壳：在仓库根目录运行 `python -m http.server 8000`，打开 http://localhost:8000/tools/shell-dev/ 。这个页面用的是同一份 `shell.js`，配一个假的 Unity，还有按钮可以模拟游戏发 go/crash/gift/spd，以及竞速和道具赛的 pos/fin/item/hit/tx。

## 设备 ID

`usb1`、`usb2`… 表示 USB 直连的板子，按连接顺序编号，重连时沿用原编号。`sim` 表示页面里嵌入的模拟器。ID 里不能出现 `|` 和换行。

## JS → Unity（`unityInstance.SendMessage`）

目标对象一律是 `"DeviceBridge"`。它由代码在加载场景前自动创建，所以场景文件里没有这个对象。

| 方法 | 参数 | 说明 |
|---|---|---|
| `OnDeviceLines` | `"usb1\|st:-35\nusb1\|jp\nsim\|hello"` | 一条或多条消息，用 `\n` 分隔，每条是 `设备ID\|原始行`。外壳负责分行，只能发完整的行；建议每帧（requestAnimationFrame）合并发一次 |
| `OnDeviceStatus` | `"usb1\|usb\|1\|v2"` | `设备ID\|类型\|1 表示连上，0 表示断开\|版本`，版本可以留空 |
| `SetKeyboardEnabled` | `"1"` 或 `"0"` | 模拟器模式下键盘用来操作模拟器，这时要关掉 Unity 自己的键盘转向，否则同一次按键会转两次 |

在收到 `onUnityReady` 之前不要调用 SendMessage，先把消息缓存起来。

## Unity → JS（jslib 调用 `window.CityScooterShell`）

| 函数 | 说明 |
|---|---|
| `CityScooterShell.onUnityReady(protocolVersion)` | DeviceBridge 就绪，可以开始 SendMessage。当前协议版本是 `"0"` |
| `CityScooterShell.sendToDevices(target, line)` | 把游戏消息（`go`、`crash`、`gift`、`spd:N`）发给设备；`target` 是设备 ID 或 `"*"`（全部）。外壳负责加 `\n`，USB 设备写串口，`sim` 设备发 `serial_input`。无线竞速的 `pos:` / `fin:` 也走这里，照常发给设备，只是串口监视器每秒只显示最新一条。道具赛的 `item:` / `hit:` / `blocked:` / `tx:` 也走这里；`tx:` 游戏连发 3 次，3 次都发给设备，监视器只显示一次 |

**课堂版只用 micro:bit**（2026-11-09 起）：`index.html` 调用 `CityScooterShell.boot(..., { keyboard: false })`。
- 页面上没有 Keyboard 模式按钮，也没有键位表。
- 不管在哪个模式，Unity 都只收到 `SetKeyboardEnabled("0")`，所以方向键、空格、F/R/E/T 都不起作用。
- 游戏里的提示文字也不再提任何按键，只说用 micro:bit。
- 以前选过 Keyboard 模式的浏览器会自动切到 micro:bit (USB)。
- 模拟器模式保留：学生还是要给虚拟 micro:bit 写程序。
- 要恢复键盘，把 `index.html` 里的 `keyboard: false` 改成 `true`，再重新打包。
- 开发页 `tools/shell-dev/?keyboard=0` 可以预览课堂版。

键盘焦点：Unity 设置了 `WebGLInput.captureAllKeyboardInput = false`，只有画布获得焦点时 Unity 才收键盘，这样外壳里的输入框可以正常打字。外壳要在点击画布时调用 `canvas.focus()`。

## Web Serial 约定

- `navigator.serial.requestPort({ filters: [{ usbVendorId: 0x0d28 }] })`，0x0D28 是 micro:bit 的 USB 厂商号；以 `baudRate: 115200` 打开。
- 页面加载时调用 `navigator.serial.getPorts()`，把以前授权过的板子直接打开，不用再弹窗。
- 监听 `connect` / `disconnect` 事件。学生重新刷程序或重新插线时，要自动重连并沿用原来的设备 ID。
- 一个串口同一时间只能被一个标签页打开。打开失败时给学生看清楚的提示，例如"另一个标签页正在用这块板"。
- 每块板的写操作要排队，同一时间只能有一个 writer。

### 断线问题（第一节课后，11/9 修）

第一节课上，V1 板子经常要刷新好几次网页才连得上，否则会断开；V2 也有，但少一些。外壳原来有两个漏洞：

1. **打开串口失败就放弃。** 刚刷新网页、刚插上或刚刷完程序时，Windows 可能还没放开端口，或者 USB 驱动还没加载好（V1 更慢），第一次 `open()` 会失败。
   - 现在最多试 6 次，间隔 0 / 0.25 / 0.5 / 1 / 1.5 / 2.5 秒，约 6 秒。
   - 试的时候提示 "Connecting to the micro:bit…"。全部失败才显示"另一个标签页可能在用这块板"，并建议拔下来数到 3 再插上。
2. **连接死了却没收到 disconnect 事件。** 读取流出了致命错误（`readable` 变成 null）但板子还插着时，读取循环停了，外壳却仍然显示"已连接"，数据再也不来，只能刷新网页。
   - 现在会自动关闭端口再重新打开。

另外三处改动：
- 打开时 `bufferSize: 8192`。Chrome 默认只有 255 字节，Unity 加载或卡一帧时就会溢出（BufferOverrunError）。这种读取错误不致命，Chrome 会给一个新的 `readable`，外壳接着读。
- 页面关闭（`pagehide`）时主动关掉端口，让刷新后的页面马上能打开。
- **USB 日志**：每次打开失败、读取出错、重连、拔出，都会在串口监视器里显示一行黄色的 "USB: …"。老师也可以在浏览器控制台输入 `CityScooterShell.usbLog`，拿到带时间的完整记录。下节课出问题时，看这里就知道是哪一种。

外壳管不到的部分：
- V1 的接口固件（DAPLink）建议升级到 0253（microbit.org/firmware 列出的 V1 最新版）。板子插上后打开 MICROBIT 盘里的 `DETAILS.TXT`，看 "Interface Version"。
- 换线、不要用 USB 集线器或机箱前面板的口，也能减少断线。
- 如果 V1 上学生的程序报 MemoryError 停了，板子也会显得"连着但不发数据"。这种情况串口监视器里有红色的错误。

### 看门狗和加载重试（第二节课后，11/10 修）

第二节课的两个现象，很可能是同一件事的两面：

- **板子连上一会儿就不动了，刷新页面才好，过一会儿又这样。** 这种状态下没有任何错误，也没有 disconnect 事件：串口看起来开着，`read()` 一直等。刷新页面等于"关掉串口再打开"。现在外壳自己做：
  - 连着的板子超过 4 秒没有任何一行（`REVIVE_AFTER_MS`），就取消读、关串口、重新打开；还是没有就在 8、16、32 秒后再试，一共 4 次，之后等板子自己再说话（它可能只是没有程序）。
  - 发给板子的一次写入 3 秒没完成（`WRITE_STUCK_MS`），同样重开串口；以前一次卡住的写入会把后面所有消息都堵住。
  - 重开期间**不告诉游戏**板子断了（不发状态 0），游戏里看不出来。
  - 每一帧的 `pump` 套了 try：以前这里抛一次异常，板子的输入就再也到不了游戏，直到刷新。
  - USB 日志存进 localStorage（`cityscooter.usbLog`），刷新以后还在。USB 页签里的 **Copy a report** 把浏览器信息、每块板子重开/重连/读错误的次数、加载问题和 USB 日志复制到剪贴板，让学生发给老师。日志里出现 "no data for N s: opening the port again" 接着 "data again after opening the port again"，就说明确实是这种卡死，而且自动恢复了。
- **有的电脑打不开游戏，提示里有 "async … wasm"，换个浏览器就好。** 这是浏览器没取到或没能启动游戏的代码文件（下载中断、缓存里的坏副本，或者同一个标签页刷新太多次以后内存不够）。学生为了救板子反复刷新，正好容易触发它。
  - 加载失败会自己重试两次：清掉 Unity 自己的缓存，给文件地址加 `?retry=…`，不走任何缓存。下载 45 秒没有进度（启动阶段 120 秒）也算失败。
  - 还不行就停下，写明怎么做（检查网络点 Try again；内存不够就关掉标签页重开），并显示浏览器的原话，也记进报告。
  - 打包文件按内容哈希命名（`BuildScript`：`nameFilesAsHashes`），新版本是新地址，不会再有"旧缓存配新文件"；Vercel 上 `/Build/*` 设成永久缓存（`deploy-web.mjs`），一台电脑只下载一次。Unity 自己那份 IndexedDB 缓存关掉了（`dataCaching = false`），少一层会出错的东西。
  - 页面离开时调用 `unityInstance.Quit()`，把内存还回去。

## 官方模拟器嵌入（已于 2026-11-05 在 vercel.app 上实测）

- 地址：`https://python-simulator.usermbit.org/v/0.1/simulator.html`，MIT 协议。官方说明嵌入 API 还不稳定，出问题再联系 support@microbit.org。
- 只接受 `event.source === iframe.contentWindow` 的消息。postMessage 的 targetOrigin 写 `https://python-simulator.usermbit.org`。
- **第一次必须由学生点模拟器里自己的 ▶ 按钮。** 模拟器要在用户点击时创建声音系统，点击后它发 `request_flash`，我们再回 `flash`。在这之前主动 `flash` 会在 iframe 里抛出 `Context must be pre-created from a user event`，之后模拟器会卡死，只能重新加载 iframe。收到过一次 `request_flash` 之后，就可以随时主动 `flash`（学生改了代码再运行时用）。
- 发给 iframe 的消息：`{kind:"flash", filesystem:{"main.py": Uint8Array}}`、`{kind:"serial_input", data:"crash\n"}`、`{kind:"set_value", id, value}`，以及 `stop`、`reset`、`mute`、`unmute`、`radio_input`。
- iframe 发来的消息：`ready`（带 `state`，里面有全部传感器 id）、`request_flash`、`serial_output`（`data` 是文本片段，**不保证按行切分**，外壳要自己分行）、`state_change`、`radio_output`、`internal_error`、`log_output`、`log_delete`。
- `set_value` 可用的 id 和取值范围：
  - `accelerometerX/Y/Z`：-2000～2000，单位 mg
  - `buttonA`、`buttonB`、`pinLogo`、`pin0`、`pin1`、`pin2`：0 或 1。设成 1 会计一次按下，所以 `was_pressed()` 能用
  - `soundLevel`、`lightLevel`：0～255
  - `temperature`、`compassX/Y/Z`、`compassHeading`
  - `gesture`：字符串，例如 `"shake"`

  值超出范围时 iframe 里会抛异常，外壳要先截断再发。
- 实测数据（无界面 Chrome）：约 2 秒就绪；游戏 → 模拟器 → 回显往返 60～70 ms。

### 模拟器里的无线（11/9 实测，无线竞速用）

对照了模拟器的源码（micropython-microbit-v2-simulator 的 `src/board/radio.ts`、README 的消息表），也下载了线上 `/v/0.1/build/simulator.js` 核对，并在嵌入的模拟器里实际跑过（`tools/shell-tests/race.test.js` 每次都会再验证一遍）：

- **发给模拟器**：`{kind: "radio_input", data: Uint8Array}`。`data` 必须是 `Uint8Array`，否则 iframe 里抛 "Invalid radio_input data field"。
- **文字要带 3 字节头。** `radio.send("p:AMY:1:0")` 在空中发的是 `\x01\x00\x01` 加文字，`radio.receive()` 会检查并去掉这 3 个字节。
  - 所以喂给模拟器的要是 `[1, 0, 1, ...文字的字节]`。
  - 不带头时 `radio.receive()` 抛 `ValueError: received packet is not a string`，这一包被丢掉，下一包照常。
- **长度**：一包最多 32 字节（`radio.config` 的 `length`，默认 32），去掉头，文字最多 29 个字符，多的被截掉。`go:99999:canningbridge:5000` 是 27 个字符，放得下。
- **队列**：模拟器按程序的 `queue` 存，满了就丢（`queue=10` 时连发 12 条，只收到前 10 条）。
- **组号**：模拟器不看组号，`radio_input` 一律当作"你这组的"。
- **无线没开时不能喂。** 线上这个版本在无线关着时收到 `radio_input` 会在 iframe 里抛 TypeError（新版源码已经修了）。外壳只在知道无线开着时才喂：
  - 模拟器在无线开关、换组时发 `{kind: "state_change", change: {radio: {type: "radio", enabled, group}}}`；
  - `radio.config(queue=10)` 会先关再开，所以会连着来一条 `enabled: false` 和一条 `enabled: true`；
  - 每次 flash 外壳先当作无线关着，等新程序打开。
- **有包在排队时不能停程序。** 程序停下（flash 新代码、切到别的模式）的那一刻，如果 `radio.receive()` 正好有包要读，模拟器会报 internal_error "Must be running as called via HAL"（它的 HAL 在板子已经算停了以后还去取包）。外壳在机器人开着时先停喂，等 0.25 秒（程序这时把队列读空），再 flash 或 stop。
- **模拟器发出来的**：`{kind: "radio_output", data: Uint8Array}`，就是 `radio.send` 发出去的字节，带同样的 3 字节头，例如 `[1,0,1,112,58,65,77,89,…]` 是 `p:AMY…`。外壳把它显示在串口监视器里，每秒一行（"radio sent: 4 messages/s, last p:AMY:1234:-2"）。

### 机器人对手（Robot racers）

模拟器面板（micro:bit 下面）有一排：**Robot racers** 开关、**Send go** 按钮和一行状态。没有板子的同学用它测试自己的转发代码：代码在模拟器里能跑通，拿到真板子就能直接比赛。

- **Robot racers 打开后**：三个机器人 BOT1、BOT2、BOT3，速度 13、15、17 m/s（游戏最高速度约 22，不会被标成 "?"）。每 0.25 秒各往模拟器的 `radio_input` 喂一条 `p:BOTn:<米数>:<车道>`，车道在 -2～2 之间慢慢变。
  - 骑到终点（2000 m）以后，每 0.5 秒发一次 `f:BOTn:<秒数>`，持续 10 秒；之后每秒一次 `p:BOTn:0:0`，和真的游戏在大厅里一样。
  - 一打开就从 0 m 开始骑，不用等 `go:`，学生马上能看到效果。
- **Send go**：往模拟器喂 `go:<1～99999 的随机种子>:uwa:2000`，0.5 秒内 5 次，和真的发令板一样。
  - 机器人开着时，它们停在 0 m 等 3 秒（游戏在倒数），再从 0 m 开始比。
  - 串口监视器记一行 "Robots sent go:… by radio"。
- 学生的转发代码把收到的打印成 `rx:p:BOT1:…`，外壳当作 `sim` 设备的行转给 Unity，游戏就能画出三个对手。
- 状态行显示每个机器人骑到了哪；程序没开无线时提示 "your code has not turned the radio on (join race)"，这时什么也不喂。
- 只在模拟器模式、代码在运行、无线开着的时候喂。切到别的模式就停。
- **机器人用道具（道具赛，11/10，见 `docs/item-race.md`）**：Robot racers 开着时，每个机器人每隔 20～40 秒（各自随机）对你用一次 `rocket` 或 `bolt`，这样没有板子的同学也能测 "when an item hits me"。
  - 往 `radio_input` 喂 `i:BOT1:rocket:<你的名字>:<序号>` 或 `i:BOT1:bolt:<序号>`，0.3 秒内 3 次（和游戏的 `tx:` 一样），带同样的 3 字节头。序号每个机器人自己数，0～9 循环。
  - "你的名字"是模拟器里的程序发的 `race:名字:组号` 里的名字。程序没发过 `race:` 时不知道该打谁，只用 `bolt`。
  - 串口监视器记一行 "BOT1 sent i:BOT1:rocket:AMY:3 by radio: a rocket for you"。学生的程序把它转成 `rx:i:…` 以后，监视器再记一行 "radio: BOT1 used rocket on AMY"，真的游戏随后发 `hit:rocket`（或者 `blocked:rocket`）。开发页的假 Unity 不会回 `hit:`，用工具栏的 hit 按钮。
  - 打开开关和 Send go 都会重新计时（Send go 是倒数结束后 20～40 秒）。
  - "有包在排队时不能停程序"的规则不变：外壳一停喂，没发完的那两次重复也不再发，然后照常等 0.25 秒再 flash 或 stop。
  - 测试和控制台可以让机器人马上用一次：`CityScooterShell.robotUseItem(1, "rocket")`（机器人 1～3，`"rocket"` 或 `"bolt"`），返回发出的无线消息；机器人没开、或者喂不了时返回空字符串。
- 模拟器的 `radio_output` 是 `i:` 时（学生的程序转发了游戏的 `tx:`），监视器记一行 "radio: you used rocket on BOT2"，同一个序号 20 秒内只记一次。
