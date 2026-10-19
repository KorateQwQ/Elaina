# 冰锥增伤与技能切换收益

2026-10-19。只进行模型试算，未修改正式参数。水球按本轮JSON的k=1.5。配置SHA256：`51a23d92dd6c8958fc8fad8ef90ab7740c69af8198413ee6fa15d42b3cc56dba`。

所有结果均使用拟议的“冻结获得一次冰凌锁”规则；当前参数组仅指冰锥数值，不代表当前游戏内已经实现该机制。

## 当前冰锥参数，120秒

| 阶段 | 冰锥系数 | 冻结后转水 | 冻结期间继续冰锥 | 纯冰锥 | 冰锥＋锁 | 冰锥＋球＋锁（不用水流） |
|---|---:|---:|---:|---:|---:|---:|
| 巨鹿后 | 1.2B | 464 | 488 | 421 | 421 | 539 |
| 肉后初期 | 1.2B | 1270 | 1306 | 756 | 970 | 1184 |
| 花后 | 1.2B | 4033 | 4148 | 2404 | 3083 | 3761 |

## 高耗蓝、快速冻结参数，120秒

每轮22.5魔力、每发5.6积蓄；数值为DPS。

| 阶段 | 冰锥系数 | 冻结后转水 | 冻结期间继续冰锥 | 纯冰锥 | 冰锥＋锁 | 冰锥＋球＋锁（不用水流） |
|---|---:|---:|---:|---:|---:|---:|
| 巨鹿后 | 1.2B | 448 | 478 | 421 | 421 | 453 |
| 肉后初期 | 1.2B | 1352 | 1395 | 756 | 970 | 1110 |
| 花后 | 1.2B | 4294 | 4430 | 2404 | 3083 | 3573 |
| 巨鹿后 | 1.5B | 467 | 515 | 523 | 523 | 533 |
| 肉后初期 | 1.5B | 1388 | 1458 | 947 | 1158 | 1271 |
| 花后 | 1.5B | 4408 | 4631 | 3009 | 3680 | 4095 |
| 巨鹿后 | 2B | 501 | 580 | 702 | 702 | 675 |
| 肉后初期 | 2B | 1447 | 1563 | 1264 | 1471 | 1539 |
| 花后 | 2B | 4596 | 4963 | 4012 | 4671 | 4960 |

## 高耗蓝参数，300秒

| 阶段 | 冰锥系数 | 冻结后转水 | 冻结期间继续冰锥 | 纯冰锥 | 冰锥＋锁 | 冰锥＋球＋锁（不用水流） |
|---|---:|---:|---:|---:|---:|---:|
| 巨鹿后 | 1.2B | 448 | 438 | 309 | 309 | 315 |
| 肉后初期 | 1.2B | 1329 | 1264 | 628 | 735 | 711 |
| 花后 | 1.2B | 4223 | 4042 | 2019 | 2367 | 2283 |
| 巨鹿后 | 1.5B | 466 | 468 | 384 | 384 | 379 |
| 肉后初期 | 1.5B | 1364 | 1322 | 786 | 887 | 842 |
| 花后 | 1.5B | 4331 | 4225 | 2527 | 2855 | 2706 |
| 巨鹿后 | 2B | 498 | 521 | 516 | 516 | 493 |
| 肉后初期 | 2B | 1421 | 1420 | 1049 | 1139 | 1061 |
| 花后 | 2B | 4512 | 4528 | 3370 | 3665 | 3408 |

## 资源代价（高耗蓝参数，120秒）

| 阶段 | 冻结后转水用药 | 冻结期间打冰锥用药 | 纯冰锥用药 | 冰锥＋锁用药 | 不用水流用药 |
|---|---:|---:|---:|---:|---:|
| 巨鹿后 | 1 | 3 | 5 | 5 | 5 |
| 肉后初期 | 2 | 3 | 3 | 4 | 5 |
| 花后 | 1 | 2 | 3 | 4 | 5 |

## 使用技能的直接机会成本

同基准B且全中时，水球k1.5整次预算6.75B；约1.37秒动作在冰锥1.2B下替代约1.64B直接伤害，尚有约5.11B净伤害收益。冰凌锁整次21.2B、占手0.75秒，替代约0.9B冰锥伤害。其约1.83秒攻击过程可以与之后的攻击重叠，不是1.83秒全程占手。以上尚未扣除额外魔力的机会成本或防御。

提高冰锥系数最先威胁水流的使用价值，而非冰凌锁。肉前没有冻结换冰凌锁奖励，水流加速下一次冻结主要带来控制，其输出竞争更脆弱。高耗蓝对长期续航有约束，但玩家在短战能用初始魔力和药剂换输出。不能将“高耗蓝”视为任意增伤的保障。

- All scenarios use proposed freeze-to-charge lock availability, not current runtime cooldowns.
- Current profile: saved ice cost15/buildup2.8; proposed profile: cost22.5/buildup5.6. Damage ratio swept independently.
- Same saved stage7/8/13 loadouts, levels35/40/65, capped skill levels; lock unavailable before hardmode.
- Single zero-defense persistent target, all hits land, expected crit, no armor procs; five50%-mana elixirs, no life conversion.
- Cycle means ice for buildup, water otherwise; cycleFrozenIce also uses ice during the4-second freeze.
- iceOnly never casts active skills; iceLock skips both water skills; iceBallLock skips only water beam.
- These are representative fixed strategies, not a search for the global optimum. Scarcity may delay or skip expensive skills.
- Short-fight results depend on the phase at which the fight ends. No boss HP, movement, defense or phase invulnerability is modeled.
- See run.cjs and REPORT.md for timing, resource, and freeze assumptions.
