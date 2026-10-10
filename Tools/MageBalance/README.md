# 法师配装成长实验室 · 通用技能配置 v2

## 使用与保存

打开 `index.html`。普通攻击设计区可以添加飞弹、冰锥等条目；主动技能区使用魔力转化定价。页面可直接计算基础 DPS、整次/每发伤害，以及附带配装与资源模拟结果。

- 双击 HTML：点击“保存到模组”，首次选择模组 `ElainaModSkills/Data/SkillBalance.json` 并授权，之后直接保存到同一文件。使用支持 File System Access 的 Chromium 浏览器；授权失效时再次授权。可点击配置文件信息旁的“更换…”重新选择；更换目标不会自动读取或写入。
- `run.ps1` 启动的本机服务页面：仍然可以直接保存固定路径，不需要选择文件。
- 主工具栏仅保留“保存到模组”“重新读取”和“更多”。重新读取会用文件内容替换页面参数，包括尚未写入的修改。
- “更多 → 恢复默认…”需要确认，只重置页面及浏览器缓存，不自动写入模组文件。
- 移除试算文件的导入／导出入口和结果下载；保留实时 DPS、伤害、成长和战斗模拟。需要文件备份时直接复制配置 JSON。
- 本次仅精简操作入口，不改变保存格式：模组 JSON 仍含整份页面配置，游戏只读取需要的参数；不保存计算结果和战斗日志。文件选择、读取和写入失败时会显示原因，不误报保存成功。

## 技能等级成长区间

普通攻击与主动技能都可以独立配置：

- `unlockStageCap`：技能进入哪个角色阶段后开放。阶段跨度由 `progression.phaseSize` 控制，默认 5；飞弹默认 5，冰锥示例默认 10。
- `unlockLevel`：技能 Lv1 对应的角色等级。默认冰锥为 6，表示史莱姆王后的 6–10 级区间开始即可升到 Lv1；如果设计为必须满 10 级才解锁，把它改成 10。
- `levelsPerPhase`：每个完整角色阶段新增多少级，不包含解锁时获得的 Lv1。飞弹和冰锥默认都是 2。

因此默认节点为：飞弹 `Lv1@角色1、Lv2@角色3、Lv3@角色5、Lv4@角色8、Lv5@角色10`；冰锥 `Lv1@角色6、Lv2@角色8、Lv3@角色10、Lv4@角色13、Lv5@角色15`。页面会按本地 KL 等级 DPS 曲线显示每级基准 B、整次伤害、每发伤害、整数取整结果和相对上一级提升，并标出当前预览角色等级能达到的技能等级。

C# 已自动接入这些节点：角色实际等级限制技能可升级上限；技能实际等级映射到节点对应的角色等级，再由 KL 曲线取得伤害基准 B。例如飞弹 Lv2 使用角色 3 级对应的 B，即使玩家已到角色 10 级，只要技能未升级，基础伤害就不变。装备增伤仍按玩家当前装备计算。

上限不会自动提升技能实际等级，也不会裁剪已有等级。首级角色门槛与 Boss 解锁条件仍需在解锁规则中判断；KL 的 `MaxLevel` 至少为 1，不能用它代替首次解锁判断。节点生成到角色 1000 级，超出节点范围的技能等级使用最后一个节点。

## 唯一配置格式

使用 `schema: elaina-mage-balance-v2`、`configuration.version: 2`。不再保留 `autoRatio`、`combat.autoCost`、`combat.autoInterval` 或 v1 兼容路径。

- `basicAttacks`：每项包括 `id`、`name`、`ratio`、`cost`、`interval`、`shots`、`unlockStageCap`、`unlockLevel`、`levelsPerPhase`。
- `skills`：主动技能每项包括 `id`、`name`、`cost`、`cd`、`cdRatio`、`cast`、`k`、`shots`、`unlockStageCap`、`unlockLevel`、`levelsPerPhase`，以及仅供网页模拟的启用、命中率、持续时间、充能等参数。`cdRatio` 缺省时按 1 读取，以兼容旧配置。
- 所有技能 ID 全局唯一。飞弹不是解析器特例，可以与其他技能一样修改/删除；至少保留一项普攻。
- `selectedBasicAttackId`：模拟中使用的普攻，也用于主动技能占手补偿定价；不会把所选普攻的参数覆盖到另一个技能 ID。
- 网页的阶段 B、配装和 `skillStats` 是试算数据；游戏用玩家真实等级限制升级，用技能实际等级映射伤害基准，并应用玩家实际装备。

## 定价

普攻：`整次基础伤害 = 等级基准B × ratio × interval`；`每发基础伤害 = 整次基础伤害 / shots`。

例如 B=1000、0.8B、0.5 秒、3 发、整次耗蓝 10：DPS=800，整次=400，每发≈133.33，每秒耗蓝=20。

主动：`整次基础伤害 = 参考普攻基础DPS × max(1/60, cast) + B × (rate / 100 / 20) × cost × cd × cdRatio × k`。`cdRatio` 是 CD 伤害折算率，范围 0–1；它只影响由 CD 换算出的额外伤害，不改变实际 CD、充能或技能释放时机。默认 1（100%），功能型长 CD 技能可设为 0.25–0.75。`shots` 对整次预算做等额分摊，不改变整次总预算。

发数必须与实际命中设计一致，配置不会自动生成弹幕或改变动作。游戏伤害最终按整数处理，小伤害/大量分段可能有取整误差。范围、多目标、防御、命中率、暴击与实际动画不是基础预算的一部分。网页沿用裸装 B 含基础暴击期望的口径；C# 返回的基础 DPS 不预乘暴击，不应把网页带装备的期望伤害直接作为弹幕 damage。


战斗模拟的释放策略包括按优先级、缺蓝攒蓝，以及“节约型”。节约型把普通攻击也作为候选动作，按预计总伤害除以耗蓝比较；只有技能性价比高于普通攻击时才优先释放技能，同效率时优先 CD 更长者。它只改变模拟释放顺序，不改变技能伤害、实际 CD 或耗蓝。高性价比技能暂时无法支付时继续普通攻击，普通攻击本身也无法支付时才等待回蓝；超过魔力上限的无效技能不会阻止其他技能。

## C# 统一读取

所有条目加载到 `SkillBalanceSystem.Current.Skills`，按 `SkillBalanceEntry.Kind` 区分定价公式，按 ID 查找，不按具体技能类分支。

### 在技能类内部：不传名称、玩家或伤害类型

飞弹、冰锥、水球都继承 `ElainaSkill`，使用同一套接口。父类默认按当前类名定位配置；网页的 ID 应与类名一致。配置存在时自动接入 `MaxLevel`，子类无需覆写；配置不存在时保留默认上限，特殊技能也可自行覆写 `MaxLevel` 或关闭 `UsesLiveBalance`。

```csharp
// 默认读取本技能的实际 Level，已经含装备增伤，不要再乘一次。
int damagePerProjectile = GetConfiguredDamage();
float baseDps = GetConfiguredDps(); // 基础周期 DPS，不含装备。

// 需要下一等级预览时显式指定；不修改已学习的 Level。
int nextDamage = GetConfiguredDamage(skillLevel: Level + 1);

// 需要耗蓝、周期和发数时，同样不指定名称。
if (!TryGetConfiguredBalance(out var balance))
    return;
int mana = balance.Entry.ManaCost;
double interval = balance.Entry.BaseCooldown;
int count = balance.HitCount;
int damage = balance.DamagePerHit;
int damageNode = balance.CharacterLevel; // 当前技能等级对应的伤害基准角色等级。
```

这两种取法选一种即可。默认按配置 `shots` 分摊；如果技能实际发数由代码确定，用 `GetConfiguredDamage(hitCount: actualCount)` 覆盖分摊数，不要在返回值上再除一次。未学习的技能实例默认按 Lv1 显示预览；底层查询显式传入 `skillLevel <= 0` 时不产生伤害。

### 在技能类外部：显式查询某个技能

只有工具、UI 或其他对象要查询某个技能时，才调用 `SkillBalanceSystem.TryGetPlayerBalance(player, skillId, skillLevel, damageClass, out var balance)`。这是上述实例接口的底层实现，不是冰锥专属用法；必须提供实际技能等级，玩家参数只提供装备增伤。

只看理论基础数值：`GetBaseDps(skillLevel, skillId)`、`GetBaseDamage(skillLevel, skillId)`。飞弹的静态 `GetDps(int)` / `GetDamage(int)` 参数也改为技能等级，不再是角色等级。主动技能的 DPS 是整次伤害除以 CD，包含占手补偿，不是净额外 DPS。

当前飞弹、水球和冰锥都通过父类实例接口读取配置。冰锥保留原有三发生成位置和动作，按实际三发分摊伤害；网页 `IceConeSkill.shots` 也应设为 3，以使每发预览一致。此次未改变动作时长或弹幕运动。

## 加载边界

配置仅在本地读取，不参与联机同步。所有构建加载 `.tmod` 内配置；仅 `#if DEBUG` 构建每 0.5 秒热读取源码 JSON。Release 无文件轮询，修改后需重新打包并加载。错误文件保留上次有效快照；新值不回溯改变已生成弹幕和进行中的冷却。

## 测试

```powershell
node test.cjs
node test.cjs --browser
node test-runtime.cjs
python test_server.py
```

浏览器测试使用隔离的 Chromium 会话，不操作用户浏览器配置。截图保存在忽略的 `test-output/`。

`test-runtime.cjs` 需要 .NET 8 SDK 或更高版本。它在临时目录编译实际 C# 配置与定价代码，对照 HTML 节点和伤害结果，验证 Debug/Release 路径、热更新、等级映射、装备增伤和多发分摊；不启动游戏。
