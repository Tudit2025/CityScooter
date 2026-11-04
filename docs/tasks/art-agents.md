# 技美智能体（由 Claude 在本机派出）

三个智能体给技美打下手。每个都在自己独立的 git 工作区里做，完成后留下一个本地分支。Claude 先检查，再推到 GitHub，然后由技美审核、修改、决定用哪些。
技美有修改意见时，可以直接告诉老师或 Claude，也可以写在对应 PR 的评论里，Claude 会转给对应的智能体继续改。

| 智能体 | 做什么 | 产出位置 |
|---|---|---|
| 天际线 | 珀斯 CBD 剪影分三层（远、中、近）加天空渐变，可以无缝横向拼接；Node 生成器可以调参数；预览页带视差动画。时间够的话再做一套 Rockingham 的。 | `art-src/skyline/`（不进 `Assets/`，由技美自己导入 Unity） |
| 资源管线 | 导入 Kenney CC0 城市和车辆素材；导入规则（1 单位 = 1 米、材料尽量少）；生成带碰撞体的道具和障碍预制体；3 段可以首尾拼接的 Street 路段（带刷怪点，其中一段有必须跳的栏杆）；预算检查菜单 | `Art/ThirdParty/Kenney/`、`Prefabs/{Kit,Road,Obstacles,Pickups}/`、`Scripts/Editor/Art/`、`Art/README.md` |
| 画面与性能 | Web 档 URP 设置（改了什么都记下来）；渐变天空着色器；"珀斯午后"光照和雾；天际线视差架子；撞车烟尘、礼物闪光、速度线、假阴影；低端机自动降画质；LookDev 预览场景；性能预算文档 | `Assets/Settings/`、`Art/{Shaders,Materials,Looks}/`、`Prefabs/{Environment,VFX}/`、`Scripts/Runtime/Rendering/`、`Scenes/LookDev.unity`、`docs/art/` |

共同的约束：

- 不碰 `Main.unity`、协议、设备代码、网页外壳。
- 资源和 Unity 生成的 `.meta` 一起提交，二进制文件走 LFS。
- 地标做成风格化剪影，不放任何商标或文字。
