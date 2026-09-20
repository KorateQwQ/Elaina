# 正式技能面板离线验证

运行 `& Tools/ConstellationSkillPanelPreview/run.ps1`。可通过 `-Tml` 指定安装目录，`-Sources` 指定本地 Terraria 源码目录。

`-PrimaryOnly` 重放正式 `DrawActionButton`，导出免费研习、付费研习、材料不足、装配、已装配在 86% / 125% / 200% 的截图，以及连续悬停/按下帧。`bake_primary_button.py` 重建主按钮切角渐变与装饰覆盖图；点数、槽号和文字仍来自运行时状态。

`-PolishOnly` 导出分类提示线滑动、进修悬停/按下和延迟提示的 FNA 连续帧，并检查三种尺寸下的紧凑材料列表。动态检查使用正式 `ConstellationTween` / `ConstellationTooltipDelay` / 控件绘制；提示位置检查使用正式布局方法。状态测试覆盖快速改选、提示延迟与控件切换、边缘约束和避让材料区。

`-ToggleOnly` 使用正式开关绘制与动画状态，导出 100%、125%、200% 下各 96 帧，覆盖开启、关闭、悬停和快速反向切换，另导出实际详情行的悬停截图。状态检查覆盖帧率独立性、端点收敛、详情重建连续性和悬停退出；输出位于 `output/toggle-frames`。这是 FNA 离线动画，不模拟完整 SUI 鼠标派发。

默认以 Debug 配置验证调试按钮及加点/重置行为。`-Configuration Release -IconsOnly` 编译并验证 Release 不包含调试状态修改方法。重置检查涵盖未展示技能、装配、等级、冷却、取消学习回调、隐藏模板恢复、点数/布局保留、重复重置与重新学习。所有操作仅在隔离角色夹具上执行。

`-HeadersOnly` 仅重放正式顶部绘制，检查研习点书本图标、标签和 0 / 9999 点数在 86%、100%、125% 比例下的布局；不重复整套状态检查和全界面截图。`bake_study_book.py` 调用 svg-to-png 技能的 resvg 转换器（可用 `--converter` 指定），再离线转换为游戏需要的预乘 Alpha PNG。

链接正式 `ConstellationState`、`ConstellationDrawing`、`ConstellationDetail`、`ElainaSkill.Constellation`，每次从正式 UI 重新抽取地图、节点、详情标题和装配栏绘制方法。富文本链接实际 SUI 解析与布局组件、KL 图标片段、Terraria 文本片段；加载实际 XNB 字体与技能 PNG。原生窗口隐藏并自动退出。

角色、注册表、库存及装备通知边界由隔离桩提供，不读写真实角色。11 个技能的夹具仅模拟注册、分类与布局；数值与文字是测试输入，不能视为游戏状态快照。状态检查覆盖资格筛选、解锁、升级、依赖、装配、空数据、角色切换；截图覆盖富文本被动、主动、装配、缺失描述、关闭前置、空分类以及滚动，包含 720p 和 125% UI 缩放。

生成项目与图片位于 `.vissandbox/constellation-skill-panel`。这是 FNA 离线预览，不覆盖完整 SUI 输入树、真实存档序列化、技能执行和联机；这些需游戏内验证。工具源使用 `.cs.txt`，生成文件位于默认编译排除的点目录，且 Tools 与 .vissandbox 均已由 build.txt 排除打包。

布局编辑验证链接实际 `ConstellationLayout` 与 `ConstellationLayoutEditor`，并抽取正式编辑按钮与操作提示绘制。新增检查覆盖缩放下拖动坐标换算、网格对齐、取消、边界更新、布局优先级、JSON 保存重载、未知技能条目保留、格式错误及写入失败。所有写入都在 `.vissandbox/constellation-skill-panel/layout-checks` 内，绝不覆盖游戏实际布局。`editor` / `editor-moved` 场景分别覆盖进入编辑与移动后的坐标、未保存标记及底部工具栏。

无色图标直接加载手工提供的 `_Gray.png`。`IconChecks` 验证原图/无色图选择、共用资产复用、缺图与空路径回退、缺图查询缓存，以及正式/演示两套实际 FNA 绘制。`-IconsOnly` 运行状态和图标检查后退出，不批量输出截图。预览宿主负责释放模拟资产加载器拥有的纹理；游戏中的这些贴图由 `ModContent` 管理。

最大等级验证每次从 KL 源码抽取实际 `ModSkill.MaxLevel`、`TryLevelUp()` 和 `Skill.MaxLevel`，检查默认 5、固定覆写、动态上限变化、满级不扣费、超上限存档保留和详情刷新；`maxed` 场景检查 5 / 5 及满级按钮显示。

新版 HTML 对照：`python Tools/ConstellationSkillPanelPreview/reference.py` 使用本机隐藏 Chrome 捕获网页并记录 CSS 颜色/尺寸，同时生成正式 `Assets/NotebookSurface.png` 无文字底图。网页资源与脚本来自本仓库的参考 HTML，捕获的是临时副本；源 HTML 不改动。

正式详情链接 `ConstellationRichText`，使用实际 KL 富文本与图标片段、项目字体，按参考的 20/21 像素行距排版。`growth` / `growth-poor` / `growth-long` 是隔离的视觉夹具，用示例值检查五行成长对比、消耗、资金不足与滚动裁剪；它们不为正式技能添加演示伤害或升级规则。常规 `active` / `passive` 等场景仍检查未提供成长对比时的实际界面行为。

`-RequirementsOnly` 专项导出研习、进修、需求满足、边缘多行提示和长材料名称，包含小屏与 UI 缩放。状态检查链接正式需求解析器和 KL 消耗规则，覆盖跨背包槽统计、嵌套组合需求、红绿状态、部分材料增加后的刷新、动态研习费用传递、失败时不扣费以及自定义说明中的“研习点”命名。免费进修测试使用显式返回 None 的夹具，默认费用测试保留项目当前的木材×10＋研习点×20。
