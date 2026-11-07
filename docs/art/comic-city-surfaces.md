# Comic City 表面材质标准 v1

现有 26 个道具、26 个建筑以及街道、骑手、车辆、围巾已经应用这套材质。底色仍使用现有顶点色；新增周期性明暗变化、粗糙度和法线凹凸，在三档漫画明暗内表现材料差异。

## 查看与调整

- [主场景效果](../../art-src/comic-city/previews/01-gameplay.png)；[14 种表面样板](../../art-src/comic-city/previews/08-surface-library.png)；[修改前主镜头](../../art-src/comic-city/previews/00-before-surface-detail.png)。
- 配置入口：`Assets/_Project/Art/ComicCity/Surfaces/ComicSurfaceLibrary.asset`。在 Inspector 修改材质参数、纹理尺度或资产对应类型，然后执行现有 `CityScooter / Comic City / 1 - Rebuild Kit and Street`，再 Apply Main 与 Render Previews。
- 也可运行 `powershell -NoProfile -ExecutionPolicy Bypass -File tools/run-comic-city.ps1 -Mode Build` 完成生成、应用及预览。
- 每类附带 `MAT_<Surface>.mat` 样板材质。场景合并网格仍共用 `ComicPalette.mat`，通过每个三角形的表面 ID 选择纹理，不需要拆分材质槽。
- [assignments.csv](../../art-src/comic-city/surfaces/assignments.csv) 记录全部 52 个资产的主体材质；[mesh-coverage.csv](../../art-src/comic-city/surfaces/mesh-coverage.csv) 记录各网格实际包含的表面及三角形数量。

## 材质家族

| ID / 类型 | 默认粗糙度 | 重复尺寸 | 法线高度幅度 | 主要应用 |
|---|---:|---:|---:|---|
| 00 Plain | 1.00 | 2 m | 0 | 标线、明确的玻璃反光色块等干净图形 |
| 01 Plaster | 0.87 | 2 m | 3 mm | 咖啡店、餐馆、理发店、洗衣店、滑板车店、维修店、超市、公寓、影院与旅馆的粉刷墙 |
| 02 Brick | 0.90 | 2.56 m | 12 mm | 便利店、面包店、披萨店、书店、邮局、联排住宅、五金店；砖块色差、凹陷灰缝与边缘法线 |
| 03 Concrete | 0.91 | 2 m | 3 mm | 药店、银行、诊所、办公楼、仓库、停车楼、塔楼、背景楼；花箱、树池、矮墩、隔离墩、建筑基座与饰条 |
| 04 Asphalt | 0.96 | 2 m | 4 mm | 道路与远端道路延伸；干燥颗粒 |
| 05 Paving | 0.89 | 2 m | 9 mm | 人行道；0.5 m 方形铺地与细缝 |
| 06 PaintedMetal | 0.39 | 1 m | 0.7 mm | 消防栓、垃圾桶、邮筒、报刊箱、快递柜、配电箱、空调、灯杆、标牌、停车计费器、售货机、施工挡板、车身和窗框 |
| 07 Metal | 0.28 | 1 m | 0.5 mm | 护栏、停车架、卷帘门、滑板车立管；细拉丝与较窄高光 |
| 08 Wood | 0.78 | 2 m | 4 mm | 长椅、货箱、立牌、树干、候车座椅；连续木纹和板缝 |
| 09 Rubber | 0.97 | 0.5 m | 0.6 mm | 轮胎、把手、鞋、交通锥底座；弱细颗粒 |
| 10 Fabric | 0.94 | 0.5 m | 0.4 mm | 衣裤、围巾、遮阳棚；细织纹 |
| 11 Glass | 0.17 | 2 m | 0.1 mm 上限 | 窗户、候车亭背板、车辆玻璃与灯罩；平滑法线、极轻明暗变化与较集中的高光 |
| 12 Foliage | 0.93 | 2 m | 2 mm | 树冠和花箱植物；叶簇明暗分布 |
| 13 Plastic | 0.53 | 1 m | 0.6 mm | 分类垃圾箱、交通锥、头盔；弱模塑颗粒 |

重建保留已修改的 Library 参数。主体对应表控制大面积材质；窗户、框架、屋顶设备、植物、雨棚、轮胎等由几何生成器单独标记。新增资产 ID 会补入对应表，现有 ID 的选择会保留。

## 贴图标准

纹理由 `ComicSurfaceAssets.cs` 确定性生成，无外部图片或许可依赖。周期噪声、灰缝、铺地、木纹和织物使用同一高度场派生法线。

- 运行时资源为 `SurfaceMasks.asset`、`SurfaceNormals.asset`：**14 层 × 256×256、线性 RGBA32、完整 Mipmap、Repeat、Trilinear、4× 各向异性过滤**。第 0 层为干净表面。
- Mask：**R = 以 0.5 为中点的明暗变化；G = 粗糙度；B = 归一化高度；A = 微弱缝隙遮蔽**。G 不是 Smoothness，不要反转后再接入本 Shader。
- Normal：**RGB = XYZ 切线空间法线，+Y**，线性采样、自定义解码，不经过 Unity 普通 Normal Map 的平台通道重打包。由周期高度场中央差分计算，不改变几何轮廓或碰撞。
- 检查用 PNG 位于 `art-src/comic-city/surfaces/maps/`：每类提供 `_Mask`、`_Normal`、`_Roughness`、`_Shade`，共 56 张。游戏直接引用数组资源。
- 默认砖块约 **0.32 × 0.16 m**，每个 2.56 m 循环包含 8 列、16 行交错砖。木纹和铺地同样按实际米制尺寸计算。
- 精确参数、种子和快照见 `profiles.csv`、`library.snapshot.json`。修改图案算法在生成器内进行；不要直接修改会被覆盖的生成数组。

## 渲染与网格约定

UV0 保留四边形墨线；UV1 保留轮廓外扩法线；**UV2.xy = 米制表面坐标，UV2.z = 稳定材质 ID，UV2.w = 每米重复次数**。每个三角形的 ID 必须一致。枚举只能在末尾追加，不能重新排序已有 ID。

米制 UV 在组装、旋转、缩放之后按面的主轴投影烘焙，先于道路卷曲固定，因此弯曲、移动和回收街区时不会依靠摄像机位置滑动贴图。围巾运行时写入沿长度、宽度的连续 UV2，织纹跟随布条变形。

法线使用屏幕导数构造切线基。大面积三档明暗仍由原面法线决定，凹凸在各档内部增加小范围光照变化；粗糙度控制简化漫画高光的宽度和强度。这是风格化材质响应，不是完整金属度 PBR 或屏幕空间反射。玻璃保持不透明色块。

全局参数为 `shadeStrength / normalStrength / highlightStrength`。细节从 **24 m** 开始淡化，到 **100 m** 收回纯色块；Mipmap 同时降低远处细纹频率。天空图片继续使用独立图片天空盒。

`MAT_*` 样板使用独立色样、固定表面覆盖和关闭世界弯曲，适合材质展示。正式场景通过共享材质与网格 ID 保留各资产原色。直接替换为样板会改变色调和弯曲设置，场景调材质应通过 Library 对应表进行。

## 预算与验证

- 13 类纹理表面 + 1 类干净表面，覆盖原有 52 个资产及额外场景几何。每段仍为 **6 个渲染器**，没有增加三角形数。
- 两个数组含 Mipmap 合计约 **9.33 MiB GPU 数据**，当前保留同样大小的可读 CPU 副本支持编辑器原地生成；每个有细节的片元增加 2 次纹理采样。
- Shader 使用 `target 3.5 / 2darray`。Unity 支持在 WebGL 2.0 使用纹理数组，参见 [Unity 官方文档](https://docs.unity.cn/2023.2/Documentation/Manual/class-Texture2DArray.html)。本轮尚未执行 Web 构建或目标设备性能测试。
- Build 校验 UV2 数量、三角形 ID 一致性、合法贴图比例、图案周期性、法线单位长度及有限值，输出到 `surfaces/validation.txt` 和 `mesh-coverage.csv`。
- 已验证重复 Build：56 张数据图的 SHA-256 全部一致，记录在 `surfaces/map-hashes.json`；104 个生成网格通过材质 ID 与 UV2 检查。
- 已渲染并查看主镜头与材质图集；现有 **21/21 PlayMode 测试通过**，包括实际主场景、跨街区回收、围巾及声音联动。结果位于 `Logs/comic-city-PlayMode-results.xml`。
