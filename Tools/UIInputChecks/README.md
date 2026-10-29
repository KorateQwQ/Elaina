# 原版 UI 点击穿透检查

运行 `Tools/UIInputChecks/run.ps1`（PowerShell）。直接链接实际输入隔离代码和本机 tModLoader；不会创建世界或修改玩家存档。

同时链接正式 `ElasticPageScrollView` / `ElasticScrollMotion` 与真实 SUI 控件树，验证详情行滚轮冒泡、滚动位置实际写入容器、缩放、反向滚动、事件消费、拖动后继续滚轮、失焦与禁用、短内容。使用真实经过时间配合零值缓存 `GameTime`，防止再次把绘制阶段滚动动画绑定到游戏更新间隔；旧实现会在此检查失败。离线固定 1/60 秒的动画预览不能覆盖这类问题。

相关历史：`f5a9552`（2026-09-21）将技能面板控件改为独立时钟；`e529513`（2026-10-20）修复技能轮盘计时，但同次新增的弹性滚动控件仍读取缓存游戏间隔；`9a9022d`（2026-10-27）再次修复玩偶轮盘的同类问题。SUI 绘制阶段的交互动画应按独立经过时间推进，并覆盖零值/重复缓存帧时间的检查。

入口先扫描技能 UI、配装、玩偶、炼金 UI 和通用 UI 目录，发现实际代码使用 `ElapsedGameTime` 或 `Main.gameTimeCache` 时直接失败（说明性注释不触发）。运行时另验证共用时钟的活动/暂停/恢复，以及真实 `UIAnimationClockSystem` 在 F11 隐藏期间完全没有绘制回调时仍排除隐藏时长、世界重置时废弃旧采样。99 项检查通过。技能槽零帧时间下的选中动画与重复采样、失焦由 `Tools/ResourceDisplayChecks/run.ps1` 检查；治疗/回城、普通技能及玩偶轮盘由 `Tools/SkillWheelPreview/check-timing.ps1` 检查。

检查左右键/中键拖出面板、释放帧、多个按键同时按下、失焦/隐藏/卸载清理；检查 100%、125%、150%、200% UI 缩放下的输入屏蔽与恢复、嵌套调用和异常恢复，并用真实 UserInterface/UIElement 验证鼠标按下和滚轮事件。另检查任意模组的新 SUI、继承注册、全局 SUI、图层前后关系、其他模组后续调整图层、隐藏/移除图层和重复界面名称。

兼容系统跟随伊蕾娜模组加载，但使用 SUI 的 HoverTarget 自动识别所有模组的根界面，不再维护界面类型或模组名单。新界面照常使用 RegisterUI/RegisterGlobalUI 和 SUI 命中机制即可；纯装饰区域按 SUI 的 IgnoreMouseInteraction/DisableMouseInteraction 规则处理。只屏蔽排在该 SUI 下面的受支持原版交互层，保留位于上层的原版界面输入。

游戏内重载后仍需检查：让技能星图/新版炼金书覆盖已打开的背包、箱子、装备栏和合成区域，验证左键、右键长按、Shift/Ctrl 点击与拖出面板均不操作底层物品；移出面板再开始的新点击应正常。另检查翻书动画、帮助层、关闭按钮、旅途模式，以及打开原版设置和 F11 隐藏 UI 后不会残留输入屏蔽。

实现依据：Main.DrawInventory 在绘制阶段直接调用 ItemSlot.LeftClick/RightClick，并不检查 Player.mouseInterface；该标志不能充当 UI 层间的事件消费标记。兼容系统在指定原版层的 DrawSelf，以及原版 UIState 的 Update 内短暂屏蔽输入，用 IDisposable 确保每次恢复。
