# 水球双冷却返还敏感性

只试算、不改正式参数。候选名称为“冰凌锁CD返还秒数/冻结CD返还秒数”。

## 阶段8 / damage

| 方案 | 无魔力约束间隔 | 120秒DPS/间隔 | 300秒DPS/间隔 | 600秒DPS/后5轮间隔 |
|---|---:|---:|---:|---:|
| noBall | 27.6秒 | 1719 / 27.6秒 | 1595 / 27.6秒 | 1593 / 27.5秒 |
| current | 27.2秒 | 1605 / 34.3秒 | 1450 / 36.7秒 | 1397 / 38.2秒 |
| 6/8 | 26.0秒 | 1600 / 34.3秒 | 1445 / 36.7秒 | 1392 / 38.2秒 |
| 8/10 | 25.2秒 | 1597 / 34.2秒 | 1442 / 36.7秒 | 1390 / 38.2秒 |
| 10/14 | 24.4秒 | 1593 / 34.2秒 | 1438 / 36.7秒 | 1385 / 38.2秒 |
| 12/16 | 23.6秒 | 1589 / 34.1秒 | 1475 / 36.7秒 | 1383 / 38.2秒 |

## 阶段8 / arcane

| 方案 | 无魔力约束间隔 | 120秒DPS/间隔 | 300秒DPS/间隔 | 600秒DPS/后5轮间隔 |
|---|---:|---:|---:|---:|
| noBall | 27.6秒 | 1398 / 27.6秒 | 1297 / 27.6秒 | 1296 / 27.5秒 |
| current | 27.2秒 | 1544 / 27.2秒 | 1422 / 27.3秒 | 1407 / 29.0秒 |
| 6/8 | 26.0秒 | 1542 / 26.0秒 | 1421 / 27.2秒 | 1401 / 29.0秒 |
| 8/10 | 25.2秒 | 1542 / 25.2秒 | 1419 / 27.2秒 | 1397 / 29.0秒 |
| 10/14 | 24.4秒 | 1545 / 24.5秒 | 1412 / 27.1秒 | 1392 / 29.0秒 |
| 12/16 | 23.6秒 | 1543 / 24.1秒 | 1411 / 27.1秒 | 1390 / 29.0秒 |

## 阶段13 / damage

| 方案 | 无魔力约束间隔 | 120秒DPS/间隔 | 300秒DPS/间隔 | 600秒DPS/后5轮间隔 |
|---|---:|---:|---:|---:|
| noBall | 27.6秒 | 5429 / 27.6秒 | 5037 / 27.6秒 | 5033 / 27.5秒 |
| current | 27.2秒 | 5085 / 32.9秒 | 4717 / 35.8秒 | 4546 / 37.3秒 |
| 6/8 | 26.0秒 | 5063 / 33.1秒 | 4699 / 35.7秒 | 4529 / 37.3秒 |
| 8/10 | 25.2秒 | 5059 / 33.1秒 | 4693 / 35.8秒 | 4522 / 37.3秒 |
| 10/14 | 24.4秒 | 5049 / 33.1秒 | 4680 / 35.8秒 | 4509 / 37.3秒 |
| 12/16 | 23.6秒 | 5042 / 33.1秒 | 4672 / 35.8秒 | 4501 / 37.3秒 |

## 阶段13 / arcane

| 方案 | 无魔力约束间隔 | 120秒DPS/间隔 | 300秒DPS/间隔 | 600秒DPS/后5轮间隔 |
|---|---:|---:|---:|---:|
| noBall | 27.6秒 | 4442 / 27.6秒 | 4121 / 27.6秒 | 4118 / 27.5秒 |
| current | 27.2秒 | 4904 / 27.2秒 | 4522 / 27.2秒 | 4481 / 28.6秒 |
| 6/8 | 26.0秒 | 4901 / 26.0秒 | 4514 / 26.7秒 | 4457 / 28.5秒 |
| 8/10 | 25.2秒 | 4898 / 25.2秒 | 4510 / 26.9秒 | 4446 / 28.5秒 |
| 10/14 | 24.4秒 | 4914 / 24.4秒 | 4497 / 26.8秒 | 4450 / 28.5秒 |
| 12/16 | 23.6秒 | 4903 / 23.6秒 | 4501 / 26.8秒 | 4439 / 28.5秒 |

- Hypothetical replacement Moon Dew, full-fight1% max mana/sec, zero instant charges, no separate0.5% potion.
- Only the two ball refunds change; damage, costs,15second ball CD and60second lock/freeze CD remain saved values.
- One ball in each completed lock interval, same ice opening and minimum ball action; no-ball control included.
- freeMana diagnoses action/cooldown limits. Finite cases include full starting mana and water recovery; no life conversion, shields or damage taken.
- Single zero-defense freezable target, full hits, expected critical damage; same approximate action/flight timings and invariants as balls-per-lock.cjs.
