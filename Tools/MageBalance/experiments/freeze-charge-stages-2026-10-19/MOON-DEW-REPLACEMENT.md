# 月露合剂：瞬间补魔替换为持续回蓝

只评估替换方案，不加入另外0.5%回蓝药，不修改游戏代码和HTML。

同一瓶药：旧制回复50%上限，新制120秒累计最多回复120%上限。旧制允许携带5份战斗储备，总计250%上限；新制取消这些瞬间储备。新制重复使用仅刷新时长。

## 120秒：完整冰水循环

| 阶段 | 方案 | DPS | 冰凌锁 | 水球 | 锁平均间隔 |
|---|---|---:|---:|---:|---:|
| 巨鹿后 | 旧制5份储备 | 448 | 0 | 8 | —秒 |
| 巨鹿后 | 旧制1份 | 448 | 0 | 8 | —秒 |
| 巨鹿后 | 新制1瓶/120秒 | 448 | 0 | 8 | —秒 |
| 肉后初期 | 旧制5份储备 | 1628 | 5 | 8 | 27.2秒 |
| 肉后初期 | 旧制1份 | 1356 | 4 | 5 | 35.1秒 |
| 肉后初期 | 新制1瓶/120秒 | 1433 | 4 | 7 | 30.4秒 |
| 双子后 | 旧制5份储备 | 2135 | 5 | 8 | 27.2秒 |
| 双子后 | 旧制1份 | 1778 | 4 | 5 | 35.1秒 |
| 双子后 | 新制1瓶/120秒 | 1879 | 4 | 7 | 30.4秒 |
| 花后 | 旧制5份储备 | 5170 | 5 | 8 | 27.2秒 |
| 花后 | 旧制1份 | 4323 | 4 | 5 | 34.2秒 |
| 花后 | 新制1瓶/120秒 | 4551 | 4 | 7 | 29.6秒 |

## 300秒：完整冰水循环

| 阶段 | 方案 | DPS | 冰凌锁 | 水球 | 锁平均间隔 |
|---|---|---:|---:|---:|---:|
| 巨鹿后 | 旧制5份储备 | 448 | 0 | 20 | —秒 |
| 巨鹿后 | 旧制1份 | 411 | 0 | 15 | —秒 |
| 巨鹿后 | 新制1瓶/120秒 | 427 | 0 | 17 | —秒 |
| 巨鹿后 | 新制续杯全程 | 448 | 0 | 20 | —秒 |
| 肉后初期 | 旧制5份储备 | 1298 | 9 | 14 | 32.1秒 |
| 肉后初期 | 旧制1份 | 1144 | 8 | 9 | 41.0秒 |
| 肉后初期 | 新制1瓶/120秒 | 1173 | 8 | 10 | 35.5秒 |
| 肉后初期 | 新制续杯全程 | 1372 | 10 | 13 | 29.7秒 |
| 双子后 | 旧制5份储备 | 1703 | 9 | 14 | 32.1秒 |
| 双子后 | 旧制1份 | 1500 | 8 | 9 | 41.0秒 |
| 双子后 | 新制1瓶/120秒 | 1538 | 8 | 10 | 35.5秒 |
| 双子后 | 新制续杯全程 | 1799 | 10 | 13 | 29.7秒 |
| 花后 | 旧制5份储备 | 4133 | 9 | 14 | 31.3秒 |
| 花后 | 旧制1份 | 3645 | 8 | 9 | 40.2秒 |
| 花后 | 新制1瓶/120秒 | 3941 | 9 | 10 | 36.0秒 |
| 花后 | 新制续杯全程 | 4576 | 11 | 13 | 29.0秒 |

## 新制持续覆盖300秒：纯冰与完整循环

| 阶段 | 纯冰DPS | 完整循环DPS | 不用水球DPS |
|---|---:|---:|---:|
| 巨鹿后 | 322 | 448 | 321 |
| 肉后初期 | 883 | 1372 | 1297 |
| 双子后 | 1160 | 1799 | 1701 |
| 花后 | 2831 | 4576 | 4121 |

- Replacement only: no old instant charges in new scenarios, and no separate0.5% regeneration potion.
- A dose grants an additional1% maximum mana per second for120seconds. Reuse refreshes duration without stacking strength.
- newOneDose is active only in the first120seconds. newRefreshed covers a300second fight with three doses; item use animation/cost not modeled.
- oldFiveCharges is the present full combat reserve, representing up to five consumed items. oldOneCharge compares a single item.
- Saved gear and skill values, capped progression; stages7/8/10/13 at35/40/50/65; lock250mana, k1.06. Formal ball15second cooldown.
- One persistent zero-defense target, all hits, expected crit; full opening mana; no incoming damage, life conversion, defense/healing spells or armor procs.
- Fixed representative casting policies, not globally optimal or an actual boss test. Output changes include cast timing near the end of the time window.
