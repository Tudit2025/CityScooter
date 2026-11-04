# 任务书：GPT Pro #2 — MicroPython 代码 + 第一课教材

> 把这整份文件交给 GPT。它需要能读写 GitHub 仓库 `Tudit2025/CityScooter`，从 `sprint/day1-skeleton` 分支（或者已经合并的 `main`）开始工作。

## 背景

CityScooter 是一款城市滑板车无尽跑酷网页游戏，学生用 micro:bit 当控制器。学生情况：

- WA 学校的 Year 9，以前用过 MakeCode，**这学期学了 MicroPython**，用的是 python.microbit.org。
- 板子 **V1、V2 混用**，数量不够每人一块，有电池盒。学生用 USB 把板子直接连到机房电脑（Windows，Chrome/Edge）。
- 没有板子的学生在游戏页面里嵌入的官方模拟器上跑自己的代码（模拟器是 V2）。
- **第一节课是周一 11 月 9 日**，之后每周两节课。

学生的 MicroPython 程序用 `print()` 把命令发给游戏，再从 `uart` 读游戏发回的事件。

## 先读这些

1. `docs/protocol.md`：消息格式，**必须读**。尤其注意两点：读消息要用缓冲函数（`uart.readline()` 会读到半行）；要写 `uart.init(baudrate=115200)`。
2. `tools/labcheck/labcheck.py`：已经在模拟器里验证过的参考程序。

## 交付物

1. `microbit/starter.py`：第一课起步程序。
2. `microbit/solutions/`：每道任务一个可运行的参考答案，文件名用 `t1_steering.py`、`t2_feedback.py` 这种格式。
3. `microbit/coop/steer.py` 和 `microbit/coop/jump.py`：两块板插在同一台电脑上，一块管转向，一块管跳。
4. 英文教材，放在 `docs/lessons/` 下：
   - `lesson1-worksheet.md`：学生作业单，最多 2 页
   - `lesson1-teacher-notes.md`
   - `troubleshooting-card.md`：一页排错卡
   - `protocol-cheatsheet.md`：半页学生版协议表
5. 把 `microbit/starter.py` 复制一份到 `Assets/WebGLTemplates/CityScooter/TemplateData/starter.py`，模拟器模式会预填这个文件。

## 硬约束

- **核心代码只能用 V1 也有的 API**：
  - `from microbit import *`
  - `accelerometer.get_x()`
  - `button_a/b.was_pressed()` 和 `is_pressed()`
  - `display.show()`、`display.clear()`
  - `Image.SAD/HAPPY/...`
  - `music.play(..., wait=False)`，包括 `music.WAWAWAWAA`、`music.BA_DING`
  - `uart.init/any/read`
  - `sleep()`

  只有 V2 有的功能（`microphone`、`pin_logo`、`audio`/`Sound`）只能出现在加分题里，而且要用 `try/except ImportError` 判断版本。在 V1 上运行时只能打印一句 "This task needs a V2 micro:bit"，不能报错。
- V1 内存很小：不要建大列表，不要在循环里拼很长的字符串。
- 主循环里不要用 `display.scroll()`，它会阻塞好几秒，转向会卡住。这正好是一个讲解点，作业单里要提到。
- 每个循环 `sleep(50)` 左右，每秒打印不要超过 50 行。
- 代码是写给 Year 9 学生读的：注释用英文，简短；变量名要有意义；在学生要动手的地方标 `# TODO (Task N)`。
- **每个文件都要先在 python.microbit.org 的模拟器里跑通。** 另外列一份清单，写明哪些地方需要老师用真 V1 板复核。

## starter.py 要做到

- 开局在 LED 上显示 1 或 2（板子版本），每秒 `print("hi:v1")` 或 `print("hi:v2")` 一次。
- 倾斜转向：`steer = max(-100, min(100, accelerometer.get_x() // 5))`，然后 `print("st:" + str(steer))`。
- A 键用 `was_pressed()` 发 `jp`；按住 B 时每个循环都发一次 `br`。
- 用 protocol.md 里的 `messages()` 缓冲函数读消息：
  - 收到 `crash` 显示哭脸，加声音留给 Task 2
  - 收到 `gift` 显示笑脸
  - 收到 `go` 清屏
  - 收到 `spd:n` 先忽略，留给 Task 4

## 任务（每道都要有参考答案）

| 任务 | 内容 | 讲解点 |
|---|---|---|
| T1 Steering | 调灵敏度（`// 5` 改成别的数）、加死区（`abs(steer) < 10` 时算 0）、方向反了就取负号 | 变量、数值映射、限幅 |
| T2 Feedback | crash 时 `music.play(music.WAWAWAWAA, wait=False)`；gift 时 `music.BA_DING`。V1 不出声，但也不报错 | 事件、输出设备 |
| T3 Buttons | B 刹车；跳跃分别用 `was_pressed()` 和 `is_pressed()` 写一遍，按住 A 时在串口监视器里比较 jp 次数 | 事件和轮询（"remembers a press" vs "only right now"） |
| T4 Speed bar | 解析 `spd:n`（0–9），用嵌套循环按行点亮 LED（例如 n*5//9 行）；哭脸会被柱状图覆盖，让学生自己决定谁优先 | 字符串处理、嵌套循环、共享资源 |
| T5 Shout (V2) | 先用 `print` 观察教室里 `microphone.sound_level()` 的大小，再设阈值，超过就 `jp`，加 1 秒冷却 | 噪声、阈值、冷却、校准 |
| T6 Foil keys | `pin0.is_touched()` 跳，`pin1`、`pin2` 左右转；电阻式触摸要一只手同时摸着 GND；V2 可以用 `pin0.set_touch_mode(pin0.CAPACITIVE)` | 电路、导体 |
| Co-op | `coop/steer.py` 只发 `st:`，`coop/jump.py` 只发 `jp`，两块板插在同一台电脑上一起玩 | 分工、协议 |

## 教材要求（英文，Year 9）

- **Worksheet**：
  - 先写 3 步怎么开始：打开游戏网址（先写 `<GAME_URL>` 占位，周五晚上确定）→ 在 python.microbit.org 里刷 starter → 连接。
  - 任务 T1–T4 带复选框，T5 和 T6 是加分题，标明 "V2 only"。
  - 每题给 1–2 个提示，最后加一道"发明你自己的控制方式"的挑战题。
- **Teacher notes**：
  - 按 60 分钟课写时间安排（课时长度不同，老师自己调）。
  - 课前检查清单：网址、Chrome/Edge、板子、数据线、电池盒、哪些电脑做过机房体检。
  - **板子不够时的分组方案**：两人一组，一个 driver、一个 engineer，10 分钟换一次；多出来的组用模拟器模式。
  - 常见错误和解决办法：NameError、缩进错误、`uart.init()` 忘了写 115200、方向反了、转向卡住（因为用了 scroll）、"port busy"。
  - 和 WA Digital Technologies 的对应：算法里的分支和循环、用户输入、数据表示。只写大类，不要编造课纲代码。
- **Troubleshooting card**：一页，问题 → 怎么办，适合打印出来贴在机房。
- **Protocol cheatsheet**：两张表，学生发什么、游戏发回什么，各附一行代码示例。

## 截止时间和交付

- **周五 11/6 中午**（珀斯时间）前交 `starter.py`、`solutions/` 和 `coop/`。老师周五要在机房用真的 V1、V2 板子测。
- **周六 11/7** 交全部教材。
- 新建分支 `feat/micropython-lesson1`，提 PR；PR 描述里附上模拟器测试记录，以及需要真 V1 复核的清单。
