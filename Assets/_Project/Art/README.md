# CityScooter 美术资源说明（Assets/_Project/Art）

本目录放第三方模型、贴图和材质。所有 prefab 都由编辑器脚本生成，脚本在 `Assets/_Project/Scripts/Editor/Art/`（编进 `CityScooter.Editor` 程序集）。生成规则只有一份，写在代码里，所以任何人重跑工具都会得到同样的结果。

## 1. 已导入的资源（Kenney，CC0）

下载日期 2026-11-05。下载地址：资源页 `https://kenney.nl/assets/<名字>` →「Continue without donating」。现在网站发的是带签名、会过期的 `kenney.nl/download/...` 链接，已经不是 `kenney.nl/media/...`。每个 zip 都在仓库外解压检查过，里面的 `License.txt` 都写着 **Creative Commons Zero (CC0 1.0)**，可以商用、教学使用，不要求署名。建议在制作人员名单里写一句「3D models: Kenney (www.kenney.nl), CC0」。

仓库里每个 Kit 只保留 FBX、`Textures/colormap.png` 和 `License.txt`。OBJ/GLB、预览图和 html/url 快捷方式都没有提交。

| Kit（文件夹） | 版本 | 选用的模型 | 导入缩放（Kenney 单位 → 米） |
|---|---|---|---|
| `CityKitRoads` | 2.2 | road-straight, road-crossing, tile-low, light-square, light-square-double, light-curved, traffic-light, road-sign-street, road-sign-warning, construction-cone, construction-barrier, construction-fence, construction-light, dumpster（14 个） | 默认 ×10（一块路面 10 m，正好 8 m 沥青 = `halfWidth` 4）；cone/barrier/light ×8，fence/dumpster ×6，路牌 ×6.5 |
| `CityKitCommercial` | 2.1 | building-a/c/e/f/g/h/k, building-skyscraper-a/e, low-detail-building-a/f/wide-a, detail-parasol-a（13 个） | ×10（3–4 层商铺约 9–17 m 高）；遮阳伞 ×6 |
| `CityKitSuburban` | 2.0 | building-type-a/c/h/k/p/t, tree-large, tree-small, planter, fence（10 个） | ×8（独立屋 6–9 m 高）；planter ×6，fence ×7 |
| `CarKit` | 3.1 | sedan, taxi, van, suv, hatchback-sports, police, box（7 个） | ×1.3（卡通车约宽 2.0 m、高 1.7 m）；box ×1.1 |
| `HolidayKit` | 2.0 | present-a/b-cube/rectangle/round（6 个礼物）, bench, bench-short | ×1.3（礼物 0.6–0.85 m，长椅 1.46 m） |

一共 52 个 FBX、5 张 colormap、5 个材质，二进制文件大约 2.6 MB，走 Git LFS。

## 2. 目录规则

```
Art/ThirdParty/<Vendor>/<Kit>/Models/*.fbx         原始模型（只放 FBX）
Art/ThirdParty/<Vendor>/<Kit>/Textures/colormap.png 该 Kit 的调色板贴图
Art/ThirdParty/<Vendor>/<Kit>/Materials/<Kit>_Colormap.mat  该 Kit 唯一的材质（工具创建）
Art/ThirdParty/<Vendor>/<Kit>/License.txt          原样保留的许可证
Art/Generated/Meshes/                               工具生成的小网格（草坪用的 PaletteQuad_Lawn）
Prefabs/Kit/<Kit>/<模型名>.prefab                   每个模型一个包装 prefab（生成）
Prefabs/Obstacles/Obstacle_*.prefab                 玩法用的障碍物（生成）
Prefabs/Pickups/Gift_*.prefab                       礼物（生成）
Prefabs/Road/RoadChunk_Street_A/B/C.prefab          示例路段（生成）
```

- 上面所有 **生成的 prefab 每次重跑工具都会被覆盖**，不要手改。需要改的话，改 `KitRules.cs`、`KitPrefabBuilder.cs` 或 `StreetChunkBuilder.cs` 再重跑；也可以做一个 Prefab Variant，或者手工搭一个不同名字的路段（比如 `RoadChunk_Street_Hand_01.prefab`）。
- 工具是「原地编辑」prefab，子物体按名字复用，所以 GUID 和 fileID 都不会变。场景和脚本里的引用不会断，没有改动时重跑也不会产生 git diff（已经验证过）。
- 新素材必须和 Unity 生成的 `.meta` 一起提交，不要手写 `.meta`。png/fbx 走 LFS（见仓库根目录 `.gitattributes`），提交前用 `git lfs ls-files` 检查一下。

## 3. 菜单和批处理命令

| 菜单 | 作用 |
|---|---|
| `CityScooter/Art/Rebuild All (Kits + Chunks + Validate)` | 下面三步按顺序全跑一遍 |
| `CityScooter/Art/Rebuild Kit Prefabs` | 建 Kit 材质 → 把 FBX 的材质重映射到 Kit 材质（导入设置和规则不一致时重新导入）→ 生成 `Prefabs/Kit`、`Pickups`、`Obstacles` |
| `CityScooter/Art/Rebuild Street Chunks` | 生成 3 个示例路段 |
| `CityScooter/Art/Validate Budget` | 检查预算和数据约定，每条结果是一行 `[CityScooter][Budget]` 日志，最后一行是汇总 |
| `CityScooter/Art/Render Chunk Previews` | 把每个路段渲染成 PNG（骑手视角、鸟瞰、俯视，每个路段连放 3 段好看接缝；另外有 A-B-C-A 连接图和障碍物/礼物排队图），输出到 `Logs/ChunkPreviews`（不进 git） |

批处理（项目路径换成自己的）：

```
Unity.exe -batchmode -nographics -quit -projectPath <项目> -executeMethod CityScooter.EditorTools.Art.ArtPipeline.RebuildAllBatch -logFile <log>
Unity.exe -batchmode -nographics -quit -projectPath <项目> -executeMethod CityScooter.EditorTools.Art.KitPrefabBuilder.RebuildBatch -logFile <log>
Unity.exe -batchmode -nographics -quit -projectPath <项目> -executeMethod CityScooter.EditorTools.Art.StreetChunkBuilder.RebuildBatch -logFile <log>
Unity.exe -batchmode -nographics -quit -projectPath <项目> -executeMethod CityScooter.EditorTools.Art.ArtBudgetValidator.ValidateBatch -logFile <log>
Unity.exe -batchmode -quit -projectPath <项目> -executeMethod CityScooter.EditorTools.Art.ChunkPreviewRenderer.RenderBatch -cityscooterPreviewOut <目录> -logFile <log>
```

- 预览渲染要用显卡，**不能加 `-nographics`**。
- 有错误时退出码是 1。看日志就搜 `error CS`、`Exception`、`[CityScooter]`。
- 在编辑器里点 Render Chunk Previews 时，会先问你要不要保存当前场景，渲染完再恢复。它在临时场景里渲染，不会改 `Main.unity`。

## 4. 导入规则（`ThirdPartyArtPostprocessor` + `KitRules`）

`Art/ThirdParty/` 下面的文件每次导入都会被统一设置。**不要在 Inspector 里改导入设置**，会被改回去。要改就编辑 `KitRules.cs`，然后跑 Rebuild Kit Prefabs。

- 模型：缩放按 `KitRules` 表（Kit 默认值加单个模型覆盖），Bake Axis Conversion 打开（根节点没有 -90° 旋转），不导入相机、灯光和动画，Read/Write 关闭，网格压缩（路面和地砖用 Low，避免接缝，其余用 Medium），不导入切线、不生成 UV2，不自动加 MeshCollider。
- 贴图：最大 1024；Kenney 的 `colormap.png` 是调色板，所以关掉 mipmap，用 Clamp 和 Bilinear，并且压缩。
- 材质：每个 Kit 只有一个 `URP/Simple Lit` 材质（不透明、无高光、无关键字）。5 个 Kit 用的是同一个 shader 变体，SRP Batcher 可以把它们合批。FBX 里的 `colormap` 材质通过 Remap 指向这个材质（记录在 FBX 的 `.meta` 里）。
  - 材质 **只在缺失时创建，已有的不会被覆盖**，look-dev 可以随便调（换 Lit/Toon、调颜色）。
  - 注意 Simple Lit 的坑：`_SpecularHighlights = 1` 表示 **关闭** 高光，0 才是打开。打开后，光滑度 0 的大高光会把调色板颜色洗成灰色。

## 5. 坐标和尺寸约定

和 `RoadChunk.cs` 一致：1 unit = 1 m，+Z 向前，+Y 向上，路面 y = 0，路段的 pivot 在路段起点的道路中线上，长 40 m。

Kit prefab 的统一方式：根节点放碰撞体和组件，子物体 `Model` 是 FBX 实例，旋转和偏移都放在 `Model` 上。

- 路面块：沿 +Z 方向，沥青面在 y = 0，路缘和人行道面在 y = 0.1。BoxCollider 只盖住沥青层（顶面 y = 0），不投影。
- 建筑、车、长椅：正面朝 +Z（汽车原始朝向是 -Z，工具已经转了 180°）。
- 碰撞体：障碍物、车、建筑用贴合包围盒的 BoxCollider。路灯、路牌、树、遮阳伞这类有悬伸的物件，只用 2.5 m 以下部分的占地范围（灯杆、树干）做 BoxCollider，这样灯臂不会伸进车道。
- 障碍物：`Obstacle.canJumpOver` = 高度 < 1.2 m（规则表也可以指定），车永远不能跳过；`spansRoad` 只给横跨整条路的栏杆。
- 礼物：只有一个 trigger SphereCollider（半径不小于 0.75 m，方便学生用倾斜控制去捡），不加实体碰撞体。

Street 路段统一横截面（所有 Street 路段在 z = 0 和 z = 40 的边缘必须一致，才能任意拼接）：

| x（绝对值） | 内容 | 高度 |
|---|---|---|
| 0 – 4 | 沥青车道（`halfWidth` = 4，骑手只在这里） | 0 |
| 4 – 5 | 路缘带（路面块自带） | 0.1 |
| 5 – 8 | 人行道（tile-low 拉伸） | 0.1 |
| 8 – 40 | 地块：铺装（tile-low）或草坪（PaletteQuad_Lawn） | 0.1 |
| ≥ 8.5 | 商铺正面；独立屋正面在 12；背景楼在 21 | 0.1 |

|x| < 5 以内除了路面块什么都不放。Validate Budget 会检查车道内 0.15–3 m 高的空间里有没有任何网格三角形或碰撞体，也会比较每个路段两端的横截面（接缝检查）。

## 6. 示例路段

| Prefab | 内容 | 生成槽位 SpawnSlot | minDifficulty |
|---|---|---|---|
| `RoadChunk_Street_A` | 城区：两侧商铺、后排高楼、路灯、咖啡伞、花槽、行道树 | 每 8 m 一排（z = 4/12/20/28/36），每排 3–4 个，Any/Obstacle/Gift 混合 | 0 |
| `RoadChunk_Street_B` | 珀斯郊区：草坪、退后 12 m 的独立屋、行道树、侧院围栏、弯头路灯 | 同上，z = 20 那排有 5 个 | 0 |
| `RoadChunk_Street_C` | 左侧商铺、右侧住宅，z = 10–20 有斑马线和红绿灯，z = 25–31 是施工区 | z = 28 有 1 个 **JumpObstacle**（放 `Obstacle_RailingSpan`），z = 34 一排 3 个礼物当奖励 | 1 |

槽位横向位置在 ±3 m 以内，障碍物不会压到路缘。每排都没有把整排写死成 Obstacle，不过 **玩法那边生成时仍然要保证每排至少留一条通路**（Any 槽位也可能刷出障碍物）。

障碍物（`Prefabs/Obstacles`）：Cone（锥桶，可跳）、Barrier（水泥隔离墩，横放，宽 1.8 m，可跳）、Bin（垃圾箱，1.26 m，不可跳）、Bench（长椅，可跳）、Box（纸箱，可跳）、Car（轿车，不可跳）、RailingSpan（横跨道路的 9 m 栏杆，可跳，`spansRoad`）。

## 7. 预算（Validate Budget 会检查）

| 项目 | 限制 | 当前 |
|---|---|---|
| 贴图尺寸 | ≤ 2048 px（第三方贴图导入时限制在 1024） | 5 张 512×512 |
| 网格 Read/Write | 必须关闭 | 全部关闭 |
| 单个 prefab 三角面 | 道具 ≤ 5k，建筑 ≤ 15k | 道具最大 suv 2474，建筑最大 building-k 2960 |
| 路段（建议值） | ≤ 60k 三角面，≤ 80 个 renderer | A 15.0k/28，B 9.2k/31，C 13.7k/30 |
| 材质数量 | ≤ 8 | 5（每个 Kit 一个） |
| 障碍物/礼物碰撞体 | Obstacle 至少一个非 trigger 碰撞体；Gift 根节点要有 trigger SphereCollider | 全部通过 |

## 8. 添加新的 Kit

1. 下载 zip，在仓库外新建一个空文件夹解压，确认 `License.txt` 是 CC0 或其他允许的许可证。不要运行压缩包里的任何东西。
2. 只复制需要的 FBX 到 `Art/ThirdParty/<Vendor>/<Kit>/Models/`，调色板贴图复制到 `.../Textures/colormap.png`，再复制 `License.txt`。
3. 在 `KitRules.cs` 的 `Kits` 表里加一项：默认缩放、默认类别（Building/Prop/Obstacle/…），需要的话再给单个模型加覆盖（缩放、类别、`CanJumpOver`、`Yaw` 让正面朝 +Z）。量尺寸可以先导入，再看 Validate 和预览图。
4. 跑 `Rebuild Kit Prefabs` 和 `Validate Budget`，再跑 `Render Chunk Previews` 看朝向和比例。
5. 把 FBX、png、`License.txt`、`.mat` 和所有 `.meta` 一起提交，然后检查 `git lfs ls-files`。

## 9. 添加新的路段

1. 在 `StreetChunkBuilder.cs` 里照着 `LayoutA/B/C` 写一个新的 `LayoutX`，然后在 `ChunkNames` 和 `Rebuild()` 里注册。也可以在编辑器里手工搭一个新名字的 prefab。
2. 必须保持第 5 节的横截面，|x| < 5 内不放东西。建筑沿 z 方向排列，不能超出 0–40。
3. SpawnSlot 放在 y = 0，|x| ≤ 3，大约每 8 m 一排。JumpObstacle 放在 x = 0，同一排不要放别的槽位。
4. 跑 Validate Budget（接缝、车道、槽位都会检查），再跑预览图确认。

## 10. 待技术美术确认

- 缩放是按真实尺寸估的（卡通 Kit 本来就不写实），尤其是车（×1.3）和商铺（×10，单层偏高），请看 `Lineup_*.png` 和各路段预览图后调整 `KitRules`。
- 红绿灯（`traffic-light`）灯面朝向还没有确认，现在左侧 yaw 0、右侧 yaw 180。
- 颜色：路面和人行道是 Kenney 的淡紫灰色调，草坪是调色板里的绿色，交给 look-dev 统一处理（材质不会被工具覆盖）。
- Kenney 没有电动滑板车、塑料水马、短护柱（bollard）这几种模型。现在分别用轿车、水泥隔离墩、施工警示灯代替，以后可以换专门的模型。
