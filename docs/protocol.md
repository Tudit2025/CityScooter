# CityScooter 协议 v1

v1（2026-11-07）在 v0 上只**加**消息：板子可以发出拳 `pu`、按铃 `bl`；游戏会发 `ko`、`warn:…`、`turn:…`。v0 的学生代码不用改，不认识的消息会被跳过。

学生写的 micro:bit 程序和游戏之间只说这一种"话"。每条消息一行，ASCII 文本，以换行 `\n` 结尾。
USB 串口波特率 115200。v0 只用于 USB 直连和模拟器；无线（radio）版在第 2 周基于同样的消息再设计。

代码里的定义：`Assets/_Project/Scripts/Protocol/Messages.cs`，解析规则和单元测试见同目录与 `Assets/_Project/Tests/EditMode/`。

## micro:bit → 游戏

| 消息 | 含义 | 学生代码写法（MicroPython） |
|---|---|---|
| `st:-35` | 转向，-100（最左）到 100（最右），超出会被截断；允许小数 | `print("st:" + str(steer))` |
| `jp` | 跳一下（在空中重复发不会多跳） | `print("jp")` |
| `br` | 刹车。每条维持 0.3 秒，按住就每个循环发一次 | `print("br")` |
| `hi:v2` | 报告板子版本（可选，游戏显示用） | `print("hi:v2")` |
| `pu` | **v1** 出拳：把前方左右 2.5 m 内最近的纸箱、垃圾桶、锥桶打飞，打中游戏回 `ko`。冷却 0.4 秒，冷却中发的不算。人、车、路障打不动 | `print("pu")` |
| `bl` | **v1** 按铃：前方 30 m 内的行人让到一边，头上冒"!" | `print("bl")` |

- 推荐循环 `sleep(50)`，也就是每秒 20 次左右。转向超过 0.5 秒没更新，游戏会把转向归零（拔线、程序报错时车不会一直往一边跑）。
- 一帧内收到多个 `jp` 只算一次。轮询写法 `is_pressed()` 会每秒发几十个 `jp`，游戏不会出错，但串口监视器会把这个次数显示出来，第 4 课正好用来讲事件写法和轮询写法。
- **任何不认识的行都不算错误**：学生自己的 `print`、Python 报错（Traceback）都原样显示在串口监视器里。
- 格式宽松：大小写不敏感，冒号两边可以有空格（`ST : 20` 也认）。`st:abc` 这种值不对的会标成"格式不对"，监视器给提示。

## 游戏 → micro:bit

| 消息 | 什么时候发 |
|---|---|
| `go` | 新的一局开始 |
| `crash` | 撞车 |
| `gift` | 捡到礼物 |
| `spd:0` … `spd:9` | 实际速度相对当前速度上限的档位，变化时才发，每秒最多 5 次；上限可随本局进度增长 |
| `ko` | **v1** 出拳打中了 |
| `warn:car` / `warn:ped` / `warn:hole` | **v1** 自己这条路上（左右 1.3 m 内）前方有车、有人、有坑：25 m 或 1.5 秒车程内，取远的那个。同一个东西 8 秒内只发一次，所有警告合起来每秒最多 4 条（V1 的串口缓冲小） |
| `turn:-1` / `turn:1` / `turn:0` | **v1** 路开始往左弯、往右弯、回到直道（假拐弯，路在物理上还是直的；难度 3 级以后拐弯会把人往弯外推一点，要往弯里转向） |

键盘对应：`F` 出拳，`R` 按铃（`B` 在所有提示里都是板子上的刹车键，所以按铃不用 B）。

### 学生代码必须先攒到换行再处理

MicroPython 的 `uart.readline()` 不会等一整行到齐，有多少字节就返回多少。2026-11-05 在模拟器里实测：`crash` 被读成了 `cr` 和 `ash` 两半。真板子出现的概率小一些，但也会有。所以起步代码统一用下面这个函数（已在模拟器里验证）：

```python
buf = ""

def messages():
    # uart.readline() can return half a line, so collect bytes until a newline arrives.
    global buf
    out = []
    if uart.any():
        data = uart.read()
        if data:
            buf += str(data, "utf-8")
            while "\n" in buf:
                line, buf = buf.split("\n", 1)
                out.append(line.strip())
            if len(buf) > 100:
                buf = ""
    return out
```

主循环里写 `for msg in messages(): ...`。另外别忘了 `uart.init(baudrate=115200)`。不带参数的 `uart.init()` 会把波特率改成 9600，游戏就收不到了。

## V1 / V2 兼容

起步代码只用两代都有的功能：`accelerometer`、`button_a/b`、`display`、`Image`、`music`（V1 没有扬声器，不出声但不报错）、`uart`、`sleep`。

只有 V2 有的功能放进加分题：`microphone`（喊叫跳跃）、`pin_logo`（触摸 logo）、`audio`/`Sound` 表情音。判断版本的写法：

```python
try:
    from microbit import microphone
    VER = "v2"
except ImportError:
    VER = "v1"
```

## 以后再加（不在 v1）

护盾 `sh`、加速 `bo`、油门 `th:0..100`、遥测 `pl:名字:数值`（画到游戏里的曲线面板）、路段事件 `zone:tunnel`、无线 radio 版（组号、频段、玩家编号）。
