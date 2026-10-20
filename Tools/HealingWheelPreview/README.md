# 治疗目标选择盘

正式入口仍是魔杖的治疗魔法。不存在其他存活同队非 PvP 队友时直接对自己施法；存在这样的队友时（即使全部在范围外），按住左键打开并保持选择盘，松开按当前方向确认。自己与队友均在外环；拖出外圈仍按方向选择，回到中心则清空选择并显示“松开取消”。右键或 Esc 也可取消。长按导致魔杖自动重用时保留当前选择，不重复打开、扣费或施法。有可选队友时按下只打开选择盘（`HealSkill.BeginWandUse` 拒绝魔杖的原版使用），选择期间没有挥杖与使用音效；松开确认目标后才扣费并由施法动作出手。取消后若按键未松开，也要等松开后才能再次打开。

开合节奏与普通技能盘共用 `SkillWheelDrawing` 的曲线：打开时环带回弹展开、目标依次飞出；悬停高亮平滑过渡，其余目标随之压暗，圆心信息随焦点切换淡入。松开确认后选中目标原地闪光并扩散细环，星环光波播完再消失；取消时全部向内收拢淡出。淡出期间根节点只保留绘制，不再接收点击，名字与血量冻结为最后一帧。

候选采用 3000 像素范围、同队、非 PvP、存活条件，最多 8 人，始终保留自己。血量和最大血量在绘制时读取；候选每十帧检查，仍在候选集内的玩家不会因走动而交换扇区。目标失效、切换装备、失去焦点或离开世界时不会继续确认旧目标。实际扣费与施法仍由 `HealSkill` 完成。

正式代码：

- `ElainaModSkills/ElainaSkillUI/ElainaHealingWheel.cs`：原生 SUI 根与扇区命中控件，真实玩家头部绘制、输入、候选与生命周期。
- `ElainaModSkills/ElainaSkillUI/HealingWheelDrawing.cs`：头像、姓名、血条、血量和圆心信息的布局绘制。
- `ElainaModSkills/ElainaSkillUI/SkillWheelDrawing.cs`：与普通技能盘共用星点、环带、法阵、指针；新几何参数为零时保留原来的普通技能盘尺寸。
- `ElainaModSkills/Skills/Heal/HealSkill.cs`：单人直接施法与最终目标/魔力校验。
- `ElainaModSkills/Skills/Heal/HealProj.Healing.cs`：十秒治疗计时、血量上限与各端本地治疗。

治疗弹幕参数为 `ai[0] = 总治疗量`、`ai[1] = 目标玩家编号`、`ai[2] = 已经过的更新帧数`。技能目前传入测试值 100；弹幕存在 600 帧，每 60 帧结算一次，最后一跳在第 600 帧。各端执行同一回血逻辑，仅本地显示实际回血量，不发送额外治疗包。无效、死亡或离线目标会终止弹幕。

## 可复现验证

在模组目录运行：

```powershell
& Tools/HealingWheelPreview/check-behavior.ps1
& Tools/HealingWheelPreview/run.ps1
python Tools/LocalizationChecks/check.py
dotnet build 伊蕾娜.csproj -t:Compile -p:BuildProjectReferences=false
```

行为检查直接链接正式 `HealSkill.cs` 与 `HealProj.Healing.cs`，以引擎、动作系统与选择盘适配桩验证 132 项断言：单人、可选队友、长按重复调用、魔杖入口只在确认后出手、距离边界、PvP、死亡/离线、取消、过期回调、技能移除、禁用魔力、魔力不足、自选治疗、参数传递、三种网络模式下的十次治疗、血量上限、本地跳字及已有计时恢复。魔力扣费按技能当前消耗验证，不固定旧测试值。它不模拟真实冷却计时、SUI 事件树、网络传输或玩家血量同步。

FNA 离线预览直接链接正式绘制代码和 KL 的 NotoSerifSC 字体；输出在 `.vissandbox/healingwheel-preview/output`。检查 2、6、8 人、长名字、中英文、中心取消、盘外选择与 1.5 倍 UI 缩放，打开、松开确认、松开取消三段动画的抽帧，以及 0.3/0.5/1/1.5 倍下的扇区、盘外方向和返回中心的命中计算。头像适配使用模组现有像素图的裁切示例；真实玩家装备、染料与第三方头部图层由游戏的 `DrawStaticPlayerHead` 负责，需进游戏检查。预览用 BasicEffect 代替 KL 的扇区 shader，不是游戏截图。

所有工具源码使用 `.cs.txt`，生成工程位于默认编译排除的 `.vissandbox`；`build.txt` 已排除 Tools 和 .vissandbox，不把检查工具打入模组。
