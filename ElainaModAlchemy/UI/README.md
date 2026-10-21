# 炼金手记

默认 **P** 开关，可在控制设置中改键；没有常驻按钮。Esc 直接合上手记。开合继续使用技能表同款动画，关闭重开保留选中条目和滚动位置。

手记正文和随笔采用伊蕾娜本人的口吻：涉及自己的手艺、喜好与旅途经历时用第一人称，避免以旁观者视角介绍“伊蕾娜”或“她”。客观配方与效果记录保持简明；角色适用范围仍用明确的角色名称区分。中英文文案同步遵循这一视角。

2026-10-19 按 `Tools/AlchemyNotebookRedesign/index.html` 接入新版设计。标题区、等级菱形、经验条、研究收录、点线与星角饰沿用新版技能手札的设计语言；五个分类页签等宽，数字紧跟名称；选中标记（点亮的一段点线 + 细长星光）落在分隔线上，切换分类时 0.18 秒滑到新页签，点亮的点始终与分隔线点阵重合。

目录采用月白纸卡、点线内框、小菱形图钉与金色选中描边。可研究条目显示金色图钉和「可研究」，材料齐全的已研究条目显示「可制」，其余不再重复产量和持有数；英文分别使用 Researchable / Craftable。纸面保留倾斜、拾起回正、阴影与呼吸光点，像素物品只随图标锚点移动，不再随卡片旋转或放大。英文长名称以词为单位排成最多两行，并与状态题签留出间距。

右栏直接写在书页上：种类与状态、物品名、编号/稀有度/持有数、效果左右对齐行、素材清单、用途跳转与研究记录。中段独立滚动，批数胶囊、缺料提示和炼制按钮固定在底部。未研究时隐藏名称、效果及配方材料，只显示真实等级和配方物品条件，不照搬 HTML 的演示规则。原版替代材料组的持有数仍以组内合计判断。

底部素材栏按“当前配方所需 → 其他持有素材”排列，所需素材即使数量为零也显示。金边表示需求已满足，玫红表示缺料；数量标在槽位下沿。滚轮横向滚动，滚动条可拖动，有对应条目的素材可点击跳转，悬停显示名称、持有数和来源。选择不同条目会回到素材栏起点，库存刷新保持横向位置。材料来自原有合成来源快照（含可用容器），并非只读背包。

`AlchemyCatalogLayout` 统一正式 SUI 与预览的五列几何：卡片 118×132，列步长 133、行步长 156，目录视口 727×550，左/上留白 39/34。`AlchemyCardMotion` 的弹簧、`AlchemySelectionQuillMotion` 的移动与正式制作/研究流程继续复用；选中卡与羽毛笔在原生目录遮罩的叠加层绘制，目录两端使用原页面像素淡出。

新资源由 `Tools/AlchemyPreview/bake_redesign.py` 可复现生成：

- `Assets/NotebookCards.png`：284×936，单列三行（纸面、选中轮廓、柔和阴影），约 1.01 MiB RGBA8；没有重复存六份相同纸面。
- `Assets/NotebookStars.png`：128×256，四行（方形 .2、页签星光 34:12、侧边 .778/.25、角星 .976/.28），各含 64/32/16/8/4 预滤波尺寸档，从技能 UI 的 `Stars.png` 复制；`AlchemyDrawing.Star` 按长宽比与腰宽选最接近的一行，不再把方形星硬压成细长星光；`NotebookDot.png` 为预滤波小圆点。
- `Assets/CatalogFadeTop.png`、`CatalogFadeBottom.png`、`DetailFadeBottom.png`：从实际页面底图预先生成的淡出条，避免分数 UI 缩放时逐像素行绘制产生接缝。目录、详情与素材栏的滚动仍由原生 SUI 处理，SUI 自带滚动条只作透明的点击区域（比可见部分宽，便于拖动），可见部分由面板按 HTML 绘制：点线轨道 + 3px 薰衣草滑块，悬停或拖动时提亮。内容放得下时才把滚动条设为 Invalid；SUI 中 Invalid 的滚动条不会更新滚动位置，所以溢出时不能这样隐藏。详情栏没有留滚动条的位置（HTML 也隐藏），滚动条完全不绘制，但仍保持有效：滚轮与沿右缘拖动照常可用，底部仍用淡出提示下方还有内容。从详情或素材栏跳转条目时目录平滑滚动到该卡片；目标卡片若在另一分类中，会先以静止姿态出现再被拾起，和直接点击卡片一致。
- `Assets/Outlines/*.png`：64 张白色预乘描边遮罩，共 194,668 字节 RGBA8；包含当前像素素材与原版材料（含再生、魔能、箭术、召唤药水），坠落之星八帧分别处理。仅在深色背景的详情、材料行、用途链接与底栏使用，纸卡上不描边。

描边按源图 alpha 的四邻域外扩一像素预处理，运行时只加载并染为淡薰衣草色，与原图在同一 PointClamp 批次绘制。没有运行时 GetData、SetData、Texture2D 创建或用于描边的 RenderTarget。所有新 PNG 已预乘 alpha；加载仍使用 ModContent 缓存。描边增加一条 SpriteBatch 提交，未测量 GPU 时间或 FPS。旧 `CatalogPaper.png` 与生成器作为旧资产保留，正式绘制已使用新纸卡。

先运行 `pwsh -File Tools/AlchemyPreview/run.ps1 -ExportMaterials` 导出本机游戏材料，再运行 `python Tools/AlchemyPreview/bake_redesign.py`。生成器的 `--check` 验证贴图是否过期，不写资源。Tools 和 .vissandbox 已被 build.txt 排除。

## 真实制作与材料

已移除生产代码中的演示库存。`AlchemyNotebookState` 只管理导航，数量与资格由 `AlchemyNotebookSource` 读取角色和原版合成材料统计。

`Gameplay/AlchemyVanillaRecipes.cs` 在正常配方加载阶段注册 17 条实际 `Recipe`，配方带“仅在炼金手记中制作”条件，不会绕过研究出现在普通合成栏。材料收集直接调用安装版本的 `Recipe.CollectItemsToCraftWithFrom`；是否足够调用 `Recipe.CollectedEnoughItemsToCraftRecipeNew`；点击制作调用原版 `Main.CraftItem`，包含 `Recipe.Create`、材料消耗钩子、物品创建/制作回调、前缀与箱子同步。

因此材料来源、消耗顺序及替代材料组遵循原版：背包、打开的箱子/个人容器、启用的虚空袋，以及 `AddMaterialsForCrafting` 提供的材料。铁/铅、椎骨/腐肉以正式 RecipeGroup 注册。批量制作逐批重新检查，材料被其他逻辑改变时只报告实际完成的批数。

成品通过原版 `Player.GetItem` 放入背包，放不下的部分在玩家位置掉落；手上已有的鼠标物品会保留。没有新增炼金专用联机协议，材料变化沿用游戏已有同步。

灰赝尘默认不参与炼金。以后将 `AlchemyVanillaRecipes.AllowFacsimileDust` 设为 true，即可同时启用现有灰赝尘系统的材料判定、最大批数和实际补料消耗；无需重写扣料逻辑，已有正常合成功能不变。

## 研究与成长

`Gameplay/AlchemyProgressPlayer.cs` 用角色存档保存已研究条目、等级与经验，不与旧 `炼金/` 系统混用。新角色等级为 1、经验为 0，目录中标记为初始解锁的条目直接可用；其余未研究条目隐藏图标、名称、效果和制作材料，只显示问号与研究条件。

暂定规则集中在 `Gameplay/AlchemyProgressionRules.cs`：上限 **20 级**；1→2 级需要 **100 XP**，之后每级增加 **50 XP**；每批制作获得 **10 + 5 × 稀有度**经验，按批数而非产物件数计算。只有成功制作才给经验，到达上限后停止累积。`RequiredLevel(id)` 集中配置各条目门槛；当前基础药液和蜂蜜面包为 1 级，后续造物分布在 2～12 级。

嗜血、星力、集中、共鸣四种战斗药水需要 **炼金 Lv.10 + 对应实体配方 ×1**；轻羽药水需要 **炼金 Lv.2 + 轻羽药水配方 ×1**，在手记研究时消耗配方并永久解锁。旧存档已有的解锁保留，不追扣配方。其他条目沿用其等级条件；净土露滴的配方来源仍为设计提示，尚未绑定实体配方。

`item/Recipes/AlchemyRecipeScroll.cs` 为配方物品基类，五张配方各有独立物品 ID，共用 `ExampleAssets/Recipe_Pixel.png`（来自 `Tools/PotionPixel/output/Recipe_Pixel_New.png`）及对应预制描边。独立本地化提供名称、描述，通用提示读取实际研究等级、消耗数量和角色已掌握状态。配方只在手记研究时消耗，不直接使用，也不是炼制材料。

`Gameplay/AlchemyRecipeSources.cs` 集中配置获取方式：星力由巫师出售，集中由军火商夜晚出售，共鸣由巫医夜晚出售，基础买价均为 5 金（随原版商店价格调整）；嗜血由肉后血月的战斗敌人按基础 1% 概率掉落。友好 NPC、城镇 NPC、无战斗能力/无价值敌人、雕像生成物及共享生命的从属节段不掉落。商店与掉落不因个人已研究而隐藏，多余配方可以留给其他角色。

`Gameplay/AlchemyRecipeChestLoot.cs` 在新世界的 `PostWorldGen` 中向地下至地狱上方的普通金宝箱（`Containers` 样式 1）独立以 10% 概率追加轻羽配方 ×1，仅占用空槽，保留原有奖励。满箱、空箱、地表箱、地牢锁金箱、特殊生物群落箱与死人宝箱不参与；不会在开箱时重复投放，也不回填旧世界。轻羽仍用月露 ×1、羽毛 ×1、太阳花 ×1 炼制，产出 ×3。

未研究详情显示配方图标、名称、背包数量、等级条件、消耗说明和获取来源。每个 `AlchemyItem` 仍可以覆盖 `ResearchRequirement`：

```csharp
// 仅等级：达到 5 级后，点击研究。
public override AlchemyResearchRequirement ResearchRequirement => new(RequiredLevel: 5);

// 配方物品：填入对应配方类的 ModContent.ItemType。
public override AlchemyResearchRequirement ResearchRequirement => new(
    RequiredLevel: 1,
    RequiresRecipe: true,
    RecipeItemType: ModContent.ItemType<YourRecipeScroll>(),
    RecipeStack: 1,
    ConsumeRecipe: true);

// 每批经验也可以逐物品覆盖。
public override int CraftExperience => 20;
```

配方物品按背包检查；`ConsumeRecipe` 控制研究后是否消耗。设置 `RequiresRecipe=true` 而配方类型仍为 0 时，研究保持禁用，不会误解锁。已研究条目不能重复消耗配方。

## Debug

与技能表一致，等级 +1、补充素材、重置手记按钮及相应操作使用 `#if DEBUG`。

- 补充素材：给予当前配方单批需求的 **10 倍**，与数量选择器无关；替代材料组给予其中一种，优先放背包，满包落地。
- 重置手记：清空全部已研究条目，恢复等级 1、经验 0，保留真实物品。
- 等级 +1：每次提升一个炼金等级，保留当前经验和研究记录；达到 20 级时经验归零，按钮显示「已满级」并禁用。制作中不能升级。
- Release 构建不创建这三个按钮，也拒绝相应操作。

## 绘制与资产

魔力名称沿用物品说明中的 `klicon` 图标与颜色标签。`AlchemyDrawing` 将魔力图标和名称作为一个行内单元测量、缩放和换行，绘制实际 `Snippets/MagicStarIcon.png` 及蓝色文字，不使用星号字符替代。普通文本保持原绘制路径。

`Tools/AlchemyPreview/run.ps1 -StarPowerOnly -Culture zh-Hans`（英文为 `en-US`）只预览星力条目，在 720p、常规尺寸及 125% UI 缩放下检查这一行内单元。全量预览现在检查实际使用的新纸卡、星形和描边资产，不再要求已停用的彩铅素材。

外框沿用技能表的紫色书页主题，纸页使用炼金专属 `Assets/NotebookSurface.png` 连续渐变底图，避免技能星图裁剪区域带来的横向底色断层。底图直接取技能页底图，按 10px 网格求平均、高斯模糊（约 30px）后再用 B 样条放大，两本手记的光斑（目录区一团、详情栏上方一团）位置与亮度一致，但去掉了技能底图的矩形拼接边；加了确定性抖动，避免 8 位色阶环。改底图后须重跑 `bake_redesign.py`，淡出条是从底图裁出来的。用 `python Tools/AlchemyPreview/bake_surface.py` 重建，图片为 1100×800 不透明 RGB，按 RGBA8 加载约占 3.36 MiB，运行时缓存并单次绘制。技能表底图不受影响。局部辉光仍使用同一张 Glow.png。23 个炼金物品使用 `item/ExampleAssets/*_Pixel`，目录与详情按原图 1 倍居中绘制，随整体 UI 缩放。配方与底部素材栏中的 29 种原版材料，通过现有物品 ID 和 `Main.GetItemDrawFrame` 读取游戏贴图及当前动画帧，在槽位中居中绘制，保留原尺寸，只在超出槽位时缩小。右侧详情内容宽度比滚动区域窄 10 设计像素，为滚动条留出间隔。所有像素图标使用 `PointClamp`，绘制后恢复原批次状态；未研究剪影、字体和装饰保持现有绘制。

**物品像素图的外轮廓必须是纯黑 `#000000`。** 纸卡上不画描边光环，深紫灰（如 `#51415f`、`#332f43`）的外轮廓在浅紫纸面上会糊成一片，物品形状不清楚。规则只管外轮廓，也就是与透明像素或画布边缘四邻接的不透明像素；瓶塞分隔线、蝴蝶结内线等内部线条可以继续用深紫灰。新画或重画的贴图按此执行，绘制约定见 `Tools/PotionPixel/README.md`。

未研究物品使用 `Assets/MysteryPixel/*_Mystery_Pixel.png`：沿用对应物品像素图的画布和轮廓，问号直接绘进透明贴图，在手记中与已研究图标同样按原图 1 倍、Point 采样显示。修改物品像素图或新增目录条目后，运行 `python Tools/AlchemyPreview/prepare_mystery_pixels.py` 更新对应问号图。

选中羽毛取自 HTML 注脚的 `notebook-quill`，注脚自身不再绘制羽毛。封面、选中标记、字体、滚动裁剪与开合动画继续沿用已实现版本。

`AlchemySelectionQuillMotion` 只对更换选中条目进行路径缓动。目录滚动与面板移动通过当前绘制帧的卡片坐标同步应用；落定后相对卡片的偏移归零。羽毛笔锚点跟随选中卡的位置偏移、抬起和缩放，但不随倾斜旋转。羽毛笔与选中卡一起放在目录原生遮罩内的独立叠加层，不参与卡片排版、不拦截输入；快速重新选择从当前动画位置继续。

## 验证与工具

- `Tools/AlchemyGameplayChecks/run.ps1`：真实 tML 原版材料收集、制作、容器、替代材料、满包掉落、回调、研究、存档、经验及 Debug/Release 检查。
- `Tools/AlchemyNotebookPreview/run.ps1`：无游戏依赖的 UI 快照与导航检查。
- `Tools/AlchemyItemChecks/run.ps1`：物品注册、Buff 与贴图/本地化检查。
- `Tools/AshenFacsimileChecks/run.ps1`：已有灰赝尘功能回归及全模组源码编译。
- `Tools/AlchemyPreview/run.ps1 -Configuration Release`：FNA 离线预览，包含问号、等级不足、可研究、配方不足、配方齐全、满级和制作状态。预览数据仅存在于 Tools/AlchemyPreview/NotebookFixture.cs.txt，生产界面不会使用。

材料收集入口为当前 tML 的私有方法，只在加载时解析为委托，安装版本检查已覆盖；升级 tML 后应重新运行原版流程测试。离线检查不替代游戏内内容重载、实际点击/滚动及两人共享箱子的验收。


### 页面滚动

主目录与详情页隐藏竖向滚动条，支持滚轮和鼠标左键拖动非交互区域。按住物品卡、材料行或用途链接不会启动拖动；拖动过的详情行不会在松手时误触链接。共用的 `System/UI/ElasticPageScrollView` 将 SUI 坐标转换为页面设计坐标，`ElasticScrollMotion` 统一处理拖动阻力与滚轮越界回弹（最多 32 设计像素），不改变原有视口裁剪。选中条目仍可自动滚动到可见位置。离线检查见 `Tools/AlchemyPreview/run.ps1 -ScrollOnly`。
