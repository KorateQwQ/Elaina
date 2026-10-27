# 正式技能面板

默认 V 打开 `ConstellationSkillPanel`。旧 `ElainaSkillPanel` 与 `/constellationpreview` 演示保留，正式面板不读取演示 Skills.json。

详情页底部操作按钮固定为左右两列，每个 133×36 设计像素、间隔 10 像素；研习/进修/状态在左侧，装备/卸下在右侧。满级后进修按钮消失，左侧留空且不接收操作；右侧按钮保持原来的位置和宽度，绘制、悬停、点击共用同一固定区域。

右侧详情与配方手记共用 `System/UI/ElasticPageScrollView` 和 `ElasticScrollMotion`：隐藏竖向滚动条，文字与空白区域支持按住左键上下拖动，滚轮与拖动在上下边界均有弹性回弹。被动开关与关联技能图标保留点击操作，拖动释放不触发点击；切换技能回到顶部，失焦、关闭和缩放取消拖动。内容未超出详情窗口时不滚动。

`& Tools/ConstellationSkillPanelPreview/run.ps1 -ScrollOnly -SkipChecks -Configuration Release` 导出详情顶部/底部、滚轮/拖动的连续状态，覆盖 720p 与 125% UI 缩放。FNA 离线预览使用正式详情行和共用滚动状态；`-SkipChecks` 跳过现有通用业务回归，不跳过滚动采样断言（原因见预览工具 README）。完整 SUI 鼠标派发仍需游戏内验证。

外框保留原来的紫色细边和角花颜色，增加内嵌角花、左侧装订凹槽，以及右侧/底部三层同色系叠页，增强书本厚度；内容底图、布局与控件保持原样。边缘装饰最多伸出设计区域 13px，沿用原有 16px 屏幕留白。

选中节点左上方增加与 HTML 同源的蝴蝶：改选时约 420ms 飞到新节点，移动期间持续播放 16 帧扇翅；到达后至少继续 650ms，并完成当前翼拍后停在首帧。静止 4.5–6.2 秒后再扇动一轮。飞行使用星图坐标，随平移/缩放一致变换并接受视口裁剪；快速改选从当前位置接续，失焦暂停，关闭/退出世界重置，空分类隐藏。仅增加选中装饰，不改技能状态或鼠标命中。

`ConstellationButterflyMotion.cs` 管理动画，`ConstellationDrawing.NotebookPage` / `SelectionButterfly` 绘制边框与蝴蝶。`Assets/butterFly.png` 为原始素材，保持不变；`python Tools/ConstellationSkillPanelPreview/bake_butterfly.py` 从其 Alpha 生成与 HTML mask 一致的预乘 `ButterflyMask.png`，运行时交由 ModContent 加载。

专项验证：`& Tools/ConstellationSkillPanelPreview/run.ps1 -OrnamentsOnly`。输出包含实际边框/选中绘制在 720p、100% / 125% UI 缩放下的 FNA 离线画面，以及蝴蝶移动、落定、静止、间歇翼拍连续帧；状态检查覆盖 30/60/144 FPS、快速改选、空分类重置与暂停。正式游戏输入和开关生命周期仍需编译重载后验收。

手札只通过绑定键（默认 V）开关，原技能栏右上方的手札按钮已删除；该位置（`ConstellationDiscoveryToastUI.DockBounds()`）仍是开合动画与炼金手札的飞出/收回锚点。打开约 560ms、关闭约 440ms：书从锚点飞出，带弧线、倾斜和 32 段连续弯曲的双面封面；关闭时反向收回。快速按 V 可在中途反向，保持位置、缩放与曲率连续。动画期间根控件接收鼠标以防穿透，子控件与技能栏禁用鼠标操作；完全展开后恢复原生 SUI 布局和命中。

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

## 技能四态与显现提示

`ConstellationState.Status` 把每个技能分为四类：

- **已习得** `learned`：角色已学会。
- **待研习** `ready`：当前阶段能凑齐条件，只是还缺材料、研习点、前置技能或某个已开放的事件（例如花后才开放的霜月女王）。前置技能未学但本身可学，也算待研习；详情把未学前置列为第一条缺口。
- **待启封** `locked`：同样可达，但 `IsConstellationRevealed` 为 false 的问号技能，详情显示 `ConstellationRevealHint`。
- **未显现** `hidden`：当前阶段不可能学会，例如角色等级上限不够、需要的事件尚未开放，或前置技能本身未显现、依赖成环。

阶段门槛由 `StageGateUnlockCondition`（`ElainaModSkills/SkillStageGate.cs`）表达，它分开“已满足”与“当前阶段可达”。组合条件里任何一个门槛不可达，技能即未显现；可达但未满足的门槛显示为缺口。`ElainaSkill` 的现有辅助方法都返回门槛：

- `RequireCharacterLevel` / `RequireConfiguredCharacterLevel`：等级达到即满足，等级上限达到即可达。
- `RequireCharacterLevelCap` / `RequireConfiguredCharacterLevelCap`：上限达到即满足且可达。
- `RequireSkillLevel<T>`：按当前等级上限推算 T 最多能升到的等级。
- `RequireEvent(completed, available, key)`：Boss/事件；已完成或已开放即可达。

新技能写条件时用这些辅助方法即可，不要再用 `SkillUnlockCondition.Custom` 写阶段判断，否则技能会一直显示为待研习。

`ConstellationDiscoveryPlayer` 每 30 帧比对一次状态：未显现→待研习/待启封为“星图显现”，待启封→待研习为“星图启封”。记录随角色保存；新角色、旧存档的第一次观察和新增技能只记录不提示。提示由 `ConstellationDiscoveryToastUI` 画在原手札按钮的位置：同类变化合并为一条，淡入 0.25 秒、停留 2.6 秒、淡出 0.4 秒，悬停时保持，点击打开手札并选中第一个技能；手札打开时暂停。星图中这些技能带金色脉冲标记，选中或习得后消失。

离线验证：`run.ps1 -DiscoveryOnly` 运行四态、门槛、追踪器和提示队列检查，并导出提示横幅到 `output/<culture>/discovery`。

## 手动编辑布局

Debug 构建且存在本模组源码目录时，底部点击 **编辑模式** 后，可以拖动任意技能图标；连线实时跟随，底部显示当前节点的 X / Y。编辑时全部节点正常显示，不受技能分类的淡化效果影响。Release 构建隐藏编辑入口，只读取模组包内的布局。

- 拖动空白处平移；滚轮缩放；按住 Shift 拖动图标，按 10 个设计像素对齐。
- **保存**：直接写入项目内的 `Layout.json` 并退出编辑，下次进入世界仍会应用。源码目录不可用或保存失败时保留编辑草稿，不回退保存到本机配置。
- **取消** 或 Esc：恢复进入编辑前的位置。直接关闭面板、按 V、退出世界也会取消未保存的修改。
- **恢复默认**：将草稿恢复为当前模组包内的布局，仍需点击保存才写回项目；也可以取消这次恢复。
- 编辑模式只调整位置，研习、进修、被动开关及清空槽位暂不可操作。

保存路径由 tModLoader 的 `Mod.SourceFolder` 定位，指向项目中受 Git 跟踪的资源：

```text
ElainaModSkills/ElainaSkillUI/ConstellationSkillPanel/Layout.json
```

它是随模组发布的统一布局，不写入角色或世界存档，也不读取旧的 `ModConfigs/Elaina/ConstellationLayout.json`。以技能类型全名作为键，坐标是星图坐标，不包含分辨率、UI 缩放、镜头平移与缩放。JSON 例如：

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

**定稿发布**：编辑器保存已直接更新本目录的 `Layout.json`，无需手动复制。将该文件提交并推送即可同步远端；重新构建并发布模组后，玩家统一使用包内布局，旧本机配置不会覆盖新版。保存不会自动执行 Git 推送、重新打包或上传创意工坊。新增技能在 JSON 中没有记录时，沿用 `ConstellationPosition` / `SkillUIInfo` 的代码默认位置。

Debug 开发环境重新进入世界时会读取源码 `Layout.json`，方便继续编辑尚未重新打包的布局；源码文件损坏或版本不支持时提示并回退到包内布局，原文件不会因读取失败而被覆盖。Release 始终只读包内资源；包内布局无效时提示并回退到代码位置。检查实际发布布局可使用 Release 构建，或在 Debug 中点击“恢复默认”查看当前包内布局。

## Debug 技能操作

以下按钮与编辑入口一样受 `#if DEBUG` 限制；Release 不创建按钮，也不包含调试状态修改方法。

- **获得100研习点**：增加当前角色的 `SkillPoint`，达到 int 上限时饱和，避免溢出。
- **升级／升满级**：提升一级或直接升至 `GetMaxLevel()` 返回的当前上限，经验归零；新等级按每级 10 点、每逢 5 级额外 10 点发放尚未结算的研习点。不推进角色 Boss 进度或世界击杀状态。
- **增加上限**：推进至下一个 Boss，并提前补齐到新上限的尚未结算等级研习点。例如 Lv5 已领完、上限 5→10 时增加 60 点；角色仍为 Lv1 时会连同初始未领等级补齐至 110 点。同上限阶段不发点，重复预览不发点。之后升级到已提前结算的等级不重复领取；提示显示预计补发和实际到账点数。
- **精通技能**：先升满级，再学习满足配置中 `unlockStageCap`（角色等级上限）和 `unlockLevel`（实际角色等级）的星图技能（包括隐藏节点和缺少解锁条件的节点），直接将技能等级提升至当前 `MaxLevel`。保留角色等级与等级上限门槛，无视 Boss、前置技能及其他自定义研习条件，不调用可能拒绝升级的 `TryLevelUp()`，不消耗材料和研习点。未加入星图的技能不自动学习；角色和技能的动态等级上限仍沿用当前进度。保留装配、冷却、资源余额、被动开关及已超过当前上限的旧等级；重复点击无额外消耗。未达到等级门槛的技能不会被学习或升级；例如冰锥术需要等级上限至少 40 且实际等级至少 36。提升进度后可再次点击以补学技能和补齐等级。
- **重置**：清除当前角色技能玩家中的全部学习记录（包含未加入星图的技能）、等级、冷却与装配，调用已有 `LockSkill` 回调，刷新技能栏并清空当前技能索引。未学习节点恢复注册模板，包括隐藏状态。研习点余额清零，等级奖励领取记录回到 1 级，重新升级可正常获得研习点。保留背包物品和星图位置，不返还已花费的材料或点数。

编辑布局期间，调试操作按钮让位于保存/取消/恢复默认；隐藏控件同时退出命中检测。按钮会修改角色真实技能状态，正常保存角色时持久化。关闭世界或切换角色后，旧面板状态无法执行这些操作。

2026-10-28：正常经验升级与调试升级共用研习点结算，`studyPointRewardLevel` 随角色保存。旧存档历史等级不追补，从后续新等级开始发放；调试重置同时清空余额并重置结算等级，重新练级正常发放。精通技能中的角色升级也会补发尚未结算的等级点数，学习及进修技能本身仍不收费。

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
- `GetConstellationUpgradeCondition(int nextLevel)`：默认只消耗研习点×20；肉前技能按 `Tools/MageBalance/PROGRESSION-PLAN.md` 覆写为 5／10／15 点，不消耗木材。返回 `SkillUnlockCondition.None` 或 null 时，已习得且未满级可免费进修。配置物品、研习点或自定义条件后，检查并消费相应资源，再调用 `TryLevelUp()`。请同步实现实际等级效果。

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

`reference.py` 生成底图后会调用 `feather_surface.py`：星图柔光在旧视口顶 y=208 处被裁出一道约 3 级亮度的台阶，星图可见区上提后它读成分割线；脚本逐列量出台阶并在 80px 内渐入抹平，保留纸面颗粒，可重复运行。

## 星图背景（2026-10-20，对应 `Tools/SkillPanelRedesign/index.html`）

- 可见区 `Sky`（26, 150, 727×558）比相机框 `Map*`（y 208..681）大，上到页签点线、下到底栏点线；`ConstellationDrawing.SkyEdgeFade` 用底图本身逐像素条带重新覆盖四边（上 44 / 下 30 / 左 14 / 右 10），复刻 HTML 的 CSS 遮罩。缩放胶囊（`DrawZoomCapsule`）与布局编辑提示因此改在 `DrawMap` 末尾绘制。相机取景、聚焦与平移边界仍用 `Map*`。
- 旧天文星盘（同心圆、斜椭圆、72 格刻度）已删除，只留三颗装饰星。
- `ConstellationSky`：150 颗固定星点各自 35%～100% 呼吸，约十分之一定时亮出小四角星芒，每 6～14 秒一颗流星；参数、种子与 HTML 一致，全部是页面坐标，不随相机移动。
- 立绘：伊蕾娜捧手札站在星图右下，固定在页面上，画在星点之上、连线与节点之下；书页处一抹微光和上升的光点。只用夜色版（线稿版仅保留在 HTML 预览里对比）。`Assets/PortraitNight.png` 由 `python Tools/SkillPanelRedesign/build_art.py --game` 从 `ElainaModSkills/` 下的原图生成：渐隐遮罩已烘焙、裁掉透明边、1.5× 设计尺寸、预乘 Alpha，打印的矩形对应 `ConstellationDrawing.PortraitArea`；换原图后重跑并核对该矩形。
