# 冰凌锁提高耗蓝的资源与伤害预算

2026-10-19。只试算，未修改正式配置。当前文件冰锥耗蓝22.5，冰凌锁耗蓝100，水球耗蓝150；冰锥积蓄仍为2.8、水流冰凌锁返还仍为0.15，冻结资格机制尚未接入。

本报告均以前面讨论的完整新循环为前提：冰锥5.6积蓄、水流0.30技能CD返还、冰凌锁需要冻结资格；两项基础冷却仍60秒。不能将结果当作只改当前cost后游戏就会出现的效果。

## 预算联动

当前主动技能伤害与耗蓝相乘，cd60、rate4、cdRatio1下，伤害预算=B×0.12×cost×k。

| 耗蓝 | 单次预算 | k | 含义 |
|---|---:|---:|---|
| 100 | 21.2B | 1.7666666667 | 当前基准 |
| 200 | 21.2B | 0.8833333333 | 只加消耗 |
| 250 | 21.2B | 0.7066666667 | 只加消耗 |
| 200 | 26.5B | 1.1041666667 | 消耗提高，单次伤害增加25% |
| 250 | 31.8B | 1.06 | 消耗提高，单次伤害增加50% |
| 250 | 53B | 1.7666666667 | 只改cost不改k的结果，伤害为当前2.5倍 |

250魔力占肉后/双子630魔力池的39.7%、花后640魔力池的39.1%。六轮冰锥135魔力，加250魔力冰凌锁，共支付385魔力；这是毛支出，施法过程仍有自然回复。再加一发水球，毛支出535魔力。

## 肉后两分钟比较

| 方案 | DPS | 冰凌锁占比 | 冰凌锁次数 | 水球次数 | 补魔次数 |
|---|---:|---:|---:|---:|---:|
| baseline100 | 1348 | 41.4% | 5 | 8 | 1 |
| cost150 | 1348 | 41.4% | 5 | 8 | 2 |
| cost200-fixed | 1348 | 41.4% | 5 | 8 | 3 |
| cost250-fixed | 1348 | 41.4% | 5 | 8 | 4 |
| cost300-fixed | 1348 | 41.4% | 5 | 8 | 5 |
| cost200-boost25 | 1488 | 46.9% | 5 | 8 | 3 |
| cost250-boost50 | 1628 | 51.4% | 5 | 8 | 4 |

## 250耗蓝、31.8B的阶段效果

| 阶段 | 时长 | DPS | 冰凌锁次数 | 水球次数 | 冰凌锁平均间隔 | 补魔次数 |
|---|---:|---:|---:|---:|---:|---:|
| 肉后初期 | 120秒 | 1628 | 5 | 8 | 27.2秒 | 4 |
| 肉后初期 | 300秒 | 1298 | 9 | 14 | 32.1秒 | 5 |
| 双子后 | 120秒 | 2135 | 5 | 8 | 27.2秒 | 4 |
| 双子后 | 300秒 | 1703 | 9 | 14 | 32.1秒 | 5 |
| 花后 | 120秒 | 5170 | 5 | 8 | 27.2秒 | 4 |
| 花后 | 300秒 | 4133 | 9 | 14 | 31.3秒 | 5 |

## 资源取舍

250耗蓝会让水球从固定每15秒必放转为需要选择资源窗口。肉后五分钟为冰凌锁留蓝的策略约1298 DPS、9次冰凌锁、14次水球；继续贪每个可支付水球约1173 DPS、6次冰凌锁、18次水球。两者都耗尽5次补魔。

该参数下完全不用水球的三技能核心，在肉后五分钟约1297 DPS、11次冰凌锁，只用2次补魔。这意味着水球在长战中的资源竞争已很明显，不能声称四技能每CD释放仍是最优。短战水球仍增加爆发；更精细的择机水球策略未做最优化搜索。

只提高耗蓝而保持21.2B，肉后五分钟留蓝策略约1097 DPS，低于100耗蓝基准约1282 DPS；250耗蓝+50%单次增伤则约1298 DPS。两分钟时后者却提高约21%总DPS，因此它是更集中的短战爆发，不能简单称为全时长平衡不变。

300耗蓝会让完整两分钟循环使用全部5次补魔，长战更依赖停用水球，第一版不建议直接采用。

## 限制与复现

- Saved ice cost is22.5; saved lock cost is100, not150. Saved freeze buildup2.8 and beam lock refund0.15 are not the proposed values modeled here.
- All scenarios assume proposed freeze permission plus60s skill cooldown, target freeze cooldown60s, freeze4s, ice5.6 buildup, beam lock refund0.30.
- Ice damage1.2B; ball mana150, k1.5, formal15s cooldown. No gameplay files changed.
- Stage8/10/13 levels40/50/65 and saved gear; full starting mana, five50%-mana elixirs; no life conversion, incoming damage, buffs or armor procs.
- Single zero-defense persistent freezable target, full hits, expected standard crit. Not actual boss DPS.
- greedyBall always casts affordable ball after lock priority. saveForLock reserves ready/nearly-ready earned lock budget and uses water to recover when needed.
- noBall isolates three-skill core. These fixed policies are not globally optimized.
- Damage budgetB is independent scenario input; k is adjusted from the actual pricing formula. Keeping k unchanged while raising cost scales damage proportionally.

采用同阶段满技能、满初始魔力、最多5次补魔、单体零防御全中模型，不计生命转魔力、受伤、护盾治疗与装备追击，非游戏Boss实测。运行 `node Tools/MageBalance/experiments/freeze-charge-stages-2026-10-19/finisher-mana.cjs` 可复现。
