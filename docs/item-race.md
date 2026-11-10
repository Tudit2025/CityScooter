# 道具赛（无线竞速 v2）

写于 2026-11-10（周二）。老师在第二节课后提出：无线竞速没问题，想加积木让学生搞道具赛，像马里奥赛车那样。同一天还发现：比赛模式里 "on start" 放的 "set the wanted stars" 不起作用（v1 的比赛模式故意不理所有 `do:`）。这一版一起改。

基础是 `docs/radio-race.md`（v1）。这里只写新增的部分。两个实现智能体（游戏内、网页和积木）都按这份做；有矛盾以这份为准，改动要回写到这里和 `docs/protocol.md`。

## 玩法

- 比赛路上每隔一段有一排**道具箱**（同一场比赛每台电脑上位置一样）。手里没有道具时骑过去，拿到一个道具，HUD 上显示。
- 道具**不会自己用**。学生的程序发 `use` 才用掉：这就是这一课要写的代码，例如"按下 B 时 → use my item"，或者"拿到 rocket 就马上用"。
- 攻击别人的道具通过无线发给同组的人。被打中的那台电脑自己算效果。
- 每条无线消息还是会丢，所以同一条发 3 次，带一个序号，收的人按（谁发的、序号）去重。

## 道具

| 名字 | 像马里奥赛车里的 | 效果 | 目标 |
|---|---|---|---|
| `boost` | 蘑菇 | 3 秒强力加速（比 `bo` 更快，不用等冷却） | 自己 |
| `shield` | 星星 | 8 秒护盾：挡掉下一次道具攻击或下一次撞车，挡一次就没了 | 自己 |
| `oil` | 香蕉皮 | 在自己身后 6 米、自己这条车道上留一滩油。谁骑上去就打滑：速度降到 40%，持续 1.5 秒，不算撞车。每滩油每台电脑上只滑一次，60 秒后消失 | 后面的人 |
| `rocket` | 红龟壳 | 打你前面最近的那个人：对方屏幕上先有 1 秒警告，然后摔车停 1.5 秒 | 前面一个人 |
| `bolt` | 闪电 | 除了你，同组所有人减速到 50%，持续 3 秒 | 其他所有人 |
| `police` | 蓝龟壳 | 第一名（如果你自己是第一名，就是第二名）被通缉 3 星，持续 15 秒，警车来追。在比赛里被抓到 = 罚停 3 秒，然后警察撤走 | 领先的人 |

拿到什么看当时的名次（落后的人拿到更厉害的）：

| 名次 | 概率 |
|---|---|
| 第一名 | oil 50%，shield 30%，boost 20% |
| 中间 | rocket 35%，boost 25%，oil 20%，shield 20% |
| 最后一名 | rocket 30%，police 25%，boost 25%，bolt 20% |

只有两个人时，第二名按"最后一名"算。一个人（大厅里练习、没有对手）按"中间"算，这样练习时什么都能拿到；`rocket`、`police` 没有目标时用掉也没有效果，HUD 提示 "no one ahead"。

道具箱：比赛起点后 150 米放第一排，之后每 250 米一排，一排 3 个，分在 3 条车道上。位置只由距离决定，所以每台电脑上一样。每排每个人只能拿一次。大厅练习时也有道具箱。普通模式（不比赛）没有。

## 消息

### USB（游戏和板子之间）

| 方向 | 消息 | 意思 |
|---|---|---|
| 游戏 → 板子 | `item:rocket` | 你拿到了一个道具。用掉或者丢了以后发 `item:none` |
| 板子 → 游戏 | `use` | 用掉手里的道具。手里没有就不理（监视器提示一次） |
| 游戏 → 板子 | `tx:<无线消息>` | 请把冒号后面的内容原样用无线发出去（`radio.send`）。和 `rx:` 正好相反。同一条道具消息游戏会给 3 次，间隔 0.15 秒 |
| 游戏 → 板子 | `hit:rocket` | 你被这个道具打中了（`oil` 是骑上去的时候才发） |
| 游戏 → 板子 | `blocked:rocket` | 你的护盾挡掉了它 |

`tx:` 由"send my position by radio"那块积木（和 `race.py`）一起转发，不用新积木：

```python
elif send_position and msg.startswith("tx:"):
    radio.send(msg[3:])
```

旧程序（11/9 刷的）不认识 `tx:`：能被打中，但自己的攻击发不出去。重新发一次代码就好。

### 无线（板子之间）

| 消息 | 意思 |
|---|---|
| `i:AMY:rocket:BEN:3` | AMY 的 rocket 打 BEN。最后的 3 是 AMY 的道具序号（0～9 循环） |
| `i:AMY:police:BEN:4` | AMY 让警察去追 BEN |
| `i:AMY:bolt:5` | AMY 的闪电，除 AMY 外所有人减速 |
| `i:AMY:oil:1234:-2:6` | AMY 在 1234 米、-2 米车道上留了一滩油 |

- 最长 22 个字符，在 29 个字符的上限以内。
- 收的人：`rx:i:…`。（发的人，序号）最近 20 秒内见过就不理。发的人是自己的名字也不理。
- 目标不是自己的 `rocket` / `police`：只在画面上给个提示（"AMY → BEN: ROCKET!"），不产生效果。
- 格式不对就丢掉，不报错。监视器把 `i:` 算进无线统计，并单独记一行 "radio: AMY used rocket on BEN"。

## 比赛模式里的 `do:` / `open:`

v1 里比赛模式不理所有 `do:`。现在分两类：

- **只影响自己的警察事件照常执行**：`chase`、`stars:N`、`heli`、`calm`。所以 "on start → set the wanted stars to 5" 在比赛里也有效，等于给自己加难度。大厅练习时也有效。
- **会改路的事件仍然不理**：`turn`、`car`、`slowcar`、`roadworks`、`police`（拦路）、`crossing`、`manhole`、`ramp`。大家的路必须一样。屏幕上提示 "not in race mode"。
- 比赛里有警察以后：被抓（`arrested`）、被撞或被狙（`busted`）都不结束比赛，和撞车一样罚停 3 秒，然后通缉清零。照常发 `arrested` / `busted` / `wanted:0` 给板子。
- `open:`（每一局开头的事件，11/9 加的）在比赛里同样处理：警察类的在每场比赛开始时执行。

## 游戏内（Unity）

- 道具箱：漫画风格的彩色问号箱，慢慢转。新的 `ItemBox` 组件和预制体由代码生成（`Scripts/Editor/Gameplay/` 下），不要动 `Main.unity`，不要动技美的目录（`Art/Shaders`、`Runtime/Rendering`、`Editor/Art`）。
- `RaceItems`（新组件，由 `RaceDirector` 或 `GameSystemsFactory` 在运行时加上）：手里的道具、使用、收到的攻击、油、护盾、去重。`RaceDirector` 已经知道每个人的名次和距离。
- HUD（`HudView.Race.cs`）：右下角一个道具格，显示手里的道具名字和图标；被打中、护盾挡掉、别人之间的攻击都有一行大字提示。
- 效果要看得见：加速有尾焰或速度线，护盾是一个半透明的罩子，油是地上一滩黑色，rocket 打中有 "BOOM!"，bolt 全屏闪一下，police 用现有的警车。
- 幽灵车不用画它们的道具状态。
- 键盘（开发用，课上的版本键盘是关的）：`E` = `use`。
- 测试：EditMode 测消息的解析和生成；PlayMode 用假的无线总线（`rx:` 直接喂给 `DeviceHub`）测每个道具的效果、去重、护盾、名次决定的概率表（用固定随机种子）、比赛里的 `do:stars` 和被抓罚停。截图放 `art-src/race/previews/items-*.png`。

## 积木（CityScooter → Race 下面加一个 "Items" 标签）

| 积木 | 类型 | 生成的代码 |
|---|---|---|
| "when I get an item" | 帽子 | 主循环里 `msg.startswith("item:")` 且不是 `item:none` 时执行。先更新 `my_item` |
| "my item" | 值（文字） | `my_item`，没有时是 `"none"` |
| "my item is [rocket ▾]" | 值（真假） | `(my_item == "rocket")`，下拉框里是 6 个道具和 none |
| "use my item" | 语句 | `print("use")` |
| "when an item hits me" | 帽子 | 收到 `hit:…` 时执行，先更新 `hit_item` |
| "item that hit me" | 值（文字） | `hit_item` |

- `my_item` 在收到任何 `item:` 时都更新（包括 `item:none`），所以"my item"随时是对的。只在用到这些积木时才生成这些变量（和 `wanted_level` 的做法一样）。
- "send my position by radio" 的提示文字加一句：也负责把你扔出去的道具发给大家。
- 提示文字里写清楚：道具不会自己用，要你的程序说 use。

例子（课件里用）：

```
when I get an item        → show icon [happy]
when button B is pressed  → use my item
when an item hits me      → show icon [sad]
```

进阶：`if my item is [shield] … else use my item`（护盾留着，别的马上用）；`when the game says [warn:car] → if my item is [boost] → use my item`。

## 程序

- `microbit/race.py`：加上 `tx:` 转发和一个最简单的用法（B 键 = `use`，如果 B 在这个程序里已经有别的用途，就用 A+B）。注意 V1 内存，别加太多。
- `microbit/race_start.py` 不用改。

## 模拟器（外壳）

- 机器人对手开着的时候：
  - 每个机器人每 20～40 秒随机对"你"用一次 `rocket` 或 `bolt`（往 `radio_input` 喂 `i:BOT1:rocket:<你的名字>:<序号>`，喂 3 次），这样没有板子的同学也能测"when an item hits me"。
  - 模拟器程序发出的无线（`radio_output`）里有 `i:` 时，监视器记一行 "radio: you used rocket on BOT2"。
- 监视器认识 `item:`、`hit:`、`blocked:`、`tx:`（游戏发的）和 `use`（板子发的）；学生自己 `print("item:rocket")` 之类的，照常显示并提示这是游戏发的消息。

## 进度

（实现的人在这里写。）
