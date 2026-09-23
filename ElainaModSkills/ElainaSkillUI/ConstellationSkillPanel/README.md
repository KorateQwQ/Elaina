# 正式技能面板

默认 V 打开 `ConstellationSkillPanel`。旧 `ElainaSkillPanel` 与 `/constellationpreview` 演示保留，正式面板不读取演示 Skills.json。

外框保留原来的紫色细边和角花颜色，增加内嵌角花、左侧装订凹槽，以及右侧/底部三层同色系叠页，增强书本厚度；内容底图、布局与控件保持原样。边缘装饰最多伸出设计区域 13px，沿用原有 16px 屏幕留白。

选中节点左上方增加与 HTML 同源的蝴蝶：改选时约 420ms 飞到新节点，移动期间持续播放 16 帧扇翅；到达后至少继续 650ms，并完成当前翼拍后停在首帧。静止 4.5–6.2 秒后再扇动一轮。飞行使用星图坐标，随平移/缩放一致变换并接受视口裁剪；快速改选从当前位置接续，失焦暂停，关闭/退出世界重置，空分类隐藏。仅增加选中装饰，不改技能状态或鼠标命中。

`ConstellationButterflyMotion.cs` 管理动画，`ConstellationDrawing.NotebookPage` / `SelectionButterfly` 绘制边框与蝴蝶。`Assets/butterFly.png` 为原始素材，保持不变；`python Tools/ConstellationSkillPanelPreview/bake_butterfly.py` 从其 Alpha 生成与 HTML mask 一致的预乘 `ButterflyMask.png`，运行时交由 ModContent 加载。

专项验证：`& Tools/ConstellationSkillPanelPreview/run.ps1 -OrnamentsOnly`。输出包含实际边框/选中绘制在 720p、100% / 125% UI 缩放下的 FNA 离线画面，以及蝴蝶移动、落定、静止、间歇翼拍连续帧；状态检查覆盖 30/60/144 FPS、快速改选、空分类重置与暂停。正式游戏输入和开关生命周期仍需编译重载后验收。

技能栏右上方新增 **手札** 按钮，显示当前绑定键；它与 V 共用开关入口。打开约 560ms、关闭约 440ms：书从按钮飞出，带弧线、倾斜和 32 段连续弯曲的双面封面；关闭时反向收回。快速按 V 可在中途反向，保持位置、缩放与曲率连续。动画期间根控件接收鼠标以防穿透，子控件与技能栏禁用鼠标操作；完全展开后恢复原生 SUI 布局和命中。

关闭/返回会先取消未保存的布局编辑，再播放合拢；Esc 在编辑模式仍先取消编辑。失焦结束拖动并收敛到动画目标；退出世界保留原有状态重置，立即关闭而不播放动画，不释放框架资产。

开合与控件动画共用 `ConstellationUIClock` 实现，每个计时器仅保存一个 `Stopwatch` 与上次采样刻度，不依赖 `Main.gameTimeCache` 或 `Main.timeForVisualEffects`。开合计时器仍在开始/反向时重启、结束时停止；控件使用独立实例，在面板打开期间持续采样，驱动分类提示线、按钮悬停/按下、被动开关、提示延迟及状态刷新。开合结束不会停止分类动画的计时；关闭时重置，失焦与恢复焦点首帧不推进控件动画。

复古光照模式仍支持此动画：原版在该模式下会调整世界 RT 绘制和场景捕获路径，但不会禁用 FNA 的 `RenderTarget2D`。手札使用 SUI 自己的 RT 池，不读取原版 `screenTarget` 或依赖 `Filters.Scene` 捕获。

封面正反面的 `BookCoverFront.png` / `BookCoverBack.png` 由 HTML 内嵌 SVG 提取，均为 1100×800 预乘 Alpha 图片。封面标题使用项目 Noto Serif SC，与装饰合成后缓存。动画页面 RT 向 SUI `RenderTargetPool` 借还，封面 RT 缓存跨世界复用；模组卸载时归还缓存，并释放本次新增绘制器自行创建的 `BasicEffect`。`ModContent` 的纹理与字体不由面板释放。完整展开走原有直绘路径，动画中不每帧创建纹理、Effect 或网格数组。

专项离线验证：`& Tools/ConstellationSkillPanelPreview/run.ps1 -BookOnly`。此预览链接正式动画与曲面绘制代码，提供连续帧和状态检查；实际 SUI 输入、世界切换和游戏设备生命周期仍需编译重载后验收。背景使用与参考一致的深色遮罩，本次未增加网页的背景模糊采样。

- 自动收录注册表中带 `SkillUIInfo` 的 `ElainaSkill`，按 State / Pixels 排列。空白阶段被压缩；星图可平移缩放。
- 已解锁技能直接使用角色的 `UnlockedSkill` 实例，装配使用 `ActiveSkill`，技能点使用 `SkillPoint`，沿用 KL 的存档格式与装备通知。
- 解锁复用 `UnlockCondition` / `TryUnlockSkill` / `UnlockSkill`；前置使用 `PrerequisiteSkills`。缺失或循环依赖不能被当作已满足。
- `IsPassiveSkill` 决定是否可装配，`IsToggleable` / `IsEnabled` 对应实际开关。开关继续沿用 KL 的运行时语义，不额外存档。战斗效果与依赖检查仍由技能自身实现。
- 描述使用现有 SkillDesc 本地化、`SkillDescriptionArgs` 与 KL/SUI 富文本。缺失描述或格式参数显示占位；名称缺失时显示技能类名。

## 手动编辑布局

Debug 构建中，底部点击 **编辑模式** 后，可以拖动任意技能图标；连线实时跟随，底部显示当前节点的 X / Y。编辑时全部节点正常显示，不受技能分类的淡化效果影响。Release 构建隐藏编辑入口，仍会读取保存好的布局。

- 拖动空白处平移；滚轮缩放；按住 Shift 拖动图标，按 10 个设计像素对齐。
- **保存**：写入 JSON 并退出编辑，下次进入世界仍会应用。保存失败保留编辑草稿。
- **取消** 或 Esc：恢复进入编辑前的位置。直接关闭面板、按 V、退出世界也会取消未保存的修改。
- **恢复默认**：将草稿恢复为模组默认布局，仍需点击保存；也可以取消这次恢复。
- 编辑模式只调整位置，研习、进修、被动开关及清空槽位暂不可操作。

保存路径使用 `Main.SavePath/ModConfigs/Elaina/ConstellationLayout.json`，本机通常为：

```text
D:/Documents/My Games/Terraria/tModLoader/ModConfigs/Elaina/ConstellationLayout.json
```

它是本机共用的布局，不写入角色或世界存档。以技能类型全名作为键，坐标是星图坐标，不包含分辨率、UI 缩放、镜头平移与缩放。JSON 例如：

```json
{
  "Version": 1,
  "Positions": {
    "伊蕾娜.ElainaModSkills.Skills.MagicMissile.MagicMissileSkill": {
      "X": 285,
      "Y": 80
    }
  }
}
```

**定稿发布**：把保存的 JSON 复制到本目录的 `Layout.json`，随模组重新构建，即成为所有玩家的默认布局。优先级为：本地 JSON > 模组内 `Layout.json` > `ConstellationPosition` / `SkillUIInfo`。新增技能在 JSON 中没有记录时，沿用代码默认位置。

本地文件损坏或版本不支持时，会提示并回退到模组默认布局，原文件不会因读取失败而被覆盖。手动修改外部 JSON 后重新进入世界即可重读。若要检查新发布的默认布局，可进入编辑模式点恢复默认，或移走本地覆盖文件后重新进入世界。

## Debug 技能操作

以下按钮与编辑入口一样受 `#if DEBUG` 限制；Release 不创建按钮，也不包含加点/重置方法。

- **获得100研习点**：增加当前角色的 `SkillPoint`，达到 int 上限时饱和，避免溢出。
- **重置**：清除当前角色技能玩家中的全部学习记录（包含未加入星图的技能）、等级、冷却与装配，调用已有 `LockSkill` 回调，刷新技能栏并清空当前技能索引。未学习节点恢复注册模板，包括隐藏状态。保留研习点余额、背包物品和星图位置，不返还已花费的材料或点数。

编辑布局期间，这两个按钮让位于保存/取消/恢复默认；隐藏控件同时退出命中检测。按钮会修改角色真实技能状态，正常保存角色时持久化。关闭世界或切换角色后，旧面板状态无法执行这些操作。

## 无色图标命名

在技能原图同目录放置 `技能类名_Gray.png`，例如：

```text
ElainaModSkills/Skills/AshenWitch/AshenWitchSkill.png
ElainaModSkills/Skills/AshenWitch/AshenWitchSkill_Gray.png
```

`_Gray` 是手工提供的无色版本，可使用白色图案与透明背景，建议与原图保持相同尺寸和对齐。`_Mask` 继续表示已有的光效层。未解锁或失效节点、失效槽位与前置关系图标需要无色显示时，会优先读取 `_Gray`；没有此图时使用调暗的原图。正常状态与布局编辑模式使用原图。

所有贴图都通过 `ModContent` 加载，由 tModLoader 管理；面板不再读取像素生成灰度副本，也不负责释放这些资产。添加图片后需重新构建并加载模组。

自定义资源目录可覆写 `ConstellationUncoloredIconPath`，返回完整资源路径但不含 `.png`。独立演示界面使用其原图目录 `ElainaModSkills/Icon/` 下的同名 `_Gray.png`。

## 补充入口

在具体技能中 override 以下成员，默认不会虚构伤害、依赖或成长数值：

- `PrerequisiteSkills`：解锁前置与展示关系。
- `GetConstellationDescription()`：覆盖描述；通常只需补本地化与 `SkillDescriptionArgs`。
- `GetConstellationStats(int level)`：补充伤害等 `(Label, Value)` 明细；应从技能实际计算结果取值。
- `GetConstellationUpgradePreview(int nextLevel)`：返回 `(Label, Current, Next, Unit)` 数值对比行，对应 HTML 的成长预览表。Current/Next 使用数字文本，Unit 可包含 MP、秒、% 等；默认空数组，不推算未实现的伤害或成长。仅展示数据，不会修改当前等级或技能状态。
- `ConstellationPosition`：可选星图坐标，以设计像素为单位。
- `ConstellationUncoloredIconPath`：可选无色图标路径；默认按技能原路径追加 `_Gray`。
- `MaxLevel`：继承 KL 的 `ModSkill.MaxLevel`，默认 5，可覆写固定或动态上限。星图详情、节点等级、提示与升级判定统一读取它。原 `ConstellationMaxLevel` 已由此公共接口替代。
- `GetConstellationUpgradeCondition(int nextLevel)`：当前项目配置为木材×10、研习点×20。返回 `SkillUnlockCondition.None` 或 null 时，已习得且未满级可免费进修。配置物品、研习点或自定义条件后，检查并消费相应资源，再调用 `TryLevelUp()`。请同步实现实际等级效果。

最大等级配置示例（写在具体技能类中，二选一）：

```csharp
// 固定上限；不覆写则继承默认 5。
public override int MaxLevel => 10;

// 或按世界进度决定上限。
public override int MaxLevel => Main.hardMode ? 10 : base.MaxLevel;
```

上限实时求值，不保存独立副本。`Skill.MaxLevel` 与基础 `TryLevelUp()` 将无效的低于 1 的值按 1 处理；上限降低或旧存档已超出上限时，保留当前等级并禁止继续升级。覆写 `TryLevelUp()` 时也应遵守该上限。最大等级与进修消耗分别配置；免费进修会提升真实等级，具体伤害成长仍由技能实现。需要暂时禁止进修时，可以返回 `SkillUnlockCondition.Custom((player, skill) => false)`。

源码编译（不打包、不重启游戏）：

```powershell
dotnet build '伊蕾娜.csproj' -t:Compile -p:BuildProjectReferences=false --no-restore -v:quiet -clp:ErrorsOnly
```

`Tools/ConstellationSkillPanelPreview/run.ps1` 提供隔离状态回归和实际绘制的 FNA 离线预览。完整鼠标输入、角色存档往返与多人游戏仍需在重新加载模组后验收。

## HTML 样式同步

底部研习/装配主按钮使用 6px 切角书签轮廓、中央稍亮的紫色渐变及浅粉白文字，已移除内层矩形框和“＋”。无研习点费用时居中显示研习；有实际点数费用时显示书本和数量；装配/已装配保留槽号。缺少材料、前置不满足等状态沿用同一轮廓并弱化背景。悬停与按下明暗继续使用持久化缓动状态。

`PrimaryActionFill.png` / `PrimaryActionTrim.png` 是 1104×152 的预乘 Alpha 静态覆盖图，由 `python Tools/ConstellationSkillPanelPreview/bake_primary_button.py` 生成，交给 ModContent 管理。

第一轮视觉细化：分类提示线宽 112 个设计像素，切换时约 180ms 滑动；进修按钮采用 3.5px 小圆角、细微渐变和缓动的悬停/按下明暗反馈，费用位置固定。材料区使用名称/数量两列，满足状态仅显示淡绿色勾选，缺少时增加红色缺口行；长名称自动换行。

悬浮提示停留约 250ms 后出现，按控件定位：右侧详情优先放在控件左侧，其余优先上方并限制在窗口范围内。研习/进修提示只给出操作及缺口，不重复完整材料表。`UpgradeFill.png` / `UpgradeOutline.png` 为离线预乘 Alpha 贴图，用 `python Tools/ConstellationSkillPanelPreview/bake_upgrade_button.py` 重建，无运行时纹理生成。

被动开关使用连续胶囊形底槽与轮廓，悬停仅平滑提亮，不绘制技能图标四角框。`ConstellationToggleAnimation` 按帧时间对位移、颜色和悬停状态缓动；动画按技能 ID 保留，详情重建不会重置，快速反向切换从当前位置继续。逻辑开关即时生效，视觉约 0.2 秒接近目标。关闭面板仅清空动画数据。

底槽、轮廓、滑块使用三个 ModContent 资产 `ToggleTrack.png` / `ToggleOutline.png` / `ToggleThumb.png`，分别为 148×84 / 148×84 / 60×60，合计约 111 KiB RGBA 显存；均为离线生成的预乘 Alpha 覆盖图。用 `python Tools/ConstellationSkillPanelPreview/bake_toggle.py` 重建；运行时不创建或释放纹理。

右上角将角色 `SkillPoint` 显示为“研习点”。书本标识源文件为 `Assets/StudyBook.svg`，运行时使用 `StudyBook.png`（128×128，显示为 27 个设计像素，金色着色）。导出命令：`python Tools/ConstellationSkillPanelPreview/bake_study_book.py`。PNG 已为 tML 的 rawimg 管线做预乘 Alpha，不能再次预乘；SVG 保持可编辑。

正式面板保留角色数据、布局编辑和进修逻辑，详情样式使用新版 HTML 的暖紫正文/数值、紧凑属性区、渐变等级进度、成长对比、按钮内进修消耗与横排依赖图标。免费进修在按钮内显示“无消耗”；物品/自定义条件仍显示实际说明；满级隐藏成长对比并使用金色进度。

研习从 `UnlockCondition` 读取需求，进修从 `GetConstellationUpgradeCondition` 读取需求，两者独立配置。`ConstellationRequirements` 展开嵌套组合条件，合并同类材料与研习点需求，按 KL 实际使用的背包范围统计持有量。详情逐项显示持有/需要/缺少数量，红色表示不足、绿色表示满足；按钮明确标注材料或研习点不足。背包数量变化会刷新详情，即使变化前后都仍不足。自定义条件沿用其判定和说明，并将独立的 SP 标记替换为“研习点”。

动态更新的研习条件会传给新创建的角色技能实例，避免界面显示一种费用而实际按构造默认值扣除。需求说明按实际行数排版；悬浮提示自动换行并限制在手札窗口内。

`Assets/NotebookSurface.png` 是从 HTML 的页面、星图柔光和详情背景 CSS 离线合成的无文字底图，1100×800 RGB，GPU 展开约 3.36 MiB。替换旧三张底图和详情单色遮罩的提交；由 ModContent 加载，无运行时生成/释放。依赖位置、节点、正文、进度、按钮数值均不烘焙。重新生成及采集浏览器参考：

```powershell
python Tools/ConstellationSkillPanelPreview/reference.py
```

截图与 CSS 实测尺寸写到 `.vissandbox/constellation-skill-panel/reference`。UI 保留项目的 HarmonyOS Sans SC / Noto Serif SC / Gelasio 字体；与浏览器的微软雅黑/系统宋体仍存在字形与字宽差异。
