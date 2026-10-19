# 冰水联动：保留60秒冷却的推荐试调

2026-10-19。本报告取代此前为扶持纯冰而提出的24秒技能CD、30秒冻结抗性方案。用户确认：完整冰水循环是平衡基准，纯冰是低操作、低收益打法，不追求强度接近。这里只试算，未修改正式参数或游戏机制。

配置SHA256：`51a23d92dd6c8958fc8fad8ef90ab7740c69af8198413ee6fa15d42b3cc56dba`。

## 建议参数

| 项目 | 当前 | 建议 |
|---|---|---|
| 冰锥伤害系数 | 1.2B | 保持 |
| 冰锥每轮耗蓝 | 15 | 22.5（每秒30） |
| 冰锥每发冻结积蓄 | 2.8 | 5.6（单体全中18发/6轮，约4.5秒施法） |
| 冰凌锁基础CD | 60秒 | 保持 |
| 冰凌锁使用资格 | 无 | 自己的魔杖触发冻结后获得1次，最多储存1次，与CD同时满足才可用；开场资格0 |
| 冰凌锁单次预算/耗蓝/k | 21.2B/100/1.7666666667 | 保持，CD未改无需补偿k |
| 冻结返还冰凌锁CD | 18秒，玩家15秒间隔 | 以冻结资格取代，不再返还18秒；移除这项奖励的15秒限频 |
| 水流每次有效脉冲削减冰凌锁CD | 0.15秒 | 0.30秒 |
| 敌人解冻后的冻结冷却 | 60秒 | 保持 |
| 冻结持续时间 | 4秒 | 保持 |
| 水流每次削减冻结CD | 0.5秒 | 保持 |
| 水球削减冰凌锁/冻结CD | 3秒/4秒 | 保持 |
| 水球伤害/耗蓝/CD | k1.5/150/15秒 | 保持，忽略临时1秒测试覆盖 |

自然倒计时仍保留。换怪冻结只能补一个资格，不能增加层数或直接重置技能CD；要加速60秒技能恢复，需要实际使用水系命中。水流每秒5个有效周期，0.30秒返还意味着每秒额外推进1.5秒冰凌锁CD。

## 配装与口径

巨鹿后丛林/35级，肉后初期钛金/40级且已完成计划中的暴雪解锁，花后输出幽灵/65级。所有技能升满阶段上限。灰之魔女开启，满初始魔力，最多5次各恢复50%最大魔力的药剂，无额外急速。

这是单体零防御、可冻结、持续存活目标的模型；不计幽灵/钛金追击、Boss移动、阶段无敌、空枪、受伤与护盾/治疗支出。不是游戏内实测，也不是某个具体Boss的击杀时间。

## 120秒技能本体输出

正确循环：冰锥触发冻结后用冰凌锁，随后转水；水球在可支付时使用。开场优先水球，因此第一次冰凌锁约5.87秒；若先冰锥则约4.5秒。

| 阶段 | 冰水循环DPS | 纯冰DPS | 冰锥＋水球＋锁、无水流DPS | 冰凌锁次数：循环/纯冰 | 补魔次数：循环/纯冰 |
|---|---:|---:|---:|---|---|
| 巨鹿后 | 448 | 421 | 453 | 0/0 | 1/5 |
| 肉后初期 | 1348 | 970 | 1110 | 5/2 | 1/4 |
| 花后 | 4283 | 3083 | 3573 | 5/2 | 1/4 |

肉后与花后的冰凌锁平均间隔：正确循环约27.15秒，纯冰约68.25秒。冰锥＋水球但不用水流约55.85秒。正确循环两分钟比纯冰高约39%伤害，而且补魔更少；肉前尚无冰凌锁，短战DPS差距较小，收益首先表现为资源效率和控制频率。

## 正确循环伤害分配，120秒

| 阶段 | 冰锥 | 水流 | 水球 | 冰凌锁 |
|---|---:|---:|---:|---:|
| 巨鹿后 | 17.6% | 47.2% | 35.2% | 0.0% |
| 肉后初期 | 10.5% | 27.0% | 21.1% | 41.4% |
| 花后 | 10.5% | 27.0% | 21.1% | 41.4% |

冰锥本体占比低于冰凌锁，但它负责取得每次爆发资格；不能只按直接伤害占比认定其弱。

## 300秒长战

| 阶段 | 正确循环DPS | 纯冰DPS | 正确循环相对提高 | 循环消耗药剂 |
|---|---:|---:|---:|---:|
| 巨鹿后 | 448 | 309 | 44.7% | 4 |
| 肉后初期 | 1282 | 808 | 58.6% | 5 |
| 花后 | 4072 | 2596 | 56.8% | 5 |

五分钟完整循环已使用4～5次补魔，不能把此结果理解为无限续航。纯冰/无水流策略在药剂耗尽且冰凌锁已就绪时会停手攒蓝，不会故意一直打低费普攻而永远释放不了技能。

## 与当前机制比较

对照采用相同配装、相同策略、正式水球CD及计划解锁顺序；当前机制包含开场冰凌锁和冻结返还18秒，冰锥15耗蓝/2.8积蓄。

| 阶段 | 当前机制120秒DPS | 新方案120秒DPS | 差异 |
|---|---:|---:|---:|
| 巨鹿后 | 464 | 448 | -3.3% |
| 肉后初期 | 1369 | 1348 | -1.5% |
| 花后 | 4349 | 4283 | -1.5% |

这组目标是保持完整循环的大致输出，同时把收益转移到冰水配合上，不是全局增伤：开场不再白送冰凌锁，必须先冻结。单看两分钟总DPS没有明显提升，机制约束与纯冰代价更明显。

## 为什么选择0.30，而非同时提高所有返还

肉后全中120秒相同策略：只接资格、保留旧冰锥时，冰凌锁间隔约36.8秒；换快速冻结后仍用旧0.15水流返还约34.6秒；0.25约29.3秒；0.30约27.2秒；0.35约26.2秒。0.30已接近目标冻结循环，继续提高的收益开始受冻结资格限制。

把水流冻结CD返还也提高到0.75时，冰凌锁仍约27.2秒一次，控制覆盖上升但没有明显增加爆发频率；把水球对冰凌锁返还改6秒也只到约26.3秒。因此第一版只加强水流对冰凌锁的返还，保留其他返还。

这些是同一固定操作策略下的比较，不是全局最优策略搜索；释放次数和有限战斗结束点可能影响平均DPS。主方案控制覆盖约19%，没有采用此前30秒目标抗性方案带来的约25～27%覆盖。

## 复现与检查

运行 `node Tools/MageBalance/experiments/freeze-charge-stages-2026-10-19/long-cd-water-cycle.cjs` 刷新报告与结果。检查魔力守恒、资格来源、每次重放已累计60秒实际冷却进度（自然＋实际水系返还）、取消18秒返还、技能伤害预算和运行期间配置未变化。

- Player lock base cooldown60s and target post-thaw freeze cooldown60s retained in EVERY scenario; freeze duration4s.
- Gated proposals require both freeze-earned permission (cap1, opening0) and recovered cooldown; no freeze18s refund or15s reward limiter.
- CurrentRuntime comparison uses original18s freeze refund/15s limiter and no permission requirement, with opening lock ready.
- Lock damage21.2B, mana100 and k1.7666666667 retained. WaterBall saved k1.5, formal15s cooldown, mana150.
- Saved stages7/8/13 equipment and levels35/40/65; capped skill levels. Planned unlock excludes lock before hardmode in all scenarios.
- Single persistent zero-defense freezable target, all hits land, expected crit; no armor procs, phase invulnerability or movement.
- Full opening mana, five50%-mana elixirs; no life conversion, incoming damage, shield/healing costs or extra buffs. No haste.
- mixed: water after freeze; mixedFrozenIce: ice during frozen duration; waterPriority: banked permission favors water until lock cooldown recovers.
- PureIce includes ice and earned lock. IceBall omits beam. PureIce/IceBall wait to reserve mana for a ready lock when needed.
- Per-hit water refunds are capped by current remaining cooldown; shared-target/multi-hit duplicate refunds are not modeled as extra gains.
- Fixed representative strategies, not global optimization or real boss DPS. See prior model for approximate action/flight timings.
