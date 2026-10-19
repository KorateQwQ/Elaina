# 每秒额外0.5%最大魔力回复的影响

按整场持续、不可叠加、回复最大魔力的0.5%计算：战斗回蓝=5+最大魔力×2.5%。只试算，不实现药剂。

## 基础回复

| 阶段 | 魔力上限 | 原回复/秒 | 药后回复/秒 | 每27秒额外回复 |
|---|---:|---:|---:|---:|
| 巨鹿后 | 540 | 15.80 | 18.50 | 72.9 |
| 肉后初期 | 630 | 17.60 | 20.75 | 85.0 |
| 双子后 | 630 | 17.60 | 20.75 | 85.0 |
| 花后 | 640 | 17.80 | 21.00 | 86.4 |

## 120秒完整循环

| 阶段 | 原DPS→药后 | 增幅 | 冰凌锁次数 | 水球次数 | 补魔次数 | 冰凌锁间隔 |
|---|---:|---:|---|---|---|---|
| 巨鹿后 | 448→448 | 0.0% | 0→0 | 8→8 | 1→0 | —→— |
| 肉后初期 | 1628→1628 | 0.0% | 5→5 | 8→8 | 4→3 | 27.2→27.2 |
| 双子后 | 2135→2135 | 0.0% | 5→5 | 8→8 | 4→3 | 27.2→27.2 |
| 花后 | 5170→5170 | 0.0% | 5→5 | 8→8 | 4→3 | 27.2→27.2 |

## 300秒完整循环

| 阶段 | 原DPS→药后 | 增幅 | 冰凌锁次数 | 水球次数 | 补魔次数 | 冰凌锁间隔 |
|---|---:|---:|---|---|---|---|
| 巨鹿后 | 448→448 | 0.0% | 0→0 | 20→20 | 4→1 | —→— |
| 肉后初期 | 1298→1488 | 14.6% | 9→11 | 14→17 | 5→5 | 32.1→29.1 |
| 双子后 | 1703→1951 | 14.6% | 9→11 | 14→17 | 5→5 | 32.1→29.1 |
| 花后 | 4133→4739 | 14.7% | 9→11 | 14→17 | 5→5 | 31.3→28.3 |

## 五分钟纯冰对照

| 阶段 | 原纯冰DPS | 药后纯冰DPS | 药后完整循环DPS |
|---|---:|---:|---:|
| 巨鹿后 | 309 | 347 | 448 |
| 肉后初期 | 794 | 936 | 1488 |
| 双子后 | 1042 | 1229 | 1951 |
| 花后 | 2640 | 3005 | 4739 |

冰锥仍消耗30/秒。药后自然回复追平连续冰锥的最大魔力阈值为1000，药前1250；这只覆盖普攻，不含终结技、水球和防御支出。每100秒理论额外回复50%最大魔力，相当于当前一份储存补魔的额度，但满蓝溢出会降低实际收益。

- Buff active for the whole120/300second fight; adds0.005*maximum mana per second without increasing maximum mana. Fixed5/sec retained.
- Current saved skills and gear; stage7/8/10/13 levels35/40/50/65. Planned unlock excludes lock before hardmode.
- Same single-target model as finisher-mana: zero defense, full hits, expected crit; no spectral/titanium procs, incoming damage, healing/shield or life conversion.
- Full opening mana and five50%-mana elixirs except explicitly no-elixirs policy. Regen potion has no modeled cast/item cost or stacking.
- WaterBall formal15second cooldown, ignoring current temporary test override. Freeze permission and60second cooldown both required.
- Fixed policies, not global optimization or actual boss measurement; extra regen can change timings and end-of-window cast counts.
- Mixed reserves earned upcoming lock budget and uses beam while funding ready lock. Pure ice can pause to save for lock.

复现：`node Tools/MageBalance/experiments/freeze-charge-stages-2026-10-19/regen-potion.cjs`。
