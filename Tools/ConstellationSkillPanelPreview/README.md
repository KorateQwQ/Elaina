# 正式技能面板离线验证

`pwsh -File Tools/ConstellationSkillPanelPreview/run.ps1 -RecallResearchOnly` 运行回城魔法启封与研习专项检查，并导出待启封问号、已启封但缺药水、可研习、已习得四种场景，覆盖 720p、默认和 125% UI 缩放。研习和启封记录直接链接正式 `RecallSkill.Research`、`RecallDiscoveryPlayer` 与 `ConstellationState`，库存和引擎边界使用隔离桩。检查返回药水与回忆药水区分、20 点及 1 瓶药水扣费、失败不扣费、角色隔离、个人储物/鼠标物品/拾取/消耗、存档标签往返和已习得旧角色兼容。此模式独立执行专项断言，英文 Release 可加 `-Culture en-US -Configuration Release`。输出为 `.vissandbox/constellation-skill-panel/output/<culture>/recall-*.png`；真实存档序列化、游戏输入和联机仍需游戏内验证。

`-DiscoveryOnly` 链接正式 `StageGateUnlockCondition`、`ConstellationState` 与 `ConstellationDiscovery`，检查已习得/待研习/待启封/未显现四态、不可达门槛与其子技能未显现、未显现不能研习、可达门槛和未学前置列为缺口、依赖成环、追踪器的显现/启封判定与静默首次观察、共享新标记，以及提示队列的合并、淡入淡出、悬停保持和清空。随后导出显现、问号显现、多项合并、启封四种横幅的入场/停留/悬停帧（100%、125%），位于 `output/<culture>/discovery`。实际 SUI 点击、存档往返和真实 Boss 进度仍需游戏内验证。

运行 `& Tools/ConstellationSkillPanelPreview/run.ps1`。可通过 `-Tml` 指定安装目录，`-Sources` 指定本地 Terraria 源码目录。

`-DebugToolsOnly` 运行调试栏专项状态检查，并以正式 `DrawLayoutButton` / `DrawDebugButton` / `ToolButton` 导出普通、升满级悬停、精通技能悬停和布局编辑状态，覆盖 86% / 100% / 125% / 200% 缩放。输出 `output/zh-Hans/debug-tools-*.png`。状态检查链接正式 `ConstellationState`，每次抽取实际 `DebugMaxLevel`、等级规范化及 KL 解锁/升级方法；角色和技能定义为隔离夹具。覆盖当前上限、保留配置等级门槛的学习（含倒序/循环前置、隐藏节点、缺失条件及 Boss 等其他条件跳过；读取正式配置验证冰锥术在等级上限 35/36/39 时不学习、40 时学习，并验证首级角色等级门槛）、绕过自定义升级限制、免费学习进修、模板不变、装配/冷却保留、重复调用及提升上限后补齐。此模式始终执行专项断言，不依赖通用检查；`-DebugToolsOnly -Configuration Release` 验证调试方法被排除，不输出调试栏图片。真实 Boss 上限和内存存档往返由 `Tools/CharacterProgressChecks/run.ps1` 验证；完整游戏输入与联机需另验。

`-ScrollOnly -SkipChecks -Configuration Release` 链接共用的 `System/UI/ElasticScrollMotion` 与正式详情绘制，使用长描述、成长对比和关联技能夹具导出顶部/底部、拖动/滚轮的 40 个状态，覆盖 1280×720 与 1600×1000 / 125% UI 缩放。输出在 `output/zh-Hans/scroll-frames`，记录实际内容范围、越界偏移及最终回弹位置；原生滚动条不绘制。另导出 `middle-<宽>.png`（内容中段），检查共用 `System/UI/ScrollHint` 的上下双层箭头同时出现；到达顶/底边时对应箭头隐藏。共用状态机的 30/60/144 FPS、点击抖动、连续输入、重抓、内容缩短和取消拖动检查由 `Tools/AlchemyPreview/run.ps1 -ScrollOnly -Configuration Release` 运行。这些是 FNA 离线预览，完整 SUI 输入派发、开关点击和拖出窗口释放仍需游戏内检查。

2026-10-20 滚动专项验证：40 个技能详情采样、90 项共用滚动检查及炼金正式轮廓资源检查通过。现有通用业务检查 `current configured upgrade requirement is represented` 仍假定升级复用解锁材料，而正式默认升级条件现为 20 研习点，因此本次用 `-SkipChecks` 跳过通用业务/图标回归；滚动专项断言照常执行，未修改升级规则或放宽原有断言。

`-OrnamentsOnly` 链接正式 `ConstellationButterflyMotion` 与边框/蝴蝶绘制，并抽取正式 `UpdateButterfly`。导出带书页厚度的新外框、选中蝴蝶、空分类与编辑模式在 1600×1000、1280×720、125% UI 缩放下的画面；`output/butterfly-frames` 包含 100% / 125% 下各 241 帧，从移动、落定到停止及间歇扇翅。状态检查覆盖 30/60/144 FPS、途中改选、落定后翼拍、静止时位置不漂移、暂停/重置和不均匀帧间隔。`python Tools/ConstellationSkillPanelPreview/bake_butterfly.py` 从已有 `butterFly.png` 重建预乘 Alpha 的同款遮罩，保留原始文件。这些是实际 FNA 绘制的离线预览，游戏内 SUI 输入与生命周期另行验收。

`-CameraOnly` 每次从正式面板抽取 `CenterOn` / `AdvanceCamera` 与实际绘制方法，导出聚焦开始、100ms、200ms、350ms 和途中改选后的 FNA 离线帧，覆盖 720p 与 125% UI 缩放；检查最终图标居中、120% 倍率和改选瞬间的位置连续性。输出为 `output/camera-*.png`。使用隔离角色夹具，不模拟完整 SUI 点击、拖拽和开书输入生命周期，这些交互仍需游戏内验证。

`-BookOnly` 链接正式 `ConstellationBookMotion` / `ConstellationBookRenderer`，导出书从技能栏右上方锚点（原手札按钮位置）飞出、翻开、合拢、收回及两次快速反向的 FNA 连续帧。1600×1000 / 100% 输出 145 帧，1280×720 / 100% 和 1600×1000 / 125%、200% 输出代表帧，位于 `output/book-frames`。标题用实际 Noto Serif SC，页面使用正式绘制方法与隔离角色夹具。状态检查覆盖 30/60/144 FPS 时长、反向瞬间位置/曲率/速度连续性、闭式阻尼步长一致性、失焦收敛和世界重置。这不是游戏截图，也不模拟完整 SUI 输入树。

`python Tools/ConstellationSkillPanelPreview/book_contact_sheet.py` 将上述帧生成 `output/book-contact-sheet.jpg` 与 `output/book-animation.webp`。`book_reference.py` 用隐藏 Chrome 捕获当前 HTML 的五个固定开合姿态；它只写 `.vissandbox`，不运行会覆盖正式底图的 `reference.py`。网页与 FNA 的角色内容、字体和飞出锚点位置不同，比较的是曲面与动画阶段。

`-BookChecksOnly` 只运行开合状态和独立计时器检查，不启动 FNA 窗口或导出图片。计时器使用实际 Stopwatch 检查采样、停止及重开；动画状态另用确定的时间间隔验证不均匀帧、反向连续性与失焦收敛。正式计时器没有测试时间委托。离线连续帧仍使用固定时间步长，不能用它证明实际游戏帧间隔已变平稳。

正式面板的分类提示线、按钮和被动开关现在也由独立 `ConstellationUIClock` 驱动。`PolishChecks` 验证不均匀/重复绘制间隔下提示线的位置一致，以及快速反向时的位置连续性与 180ms 收敛；可用 `-IconsOnly` 运行包括这些检查的状态回归，不导出连续帧。

`bake_book_cover.py` 从参考 HTML 提取无文字正反封面 SVG，调用 svg-to-png 的 resvg 转换器生成预乘 Alpha PNG，标题由游戏字体绘制。Python 依赖是 svg-to-png 技能的 `scripts/requirements.txt`；可使用隔离环境 `.vissandbox/book-python/Scripts/python.exe` 执行上述脚本。

预览现在直接链接相邻 `SilkyUIFramework` 源码。旧 `-Sources` 默认路径不存在时会尝试用户 Downloads 下的 `tML 2025.06源码与ExampleMod/tModLoader`；其他机器可显式传入路径，不依赖 DLL 反编译副本。

`-PrimaryOnly -SkipChecks -Configuration Release` 重放正式 `DrawActionButton`，导出免费研习、付费研习、材料不足、装配、已装配、满级未装配、满级已装配在 86% / 125% / 200% 的截图，以及连续悬停/按下帧。专项检查按正式命中区域连续点击进修至满级并继续点击，验证左右按钮位置/宽度不变、原进修区不触发装卸、右侧按钮仍可点击；结果写入 `primary-layout-checks.txt`，不受 `-SkipChecks` 影响。`bake_primary_button.py` 重建主按钮切角渐变与装饰覆盖图；点数、槽号和文字仍来自运行时状态。

`-PolishOnly` 导出分类提示线滑动、进修悬停/按下和延迟提示的 FNA 连续帧，并检查三种尺寸下的紧凑材料列表。动态检查使用正式 `ConstellationTween` / `ConstellationTooltipDelay` / 控件绘制；提示位置检查使用正式布局方法。状态测试覆盖快速改选、提示延迟与控件切换、边缘约束和避让材料区。

`-ToggleOnly` 使用正式开关绘制与动画状态，导出 100%、125%、200% 下各 96 帧，覆盖开启、关闭、悬停和快速反向切换，另导出实际详情行的悬停截图。状态检查覆盖帧率独立性、端点收敛、详情重建连续性和悬停退出；输出位于 `output/toggle-frames`。这是 FNA 离线动画，不模拟完整 SUI 鼠标派发。

默认以 Debug 配置验证调试按钮及加点/重置行为。`-Configuration Release -IconsOnly` 编译并验证 Release 不包含调试状态修改方法。重置检查涵盖未展示技能、装配、等级、冷却、取消学习回调、隐藏模板恢复、点数/布局保留、重复重置与重新学习。所有操作仅在隔离角色夹具上执行。

等级栏由正式的 `ConstellationDrawing.PlayerLevel` 绘制，场景使用 `Lv. 120 / 上限 120`、`经验 0 / 100`，覆盖三位等级和值分隔符的排版。该预览只提供隔离的固定角色数值；角色 Progress、ProgressSet、当前等级和经验的重置由正式 Debug 构建验证。

`-HeadersOnly` 仅重放正式顶部绘制，检查研习点书本图标、标签和 0 / 9999 点数在 86%、100%、125% 比例下的布局；不重复整套状态检查和全界面截图。`bake_study_book.py` 调用 svg-to-png 技能的 resvg 转换器（可用 `--converter` 指定），再离线转换为游戏需要的预乘 Alpha PNG。

链接正式 `ConstellationState`、`ConstellationDrawing`、`ConstellationDetail`、`ElainaSkill.Constellation`，每次从正式 UI 重新抽取地图、节点、详情标题和装配栏绘制方法。富文本链接实际 SUI 解析与布局组件、KL 图标片段、Terraria 文本片段；加载实际 XNB 字体与技能 PNG。原生窗口隐藏并自动退出。

角色、注册表、库存及装备通知边界由隔离桩提供，不读写真实角色。11 个技能的夹具仅模拟注册、分类与布局；数值与文字是测试输入，不能视为游戏状态快照。状态检查覆盖资格筛选、解锁、升级、依赖、装配、空数据、角色切换；截图覆盖富文本被动、主动、装配、缺失描述、关闭前置、空分类以及滚动，包含 720p 和 125% UI 缩放。

生成项目与图片位于 `.vissandbox/constellation-skill-panel`。这是 FNA 离线预览，不覆盖完整 SUI 输入树、真实存档序列化、技能执行和联机；这些需游戏内验证。工具源使用 `.cs.txt`，生成文件位于默认编译排除的点目录，且 Tools 与 .vissandbox 均已由 build.txt 排除打包。

布局编辑验证链接实际 `ConstellationLayout` 与 `ConstellationLayoutEditor`，并抽取正式编辑按钮与操作提示绘制。新增检查覆盖缩放下拖动坐标换算、网格对齐、取消、边界更新、布局优先级、JSON 保存重载、未知技能条目保留、格式错误及写入失败。所有写入都在 `.vissandbox/constellation-skill-panel/layout-checks` 内，绝不覆盖游戏实际布局。`editor` / `editor-moved` 场景分别覆盖进入编辑与移动后的坐标、未保存标记及底部工具栏。

无色图标直接加载手工提供的 `_Gray.png`。`IconChecks` 验证原图/无色图选择、共用资产复用、缺图与空路径回退、缺图查询缓存，以及正式面板的实际 FNA 绘制。`-IconsOnly` 运行状态和图标检查后退出，不批量输出截图。预览宿主负责释放模拟资产加载器拥有的纹理；游戏中的这些贴图由 `ModContent` 管理。

最大等级验证每次从 KL 源码抽取实际 `ModSkill.MaxLevel`、`TryLevelUp()` 和 `Skill.MaxLevel`，检查默认 5、固定覆写、动态上限变化、满级不扣费、超上限存档保留和详情刷新；`maxed` 场景检查 5 / 5 及满级按钮显示。

新版 HTML 对照：`python Tools/ConstellationSkillPanelPreview/reference.py` 使用本机隐藏 Chrome 捕获网页并记录 CSS 颜色/尺寸，同时生成正式 `Assets/NotebookSurface.png` 无文字底图。网页资源与脚本来自本仓库的参考 HTML，捕获的是临时副本；源 HTML 不改动。

正式详情链接 `ConstellationRichText`，使用实际 KL 富文本与图标片段、项目字体，按参考的 20/21 像素行距排版。`growth` / `growth-poor` / `growth-long` 是隔离的视觉夹具，用示例值检查五行成长对比、消耗、资金不足与滚动裁剪；它们不为正式技能添加演示伤害或升级规则。常规 `active` / `passive` 等场景仍检查未提供成长对比时的实际界面行为。

`-RequirementsOnly` 专项导出研习、进修、需求满足、边缘多行提示和长材料名称，包含小屏与 UI 缩放。状态检查链接正式需求解析器和 KL 消耗规则，覆盖跨背包槽统计、嵌套组合需求、红绿状态、部分材料增加后的刷新、动态研习费用传递、失败时不扣费以及自定义说明中的“研习点”命名。免费进修测试使用显式返回 None 的夹具，通用材料需求使用隔离测试技能，正式默认进修只消耗研习点；肉前技能费用由 CharacterProgressChecks 对实际程序集逐项检查。

## 标题栏角色成长（2026-10-08）

`& Tools/ConstellationSkillPanelPreview/run.ps1 -HeadersOnly` 重放正式 `DrawChrome` / `DrawFilter` / `ConstellationDrawing.PlayerLevel`，沿用 KL 的 Noto Serif SC、HarmonyOS Sans SC 与 Gelasio 字体。标题栏等级布局对应 `elaina-battle-eight-skills-level-preview.html`：淡金菱形等级、右侧上限与经验、3px 渐变进度条；星图收录独立位于详情页上方。游戏仍从 `ElainaStatePlayer` 实时读取等级与经验，预览只注入隔离夹具。

输出 `output/study-header-{研习点}-lv{等级}-xp{经验}-{缩放}.png`，覆盖 1/24/99/120 级、0/65/99/100 经验、0/9999 研习点和 86%/100%/125% 缩放，共 30 张。此模式无需加载全场景的技能图标；常规全场景检查若因旧夹具引用缺失的 `AshenWitchSkill_Gray.png` 中断，不能视为全场景验证通过。所有图像为实际 FNA 绘制的离线预览，不是游戏截图，实际 SUI 输入和游戏重载需另验。

`-HealingOnly -SkipChecks` 链接正式 `HealSkill.Values.cs` 与本地化说明，导出未学习、1 级、满级、说明滚动和升级对比，覆盖 720p、默认和 125% UI，英文加 `-Culture en-US`。耗蓝/冷却和成长上限读取正式 JSON，人物与施法生命周期为夹具；每个场景验证格式参数已解析并显示当前治疗量。施法、叠加、禁止生命代付与逐级回血另由 `Tools/HealingWheelPreview/check-behavior.ps1` 检查。

## 专有名词悬停说明（2026-10-26）

`-TermsOnly -SkipChecks`（英文加 `-Culture en-US`）导出 `terms-freeze`（冰锥术）、`terms-magic`（灰之魔女）与 `terms-cauldron`（炼金术）在 720p、默认和 125% UI 下的详情页，悬停首个已注册名词：重放正式 `ConstellationRichText` 命中区域、`ConstellationDetail` 下划线与锚点、`ConstellationDrawing.TermTooltip` 说明卡（2026-10-26 定稿为“手札便签”款：书页点线内框加四角星、衬线名称、可选的大号数值两栏；曾对比书签夹页、星注两款）。名词注册在 `Program` 中按正式路径、本地化键与冻结常量复刻（正式注册位于 KL `ElementBuildUpSystem` 与 `Localization/ElainaTerms.cs`），并额外加载 KL 的 HJSON；`KL/` 开头的贴图从相邻 KL 目录读取，`Item:` 名词图标从原版 XNB 内容读取（桩 `ItemID.Search` 只覆盖预览文本用到的物品名）。冰锥术的伤害/积累参数是夹具值。实际 SUI 悬停派发、提示延迟和滚动中的命中需游戏内验证。

`-ForgetOnly -SkipChecks`（英文加 `-Culture en-US`）导出魔法飞弹 Lv.3 持有 2 本忘却之书时的 `forget`（未确认）、`forget-armed`（等待确认）和 `forget-lv1`（已是 Lv.1）详情页，滚动到详情底部，覆盖 720p、默认和 125% UI。正式结算位于 `ElainaModSkills/SkillForgetting.cs`，预览中 `BookOfOblivion` 物品 ID 为桩值；默认检查覆盖无书不可忘却、返还等于进修研习点之和、只扣 1 本、退回 Lv.1 仍习得、Lv.1 不扣书、改选取消确认、忘却后可再次进修。实际点击、背包变化和存档需游戏内验证。


旧游戏内星图演示和聊天命令已移除；本工具只在游戏外重放正式面板。
`-IconsOnly -SkipChecks` 单独运行图标回归（显式指定 IconsOnly 时仍执行），不依赖通用业务夹具。成功加载用已有无色图样本验证；正式技能的可选路径允许缺失，仍由正式绘制验证回退。
正式贴图和图集索引统一位于 `ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Assets`，炼金共用的 Glow/Disc 也从此处加载。
`GenerateAssets.py` 重建 Icons 图集、Disc、Glow 和连线贴图；`GenerateBackground.py` 只重建 Glow，`GenerateLinks.py` 只重建连线；`GenerateHeaderHat.py` 从正式目录中的 HeaderHat.svg 重建徽记。星形仍由 `bake_stars.py` 重建/检查。
上述生成脚本位于 Tools，不参与游戏加载；不会再生成演示技能、独立点数或旧演示底图。字体和图标检查由本工具覆盖，旧 TypographyPreview 已删除。
