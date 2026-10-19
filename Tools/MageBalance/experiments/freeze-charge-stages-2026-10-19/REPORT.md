# 冻结换冰凌锁：三个阶段的伤害分配

2026-10-19。按拟议机制试算，未修改游戏逻辑或正式JSON。配置SHA256：`bb833cecaccfc8d0adce2b500f6ee89eb6fbc6debafc88dee382e708a3995362`。

## 配装与技能

| 阶段 | 等级 | 护甲 | 魔力 | 回蓝/秒 | 冰锥等级 | 水流等级 | 水球等级 | 冰凌锁等级 |
|---|---:|---|---:|---:|---:|---:|---:|---:|
| 巨鹿后 | 35 | 丛林套 | 540 | 15.8 | 7 | 5 | 3 | 未解锁 |
| 肉后初期 | 40 | 钛金法师套 | 630 | 17.6 | 9 | 7 | 4 | 2 |
| 花后 | 65 | 输出型幽灵套（面具＋长袍＋裤）· 不含追击 | 640 | 17.8 | 19 | 17 | 9 | 7 |

阶段配装逐项快照在results.json中；巨鹿后6槽中只有5件有效饰品。肉后初期指按保存配装准备好钛金套并完成计划中的暴雪解锁，非刚击败血肉墙尚未采矿的瞬间。各技能均升满阶段上限；肉后冰凌锁若仅Lv1，伤害会低于本表Lv2。

## 120秒：当前冰锥数值＋冻结换次数

冰锥1.2B、15魔力/轮、2.8积蓄/发；水球k=3。

| 阶段 | 冰锥 | 水流 | 水球 | 冰凌锁 | 总DPS | 冰凌锁次数 | 水球次数 | 药剂次数 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 巨鹿后 | 20.3% | 28.9% | 50.8% | 0.0% | 622 | 0 | 8 | 1 |
| 肉后初期 | 14.6% | 20.1% | 36.6% | 28.7% | 1554 | 4 | 8 | 1 |
| 花后 | 14.6% | 20.1% | 36.6% | 28.7% | 4936 | 4 | 8 | 1 |

## 120秒：建议冰锥数值＋冻结换次数

冰锥1.2B、22.5魔力/轮、5.6积蓄/发；水球k=3。

| 阶段 | 冰锥 | 水流 | 水球 | 冰凌锁 | 总DPS | 冰凌锁次数 | 水球次数 | 药剂次数 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 巨鹿后 | 13.0% | 34.9% | 52.1% | 0.0% | 606 | 0 | 8 | 1 |
| 肉后初期 | 8.7% | 22.5% | 34.8% | 34.1% | 1636 | 5 | 8 | 2 |
| 花后 | 8.7% | 22.5% | 34.8% | 34.1% | 5198 | 5 | 8 | 1 |

## 300秒：建议参数的持续战斗

| 阶段 | 冰锥 | 水流 | 水球 | 冰凌锁 | 总DPS | 冰凌锁次数 | 水球次数 | 药剂次数 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 巨鹿后 | 12.5% | 35.4% | 52.1% | 0.0% | 606 | 0 | 20 | 4 |
| 肉后初期 | 8.4% | 23.1% | 35.2% | 33.2% | 1614 | 12 | 20 | 5 |
| 花后 | 8.4% | 23.1% | 35.2% | 33.2% | 5126 | 12 | 20 | 5 |

## 建议参数的节奏与资源

| 阶段 | 首次冻结 | 冰凌锁平均间隔 | 平均耗蓝/秒 | 最低魔力 | 120秒结束魔力 | 首次用药 |
|---|---:|---:|---:|---:|---:|---:|
| 巨鹿后 | 5.8秒 | —秒 | 20.9 | 0.7 | 194.7 | 82.9秒 |
| 肉后初期 | 5.8秒 | 26.3秒 | 24.9 | 64.4 | 389.2 | 61.7秒 |
| 花后 | 5.8秒 | 26.3秒 | 24.9 | 14.8 | 113.2 | 61.7秒 |

开场优先使用水球，故首次冻结时刻包含水球占手；4.5秒是冰锥本身所需的六轮施法时间，不是本模型从开战到冻结的总时间。

## 冻结期间继续冰锥的操作对照

主表在冻结后转水系恢复魔力；以下在冻结4秒内继续冰锥输出，解冻后才转水系。

| 阶段 | 冰锥 | 水流 | 水球 | 冰凌锁 | 总DPS | 冰凌锁次数 | 水球次数 | 药剂次数 |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| 巨鹿后 | 24.0% | 26.3% | 49.7% | 0.0% | 636 | 0 | 8 | 3 |
| 肉后初期 | 14.9% | 18.0% | 33.9% | 33.2% | 1679 | 5 | 8 | 3 |
| 花后 | 14.9% | 18.0% | 33.9% | 33.2% | 5334 | 5 | 8 | 2 |

## 敏感性对照

| 场景 | 总DPS | 冰锥占比 | 水球占比 | 冰凌锁占比 | 药剂次数 |
|---|---:|---:|---:|---:|---:|
| 7-current-120-always-ice | 697 | 54.7% | 45.3% | 0.0% | 4 |
| 7-proposed-120-always-ice | 571 | 58.5% | 41.5% | 0.0% | 5 |
| 7-proposed-120-ball-k1.5 | 448 | 17.6% | 35.2% | 0.0% | 1 |
| 8-proposed-120-ball-k1.5 | 1352 | 10.5% | 21.0% | 41.3% | 2 |
| 13-proposed-120-ball-k1.5 | 4294 | 10.5% | 21.0% | 41.3% | 1 |

## 模型限制

- Design proposal only: own freeze grants one lock charge; cap one; no opening charge, natural lock cooldown, lock-CD refunds or 15-second reward limiter.
- Lock damage is deliberately held at the current 21.2B budget and cost100; the cd60 field is only used for existing damage pricing, not availability.
- Planned unlock order: no lock at stage7, lock learned through the planned blizzard unlock at stage8 onward. That event is not implemented by this model.
- Current ice uses saved JSON; proposed ice overrides cost22.5 and freezeBuildup5.6, retaining saved ratio1.2 and interval0.75.
- Stage7/8/13 use levels35/40/65, saved loadouts, capped learned skill levels, full starting mana, Ashen Witch on.
- Runtime regeneration5+2% maximum mana; up to five50%-mana elixirs, except no-potions rows; no life conversion, shields, healing, incoming damage or extra buffs.
- Single persistent zero-defense target; expected standard crit, all attacks land, no armor procs including spectre or titanium procs.
- WaterBall uses current k3 except explicitly labeled k1.5 sensitivity; formal15-second cooldown, one single-target hit, no absorption/splash/terrain effects.
- 45-frame ice/lock action, 20-frame water recovery, approximate82-frame minimum ball action; fixed short travel, no target motion or phase invulnerability.
- Priority lock > affordable ball > ice when freeze buildup is allowed > water otherwise, including frozen duration. Extra rows test ice during frozen duration or always-ice filler.
- Freeze240 frames, then cooldown3600 frames; water/ball subtract0.5/4 seconds. Lock never applies buildup or consumes elemental freeze.
- First-hit timings depend on fixed travel assumptions. DPS and shares are analytical estimates, not game measurements or specific boss kill times.

花后仅计算幽灵套的静态属性，幽灵追击未模拟，不能将表中总DPS称为完整幽灵套实战DPS。未计防御、多部位、溅射、空枪或生存消耗。冻结奖励机制尚未正式接入，这些数值是设计方案比较。

运行 `node Tools/MageBalance/experiments/freeze-charge-stages-2026-10-19/run.cjs` 刷新结果和本报告。检查：resource conservation、mana bounds、lock charges require freezes、no pre-hardmode locks、ball formal cooldown spacing、completed lock damage budget。
