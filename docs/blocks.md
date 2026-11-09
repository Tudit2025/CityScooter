# 积木编程框架（Blocks → MicroPython）

学生在网页外壳里用积木编程，积木生成的就是 MicroPython，旁边实时显示对应代码。生成的程序可以直接在页面里的官方模拟器上运行；连着 USB 的真板子点 **Send to micro:bit** 就能写进去（见下面"发送到 micro:bit"）。V1、V2 都能用。

这套框架分成两层，**换一个游戏只需要写一个新的游戏插件**，外壳、编辑器、micro:bit 通用积木和主题都可以直接复用。

## 文件

除了 Blockly，都在 `Assets/WebGLTemplates/CityScooter/TemplateData/` 下：

| 文件 | 层 | 作用 |
|---|---|---|
| `web-vendor/blockly/`（仓库根目录） | 第三方 | Blockly 13.4.0，Apache 2.0（见 `LICENSE`、`README.txt`）。原样取自 npm 包，不走 CDN；积木音效已关掉。**不能放进 `Assets/`**：Unity 的网页模板预处理器会把 `<<<名字>>>` 当成引用内置文件，而 Blockly 的 CSS 里就有 `<<<PATH>>>`，放进去打包会直接失败。打包脚本会在 Unity 打包成功后，把它复制到 `Builds/Web/TemplateData/vendor/` |
| `fonts/` | 第三方 | Bangers 漫画字体，SIL OFL（见 `Bangers-OFL.txt`），只用在标题上 |
| `code/microbit-blocks.js` | 通用 | micro:bit 通用积木和对应的 MicroPython 生成器、程序组装、漫画主题、编辑器挂载、风险警告 |
| `code/game-cityscooter.js` | 游戏 | CityScooter 插件：专属积木、消息分发、工具箱分类、默认的 starter 积木 |
| `shell.js` / `shell.css` | 通用 | 外壳：积木和 Python 两个视图、按需加载 Blockly、分隔条、模拟器、USB、串口监视器 |

协议 v1（2026-11-07）加的积木：
- CityScooter 分类："出拳"（`print("pu")`）、"按铃"（`print("bl")`）；"当游戏说"的下拉里加了 `ko`、`warn:car`、`warn:ped`、`warn:hole`、`turn:-1`、`turn:1`、`turn:0`。
- 通用 Events 分类："当摇一摇 / 屏幕朝上 / 屏幕朝下时"，生成 `accelerometer.was_gesture(...)`，V1、V2 都能用。
- 连按（11/8）：
  - Events 分类的"当按钮 A 连按 N 次时"（N = 2～5）：按到第 N 下立刻触发，每下之间不超过 0.4 秒。
  - Input 分类的"按钮 A 连按了几次"：数值积木，1、2、3……停顿超过 0.4 秒后从 1 重新数。想按奇偶数区分，用"除以 2 的余数"。
  - 生成的 Python 会把计数写出来：`presses_a`、`last_press_a`、`running_time()`，学生能看懂原理。
- 长按（11/8）：
  - Events 分类的"当按钮 A 按住 N 秒时"（N = 0.5、1、2、3）：按住到这个时间就触发一次，同一次按住不会重复触发。
  - Events 分类的"当按钮 A 松开时"。
  - Input 分类的"按钮 A 按住了多久（毫秒）"：按住时一直变大；松开后保留这次按住的时长，直到下一次按下。
  - 生成的 Python 用 `held_since_a` 记下开始按下的时刻（`running_time()`），用 `held_a` 记下按住了多久。适合"按住蓄力、松手出招"这类玩法，例如按住 1 秒以上松手才加速，否则只是出拳。
- 触发游戏事件（11/8，CityScooter 分类）：
  - "让游戏开始 [警方追击 ▾]"：可选警方追击、直升机、警察放弃、左弯、右弯、对向车、慢车、路政、警察拦路、行人过马路、窨井。
  - "把通缉星级设为 [N]"。
  - 例子：当游戏说 `ko` → 变量"打中次数"加 1 → 如果"打中次数" = 3 → 让游戏开始警方追击。模拟器的 V2 和 V1 上都测过。
- 修了一个 bug：Variables 分类的"把变量增加"，Blockly 默认会生成 `from numbers import Number`，但 MicroPython 没有这个模块，程序一启动就报错。现在改成 `x = (x if type(x) in (int, float) else 0) + 1`。
- 让游戏说（11/8，CityScooter 分类）：
  - "让游戏说 [party]"发出 `say:party`，游戏把 `party` 转发给所有连着的板子，屏幕上也会显示。
  - "当游戏说词 [party] 时"接收。
  - 这样两块板只用 USB 就能经过游戏互相广播，学生也能自己设计事件。
  - 词的规则见 `docs/protocol.md`。
- 广播（11/8）：
  - Events 分类的"广播 [消息名]"和"当接收到 [消息名] 时"，就是 Scratch 的广播：一个事件可以让好几段积木各做各的，比如收到 `gift` 时广播 `party`，一段亮灯，一段放音乐。
  - 广播在每一轮循环的最后统一处理，每轮最多处理 20 条，免得两段积木互相广播停不下来。
  - 生成的代码用 `broadcasts` 列表。
- 无线（11/8，新的 Radio 分类）：
  - "无线：设置频道"（0～255，放在"开始时"里）、"无线：发送 [文字]"、"当无线收到消息时"、"收到的无线消息"。
  - 同一频道的板子能互相听到，V1、V2 都能用。适合两块板合作，比如副驾驶那块收到 `warn:` 后用无线告诉主驾驶。
  - 无线消息不会发给游戏，游戏只听 USB 串口。
- CityScooter 分类："加速"（`print("bo")`）、"卡通模式 开/关"；"当游戏说"里加了 `boing`、`tired`。
- 西澳警察（11/8）：
  - "靠边停车（投降）"（`print("sur")`）；
  - "当游戏说"里加了 `chase:1`、`chase:0`、`heli:1`、`heli:0`、`inc:C`、`inc:L`、`inc:R`、`arrested`、`busted`、`lost`；
  - "当通缉星级变化时"帽子积木和"通缉星级 (0-5)"数值积木，生成 `wanted_level = int(msg[7:])`，写法和 `spd:`/`game_speed` 一样；
  - "警车距离 (0-9)"数值积木，生成 `police_distance = int(msg[4:])`，开始是 9（很远）。
  - 用到这些积木时才生成 `wanted_level`、`police_distance` 两个全局变量，第一课的程序不变。
- 跳台和滞空（v0.3，11/8 晚）：
  - CityScooter 分类加了"做一个特技"（`print("trick")`）：从跳台飞起来以后才有用，第一次转体（SPIN），第二次后空翻（FLIP），轮流来；在地上什么也不做，一直狂发会"头晕"一会儿。
  - "当游戏说"里加了 `air:1`（飞起来了）、`air:0`（落地了）、`land:ok`（安全落地）、`land:trick`（做完特技落地）。工具箱里放了一个现成的"当游戏说 air:1"。
  - "让游戏开始"的下拉里加了"前面放一个跳台"（`do:ramp`）。
  - 例子：当游戏说 `air:1` → 做一个特技；当游戏说 `land:trick` → 让游戏开始"前面放一个跳台"。积木测试里用上了这两段，模拟器 V2 和 V1 上都跑过。
  - 这些只是多了几个 `if msg == ...` 分支和一行 `print`，没有新的全局变量，生成的程序几乎不变大（V1 内存）。
- 场景（v0.3，UWA → 珀斯市中心）：
  - "当游戏说"里加了 7 个 `zone:`：`zone:uwa`、`zone:matilda`、`zone:kingspark`、`zone:westperth`、`zone:cbd`、`zone:quay`、`zone:mountsbay`（下拉里写着 "entering Kings Park" 这样的说明）；
  - 新的帽子积木"当进入 [Kings Park] 时"（`cs_on_zone`，下拉是场景名，按路线顺序），和"当游戏说 zone:kingspark"放进同一个分支，只多一个 `elif msg == "zone:kingspark":`，不加全局变量，V1 的内存也够；
  - 开局时在 `go` 后面收到出发场景，之后每进一个新场景收到一次。例子：进 Kings Park 显示一棵树，进 Elizabeth Quay 放一段音乐。
  - 串口监视器里游戏发的 `zone:` 后面会标出场景名（"entering Kings Park"）；板子自己打印 `zone:...` 时提示这是游戏发的消息。
- 选出发场景（11/9）：
  - CityScooter 分类加了"start rides in [Kings Park]"（`cs_start`，下拉是 7 个场景，按路线顺序），生成 `start_zone = "kingspark"` 和 `print("start:" + start_zone)`。
  - 放在"开始时"里最方便。程序一开始发的那条游戏可能还没在听，所以生成的代码在每秒一次的 `hi:` 旁边再发一次 `start:`（只在用了这块积木时才有 `start_zone` 这个全局变量）。
  - 在开局画面上马上换过去；骑行中发的，下一局从那里出发。串口监视器只在场景变化时记一行"rides start in Kings Park"，不会每秒刷屏。

- 无线竞速（11/9，方案见 `docs/radio-race.md` 的"实现约定"）：
  - CityScooter 分类的最后，在标签 **Race** 下面按顺序放了 4 块（课件按这个顺序讲）：

    | 积木 | 放在哪 | 生成的代码 |
    |---|---|---|
    | "join race [12] as [AMY]" | "on start" | `race_name = "AMY"`；组号变了才 `radio_group = 12`、`radio.config(group=radio_group)`。主循环每秒一次（跟 `hi:` 一起）`print("race:" + race_name + ":" + str(radio_group))` |
    | "send my position by radio" | "on start" | `send_position = True`。主循环收到游戏的 `pos:` / `fin:` 时 `radio.send("p:" + race_name + ":" + msg[4:])` / `f:` |
    | "pass radio messages to the game" | "on start" | `pass_radio = True`。主循环每一轮把无线队列读空，每条 `print("rx:" + got_radio)` |
    | "start a race from [UWA] for [2000] m" | "when shaken"（或者骑车用不到的按钮） | `start_race("uwa", 2000)`：`go:<1～99999 的随机种子>:uwa:2000` 先 `print("rx:" + go)` 给自己的游戏（板子收不到自己发的无线），再 `radio.send` 5 次，每次隔 0.1 秒 |

  - 前三块只设变量，真正的活在主循环里做，所以学生误放进 "forever" 也没事：不会每轮都发东西，组号没变也不会重设无线。提示文字都以 `Put it in "on start".` 开头。
  - 名字字段带校验：去掉字母、数字以外的字符，转大写，最多 5 个（`amy!` 显示成 `AMY`），和游戏的规则一样；空名字不接受。组号 0～255，米数 300～5000。
  - 用到才生成：`race_name`、`send_position`、`pass_radio` 三个全局变量，和 `start_race()` 函数（加上 `import random`），只在用了对应积木时才有。第一课的程序不变。
  - 和 Radio 分类一起用：
    - 现在所有设组号的积木共用一个 `radio_group` 变量。"radio: set group" 生成 `radio_group = int(N)` 和 `radio.config(group=radio_group)`；和 "join race" 一起用时，最后执行的那块说了算，`race:` 报的也是这个组号。
    - 用了 "pass radio messages to the game" 时，"when radio receives a message" 和它共用同一个读空队列的循环，每条消息都先转给游戏，再跑学生的积木。否则两边抢消息，游戏会漏掉一大半。没用竞速积木时，"when radio receives" 还是每轮读一条，和以前一样。
    - 用了竞速积木时开机是 `radio.config(group=radio_group, queue=10)`：MicroPython 默认只存 3 条，一场 5 人、每人每秒 4 条，3 条不够。
  - 生成的代码都在模拟器的 V2 和模拟 V1 上跑过：机器人对手的 `p:` 转成 `rx:p:BOT1:…` 到了游戏；游戏发 `pos:` / `fin:` 后，模拟器的无线发出了 `p:AMY:…` / `f:AMY:…`；摇一摇发出 5 次 `go:`。
  - 用了竞速积木以后，第一课 starter 加上三块 "on start" 生成的完整代码，见积木测试的输出 `blocks_race.py`。和第一课相比多了：

    ```python
    import radio
    ...
    radio_group = 1  # micro:bits only hear others in the same group (0-255)
    race_name = ""  # your name in the race ("join race")
    send_position = False  # True: send where you are by radio
    pass_radio = False  # True: pass what the radio hears to the game
    ...
    radio.on()  # micro:bits in the same group hear each other
    radio.config(group=radio_group, queue=10)  # keep up to 10 messages until the loop reads them (3 is too few for a race)
    display.show(VERSION[1])  # 1 or 2 on the LEDs
    # on start
    race_name = "AMY"
    if radio_group != 12:  # a new group: tell the radio
        radio_group = 12
        radio.config(group=radio_group)
    send_position = True
    pass_radio = True

    while True:
        ...
        # radio from other micro:bits: read everything that arrived, every loop
        got_radio = radio.receive()
        while got_radio:
            if pass_radio:
                print("rx:" + got_radio)  # pass it on to the game
            got_radio = radio.receive()
        # messages from the game
        for msg in messages():
            ...
            elif send_position and msg.startswith("pos:"):
                radio.send("p:" + race_name + ":" + msg[4:])  # tell the others where you are
            elif send_position and msg.startswith("fin:"):
                radio.send("f:" + race_name + ":" + msg[4:])  # you crossed the line: tell them your time
        loops += 1
        if loops % 20 == 0:
            print("hi:" + VERSION)  # tell the game which micro:bit this is
            if race_name:
                print("race:" + race_name + ":" + str(radio_group))  # I want to race, in this group
        sleep(50)
    ```
  - 没加 `try/except`：如果同组有板子用 `send_bytes` 或 MakeCode 发了不是文字的包，`radio.receive()` 会报 `ValueError: received packet is not a string`，程序停下。串口监视器对这一行有提示（"换一个组号"）。课上讲"信任"时可以拿来演示。

Blockly 约 800 KB，只有进入 **micro:bit (USB)** 或 **Simulator** 模式时才加载，纯键盘模式的玩家不会下载它。

## 生成的程序长什么样

每个积木程序都是同一个形状，学生在 **Python made by your blocks** 里能看到全部代码：

```python
from microbit import *
import music                  # 用到才加
try:                          # 判断板子版本；V2 专属积木会先检查它，所以 V1 不会报错
    from microbit import microphone
    VERSION = "v2"
except ImportError:
    VERSION = "v1"
uart.init(baudrate=115200)
# 变量、插件的全局变量和辅助函数（CityScooter：buffer、messages()）
# 用了无线积木才有：radio.on()、radio.config(group=radio_group)（竞速积木再加 queue=10）
display.show(VERSION[1])
# "on start" 积木
while True:
    # "forever" 积木
    # "when button A/B is pressed" 积木：每个按键只调用一次 was_pressed()
    # 插件的循环末尾（CityScooter：分发游戏消息，每秒报一次 hi:）
    sleep(50)
```

- 帽子积木（`on start`、`forever`、按键事件、游戏消息事件）的内容按类别放进程序里固定的位置。积木在画布上怎么摆，都不影响生成的代码。
- 没接在帽子积木下面的散积木会变灰，什么也不做（用的是 Blockly 的 `disableOrphans`）。
- 缩进是 4 个空格，和课上教的一致；学生的变量名不会和框架用到的名字冲突（框架会保留 `VERSION`、`buffer` 等名字）。

## V1 / V2 和硬件的坑（已经处理）

- **V2 专属积木**（`logo is touched (V2)`、`sound level (V2)`）生成的是带版本判断的表达式，在 V1 上分别返回 False 和 0，不会报错。
- **声音和引脚 0**：micro:bit 的声音默认走引脚 0，这时再读 `pin0.is_touched()` 会报 `Pin 0 in music mode`。现在的处理：
  - V2 只用板载扬声器（`SOUND_PIN = None`），引脚 0 留给触摸；
  - V1 没有扬声器，声音只能走引脚 0。所以工作区里同时出现"播放声音"和"引脚 0 被触摸"时，积木上会显示警告，提示改用引脚 1 或 2；串口监视器里这个报错也有对应提示。
- `show` 积木用 `display.show(..., wait=False)`，不会卡住转向；框架里没有 `scroll` 积木。

## 积木和 Python 怎么切换

- 默认显示积木视图，第一次打开时载入 starter 积木（和 `microbit/starter.py` 等价）。
- 切到 **Python** 时，如果学生还没改过 Python，会显示积木生成的代码。
- 学生在 Python 里改过之后，积木的改动不会再覆盖它，只有点 **Use my blocks' code** 才会替换。
- 积木（Blockly 的 JSON）、Python 草稿和当前视图都存在浏览器的 localStorage 里，读写都包了 try/catch。
- **Run my code**（模拟器）、**Send to micro:bit** 和 **Download main.py** 用的都是当前视图的代码。

## 发送到 micro:bit

USB 模式下代码区有 **Send to micro:bit** 按钮（模拟器模式里不显示，那里用 **Run my code**）。它通过已经连着的串口，把当前代码写成板子上的 `main.py`，然后重启板子，大约 2 到 4 秒。不需要离开页面，也不用断开连接。

- **原理**：和 Mu、microfs 一样走 MicroPython 的 REPL。先发 Ctrl-C 停掉正在跑的程序，再发 Ctrl-A 进原始 REPL，用 `f.write(b'...')` 每次写 64 字节，然后在板子上算文件长度和字节和来核对，最后 `microbit.reset()`。串口每次只写 32 字节、间隔 10 毫秒，照顾 V1 很小的串口缓冲区。
- **前提**：板子上已经有 MicroPython。新板子、或者刷过 MakeCode 程序的板子，Ctrl-C 没有反应，按钮会提示"走一次老路"：点 Download main.py，到 python.microbit.org 刷一次，之后就能直接 Send。**所以上课前最好把班上所有板子都用 python.microbit.org 刷一次 starter。**
- **发送期间**：游戏发给这块板的消息（crash、gift 等）会被丢掉，不然会被打进 REPL；监视器不显示 REPL 的输出，也不报"板子没数据"；板卡上显示进度。
- **出错时**：拔线、板子没空间、板子不回话，都有对应提示，然后退出原始 REPL、软重启板子。如果已经写了一半，提示里会说旧代码被覆盖了，要重新发。
- **多块板**（合作模式）：每块板的卡片上会出现 **Send my code here** 按钮，发给哪块点哪块。
- 在 Python 视图里按 **Ctrl+Enter**：USB 模式下是发送，模拟器模式下是运行。

## 给新游戏写插件

照着 `code/game-cityscooter.js` 的结构写一个新文件，调用 `MicrobitBlocks.registerPack({...})`：

| 字段 | 作用 |
|---|---|
| `id`、`name`、`colour` | 插件名和积木颜色 |
| `blocks` | Blockly JSON 积木定义；`style: "game_blocks"` 会用上 `colour` |
| `generators` | `{ 积木类型: (block, generator) => 代码 }`。帽子积木调用 `MicrobitBlocks.addSection(key, MicrobitBlocks.statements(block, "DO"))`，然后返回 `""` |
| `isHat(type)` | 哪些积木是这个插件的帽子积木 |
| `reserved` | 插件生成代码里用到的名字，用逗号分隔 |
| `globals(ctx)`、`helpers(ctx)` | 放在 `on start` 前面的全局变量和辅助函数（字符串数组） |
| `loopTail(ctx, section)` | 每次循环末尾要执行的代码（不带缩进）；`section(key)` 取帽子积木存进来的内容，`ctx.flags` 是生成器设置的标记 |
| `radioEach(ctx)` | 可选（11/9）：收到的每条无线消息（变量 `got_radio`）都要执行的代码（不带缩进）。返回非空时，主循环每轮把无线队列读空。CityScooter 用它把消息转给游戏（`rx:`）。生成器里用 `MicrobitBlocks.useRadio(g, 队列长度)` 打开无线 |
| `toolbox(blk, num)` | 返回工具箱分类，排在通用分类前面 |
| `starter()` | 默认积木（Blockly 序列化 JSON） |

然后在 `index.html` 的 `boot(...)` 参数里，用 `codePacks: ["code/你的插件.js"]` 换掉 CityScooter 的插件。

## 测试

每次改积木或外壳，都要跑下面几套无头浏览器测试，脚本在 `tools/shell-tests/`（用法见那里的 `README.md`）：

1. **积木测试**（`tools/shell-dev` 开发页 + 真的官方模拟器）：
   - starter 积木生成的关键代码行；
   - 用 CPython 做语法检查；
   - 在模拟器里运行：倾斜转向、A 跳、报 `hi:v2`；
   - 用上全部积木的程序：在 V2 上和模拟 V1 的情况下都运行，并收发 crash/gift/go/spd，要求没有任何 Python 报错；
   - 场景（v0.3）：全部积木的程序里有"当进入 Kings Park 时"和"当游戏说 zone:quay"，收到 `zone:uwa`、`zone:kingspark` 时只有 Kings Park 那段运行一次；
   - 无线竞速（11/9）：
     - 工具箱 CityScooter 分类最后是 Race 标签和 4 块竞速积木，顺序不变；
     - 第一课 starter 加三块竞速积木（名字填 `amy!`）和"when shaken → start a race from Kings Park for 1500 m"，生成的关键代码逐行核对，"on start" 里只设变量；
     - 在模拟器的 V2 和模拟 V1 上运行，打开机器人对手：`race:AMY:12` 到了游戏，三个机器人的 `rx:p:BOT1…` 每秒约 4 条到了游戏，监视器每秒一行汇总、没有逐行的 `rx:`；
     - 游戏发 `pos:` / `fin:` 后模拟器的 `radio_output` 是 `p:AMY:…` / `f:AMY:72.35`（带 3 字节文字头）；监视器里 `pos:` 每秒最多一行；
     - Send go 后游戏收到 `rx:go:<种子>:uwa:2000`；摇一摇时发出 5 次同一个种子的 `go:…:kingspark:1500`，自己的游戏也收到一次 `rx:go:…`；
     - 全部积木的程序里也加了竞速积木（和 Radio 分类的积木一起用）；
   - 引脚 0 加声音时会出警告；
   - 积木和 Python 的切换规则；
   - 分隔条能拖动；
   - 刷新后积木、视图和模式都能恢复；
   - 页面不向外网发任何请求。
2. **外壳测试**：USB 模式（用模拟的 Web Serial）、模拟器模式、串口监视器、帮助面板；监视器里 `zone:` 的说明（v0.3）；无线竞速（11/9）的 `race:`（只记一次，格式不对有提示）、`rx:` 每秒一行汇总、`go:` 和 `f:` 各记一次、游戏发的 `pos:` / `fin:` 照常写进板子但每秒最多显示一行、板子自己打印 `pos:` / `p:` 时的提示。
5. **竞速程序测试**（`race.test.js`，11/9）：
   - `microbit/race.py`、`microbit/race_start.py` 用 CPython 检查语法（`python -I`，不写任何文件）；
   - 先验证模拟器的无线格式：`radio_input` 带 `\x01\x00\x01` 头才是文字，不带头 `radio.receive()` 报 ValueError，超过 29 个字符会被截断；
   - `race.py` 在 V2 和模拟 V1 上跑：机器人对手的 `rx:p:` 到了游戏，`pos:` / `fin:` 变成无线的 `p:AMY:…` / `f:AMY:…`；
   - `race_start.py`：开机在 11 组，B 换到 12 组，A 倒数 3 秒后发 5 次同一个 `go:…:uwa:2000`，A+B 依次在 11～15 组各发 5 次（同一个种子）再回到 12 组，B 在 15 组之后是决赛 20 组。
3. **真实网页包测试**：用真的 Unity 网页包，USB 和模拟器两条路都能开一局，游戏消息能发回设备。
4. **发送测试**（`send.test.js`）：页面的 Web Serial 接到用 CPython 模拟的 MicroPython 板子（`fake_repl_board.py`，原始 REPL 的每条命令都真的执行）：
   - 积木代码原样写进板子，板子重启后跑的是新程序；
   - 串口单次写入不超过 32 字节；
   - 发送期间游戏消息不会打进 REPL，监视器里没有 REPL 杂讯；
   - Python 视图里的引号、反斜杠、中文和 emoji、CRLF 都能逐字节写对；
   - 没有 MicroPython 的板子、空间满、发送中拔线，都给出对应提示；
   - 两块板时按板发送，模拟器模式里没有这个按钮。
