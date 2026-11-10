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

### 游戏内（Unity），11/10 晚

游戏内这部分做完了，在分支 `worktree-agent-a92107786fa672e55` 上，没有合并。网页、积木、`race.py` 和 `docs/protocol.md` 由另一边做。

**做了什么**

- 协议（`Scripts/Protocol/`）：`use`、`item:`、`tx:`、`hit:`、`blocked:`，以及 `rx:` 里面的 `i:…`（`RadioMessage` 多了一种 `Item`）。格式和上面的表一样，没有改。
- `RaceItems`（新组件，`GameController` 在运行时加上）：道具箱、手里的道具、使用、收到的攻击、去重、油、护盾、警察的 15 秒。只在比赛模式里工作。
- `ItemBox`：一个问号箱，只负责转和上下浮动。位置由 `RaceItems` 按距离算。
- `RaceItemArt`：道具箱、油、火箭、护盾、尾焰、闪电和 BOOM! 这些漫画字，都是运行时用代码生成的网格，用车手身上的漫画材质。
- HUD（`HudView.Items.cs`）：右下角的道具格、旁边的状态（SHIELD 6、BOOST、SLOW 2、POLICE 12、ROCKET!）、闪电的全屏闪光、加速时的速度线。一行大字用原来的 `Flash`，加了颜色。
- `RaceDirector`：罚停时间可以不是 3 秒（rocket 是 1.5 秒）；冲线时警察撤走。
- `PoliceChase`、`ProgramTriggers`：比赛模式里的新规则（见下面）。
- `RiderController`：道具加速、道具减速、护盾挡撞车的入口。

**发给板子的消息，例子**

| 什么时候 | 消息 |
|---|---|
| 骑过道具箱 | `item:rocket` |
| 用掉，或者这一局结束时手里还有 | `item:none` |
| 用 rocket，前面最近的是 BEN | `tx:i:AMY:rocket:BEN:3`，发 3 次，隔 0.15 秒 |
| 用 police，领先的是 CHLOE | `tx:i:AMY:police:CHLOE:4`，同上 |
| 用 bolt | `tx:i:AMY:bolt:5`，同上 |
| 用 oil，自己在 1240 米、车道 -2 | `tx:i:AMY:oil:1234:-2:6`，同上 |
| 被打中 | `hit:rocket`、`hit:bolt`、`hit:police`、`hit:oil` |
| 护盾挡掉 | `blocked:rocket`、`blocked:bolt`、`blocked:police`、`blocked:oil`、`blocked:crash` |

用 boost 和 shield 只发 `item:none`，不发 `tx:`。

**规格里没写清楚、这里定下来的**

1. **不生成预制体。** 规格写的是"预制体由 `Scripts/Editor/Gameplay/` 下的代码生成"。实际做成运行时生成网格，不新增资源，也不改主题资源和 `Main.unity`。这样不会和东线的改动冲突。护盾的半透明罩子用 Unity 自带的 `Sprites/Default` 着色器（在 Always Included 里）。
2. **道具箱的位置。** 三个箱子在 x = -2.6、0、2.6 米，4 车道的路上也一样。离终点不到 40 米就不放了。骑过那一排时左右离箱子 1.1 米以内就算拿到，不看高度：有的排正好在跳台的位置，飞过去也算。
3. **箱子可能和路上的东西重叠。** 位置只看距离，不看那里有没有车或路障。同一场比赛每台电脑上都一样。
4. **大厅练习**按"中间"那张表拿道具，不看别人在哪。大厅里 rocket 和 police 没有目标。bolt 和 oil 在大厅里也会发出去，别人练习时也会被打中，方便不开比赛就测 "when an item hits me"。
5. **`use` 时手里没有道具**：游戏什么都不做，也不发消息。
6. **rocket**：`hit:rocket` 在 1 秒警告结束、真的打中时才发。警告的这 1 秒里用 shield 可以挡掉。被打中不发 `crash`，不算撞车次数。正在罚停的人打不中，也不发 `hit:`。
7. **shield 挡撞车**：只挡撞到东西（包括被警车撞），发 `blocked:crash`，然后 1 秒内可以穿过那个东西。掉进缺口、被捕、被狙不挡。
8. **oil**：滑过一次（或者护盾挡掉）以后，这滩油就从自己的屏幕上消失；没人碰的 60 秒后消失。自己放的油自己不会滑。车道取整数，和无线消息里一样。油里画了一个倒着的油桶，远处才看得见。
9. **police 道具**：3 星，15 秒后回到被打中之前的星级（一般是 0）。这期间被抓或者撞车，就提前结束。3 星会来直升机撒网。
10. **比赛里的任何一次罚停都把通缉清零**，不只是被抓和被撞，普通撞车也算。消息顺序：被抓是 `wanted:0`、`chase:0`、`crash`、`arrested`；被警车撞是 `crash`、`wanted:0`、`chase:0`、`busted`。`arrested` / `busted` 仍然是最后一条。
11. **比赛里打警察也会加星**（v1 的比赛模式里不加）。
12. **冲线以后**警察撤走，手里的道具清掉（发 `item:none`）。之后的道具攻击和警察类 `do:` 都不起作用。
13. **`open:` 里的改路事件**在比赛模式里也提示 "not in race mode"，每种事件 8 秒最多提示一次（它每秒都来）。
14. **键盘 `E`**：手里有道具时是 `use`，没有时还是加速。
15. **序号**从 0 开始，0～9 循环。刷新网页后又从 0 开始，20 秒内可能和上一条重号，被当成重复丢掉，概率很小。
16. **减速结束后**的 1.5 秒里加速度是平时的 4 倍，不然 oil 比 rocket 还狠。
17. **被 police 道具打中时**，屏幕上显示谁派的警察，盖掉了 "POLICE HELICOPTER!" 那行提示。

**测试**

- 编辑器测试 292 个全过，新增 `ItemProtocolTests`（37 个）：`use`、五种游戏发的消息、`i:` 的四种格式、坏格式丢掉、长度不超过 22 个字符。
- PlayMode 新增 `RaceItemTests`（19 个），用假的无线：把别人的 `rx:i:…` 直接喂给 `DeviceHub`，从 `DeviceHub.LineSent` 读游戏发出的 `tx:`。
  - 道具箱：同一个 `go:` 两次，路和箱子都一样；关掉道具再跑一次，路上的障碍物和礼物一个不差。
  - 六个道具各自的效果，去重（20 秒），护盾挡道具和挡撞车，按名次的概率表（固定种子）。
  - 比赛里的 `do:stars`，被抓罚停 3 秒，`open:stars` 每场开头执行，冲线以后清空。
- PlayMode 新增 `RaceItemMainSceneTests`（2 个），在真的 Main 场景里：比赛中被警车撞（BUSTED）是罚停 3 秒，不结束；道具的模型用的是真的漫画材质，都没有碰撞体。
- `RaceTests` 里原来断言"比赛里没有警察"的那一条改成了新规则：改路的事件不理，警察类的照常。
- 全部跑一遍：编辑器 292 个，PlayMode 154 个，都通过（另有 21 个截图类测试默认不跑）。

**截图**

`art-src/race/previews/items-01-boxes.png` 到 `items-16-arrested.png`，共 16 张，由 `RaceItemScreenshots`（默认不跑）在真的 Main 场景里生成：道具箱、拿到 rocket、发射、护盾、挡掉、加速、前面的油、打滑、闪电、被闪电减速、rocket 来了、BOOM!、别人之间的攻击、警察来了、警车并排、比赛里被捕。

**还不够好的地方**

- 没在真板子上试过：`tx:` 转发、3 次重发够不够、全班一起用道具时的丢包。
- 没在真的网页包里看过护盾的半透明效果。编辑器里正常。
- 幽灵车不显示被打中，所以打中别人只能看对方的屏幕。
- 道具箱的位置不躲开路上的东西，偶尔会有一个箱子和车或路障重叠。
- 打滑和被闪电减速只是变慢，车手的动作变化不大（打滑时左右晃）。
