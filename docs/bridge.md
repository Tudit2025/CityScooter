# 网页外壳 ↔ Unity 接口（bridge v0）

Unity 的 Web 构建外面有一层网页外壳（HTML/JS），负责：用 Web Serial 连 USB micro:bit、嵌入官方 MicroPython 模拟器、显示串口监视器。
外壳和 Unity 之间只走下面这些调用。改动这里要同时改两边，先在群里说一声。

- Unity 侧：`Assets/_Project/Scripts/Runtime/Devices/DeviceHub.cs`、`WebShellBridge.cs`、`Assets/_Project/Plugins/WebGL/CityScooterBridge.jslib`
- 外壳侧：`Assets/WebGLTemplates/CityScooter/`（`index.html` + `TemplateData/`）。11/6 起是完整版，由 Claude 按 `docs/tasks/gpt1-web-shell.md` 完成，包括 USB、模拟器模式、串口监视器和帮助面板。11/7 起加了积木编程（积木生成 MicroPython），并换成漫画风，见 `docs/blocks.md`。页面结构由 `TemplateData/shell.js` 生成；模拟器模式预填 `TemplateData/starter.py`，它是 `microbit/starter.py` 的副本，改 starter 时两边要一起改。
- 不用 Unity 也能开发外壳：在仓库根目录运行 `python -m http.server 8000`，打开 http://localhost:8000/tools/shell-dev/ 。这个页面用的是同一份 `shell.js`，配一个假的 Unity，还有按钮可以模拟游戏发 go/crash/gift/spd。

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
| `CityScooterShell.sendToDevices(target, line)` | 把游戏消息（`go`、`crash`、`gift`、`spd:N`）发给设备；`target` 是设备 ID 或 `"*"`（全部）。外壳负责加 `\n`，USB 设备写串口，`sim` 设备发 `serial_input` |

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
