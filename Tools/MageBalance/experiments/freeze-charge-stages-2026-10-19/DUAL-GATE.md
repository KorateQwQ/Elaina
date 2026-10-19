# 冰凌锁独立冷却＋冻结资格：参数与三阶段试算

> 历史方案，已撤回。用户明确要求完整冰水联动为基准，不扶持纯冰；24秒技能CD与30秒冻结抗性的建议不再采用。后续参数以同目录 LONG-CD-WATER.md 为准，以下数据仅保留作比较。

2026-10-19。未修改正式配置或游戏机制；以下为待实施的设计参数。配置SHA256：`51a23d92dd6c8958fc8fad8ef90ab7740c69af8198413ee6fa15d42b3cc56dba`。

## 第一版建议

| 项目 | 当前正式值 | 建议试值 |
|---|---|---|
| 冰锥DPS系数 | 1.2B | 保持1.2B |
| 冰锥每轮耗蓝 | 15 | 18（24/秒） |
| 冰锥每发冻结积蓄 | 2.8 | 5.6（单体全中18发、6轮） |
| 冰凌锁自身CD | 60秒，可被返还 | 24秒，不接受本轮讨论的直接返还 |
| 冰凌锁释放资格 | 无冻结前置 | 自己成功冻结后获得1次，最多储存1次；资格和CD同时满足才可用 |
| 冰凌锁开场资格 | 无此机制 | 0，先用冰锥触发冻结 |
| 冰凌锁耗蓝/单次伤害 | 100/21.2B | 保持 |
| 冰凌锁伤害系数k | 1.7666666667 | 4.4166666667，补偿CD变化以保持21.2B |
| 目标解冻后冻结抗性 | 60秒 | 30秒 |
| 冻结持续时间 | 4秒 | 保持 |
| 水流每次有效脉冲削减目标冻结CD | 0.5秒 | 保持 |
| 水球 | 15秒CD、150耗蓝、k1.5、削减目标冻结CD4秒 | 保持；不采用1秒测试覆盖 |
| 直接冰凌锁减CD | 冻结18秒、水流0.15秒、水球3秒 | 取消；15秒冻结返还限频也取消，由资格上限和自身CD限频 |

伤害预算当前含cd乘数。仅改60→24而不调整k，会将单次预算从21.2B降至8.48B；本模型同步补偿k，未暗中降低伤害。无急速时24秒是玩家自己的最低释放间隔，换怪冻结不能突破。现有急速会改变实际CD，本轮没有额外急速。

## 120秒单体结果

技能升满阶段上限，灰之魔女开启，满初始魔力，允许5次各回复50%最大魔力的药剂。纯冰=冰锥＋取得资格的冰凌锁；巨鹿后尚无冰凌锁。

| 阶段 | 配装/等级 | 纯冰DPS | 冰锥＋水球＋锁，不用水流 | 完整循环，冻结期间继续冰锥 | 冰凌锁平均间隔：纯冰/混合 |
|---|---|---:|---:|---:|---|
| 巨鹿后 | 丛林/35 | 421 | 534 | 493 | —/—秒 |
| 肉后初期 | 钛金/40，已完成暴雪解锁 | 1080 | 1397 | 1424 | 38.3/24.3秒 |
| 花后 | 输出幽灵/65 | 3431 | 4440 | 4524 | 38.3/24.3秒 |

未计目标防御、幽灵/钛金套追击、空枪、多部位、阶段无敌、受伤和护盾/治疗消耗，不是实测Boss DPS。两分钟结束点可能截断最后一次冰凌锁；例如30秒目标抗性下纯冰第四次施法接近120秒，次数不等于四次完整预算。

## 完整循环伤害分配与资源，120秒

| 阶段 | 冰锥 | 水流 | 水球 | 冰凌锁 | 用药：纯冰/无水流/完整循环 |
|---|---:|---:|---:|---:|---|
| 巨鹿后 | 42.7% | 25.3% | 32.0% | 0.0% | 2/5/3 |
| 肉后初期 | 26.2% | 14.6% | 20.0% | 39.2% | 2/5/3 |
| 花后 | 26.2% | 14.6% | 20.0% | 39.2% | 2/5/3 |

已储存冻结资格且技能仍在CD时，继续使用冰锥的更积极策略，在肉后/花后两分钟约1456/4627 DPS，但各用4次补魔，长期未必有利。

## 300秒续航对照

长战完整循环采用冻结后立刻转水的节约策略，以避免持续用冰锥耗尽药剂。

| 阶段 | 纯冰DPS | 不用水流DPS | 冻结后转水DPS |
|---|---:|---:|---:|
| 巨鹿后 | 386 | 380 | 450 |
| 肉后初期 | 1057 | 1079 | 1330 |
| 花后 | 3395 | 3456 | 4290 |

纯冰与无水流策略在药剂耗尽后，会为已就绪的冰凌锁停手攒蓝，不采用一直打便宜普攻、永远攒不到100魔力的策略。尽管如此，长战水流的续航仍有价值。模型没有求全局最优操作，结果不能视为玩法强度上限。

## 为什么不再增强水流

以肉后120秒相同操作为例，30秒冻结抗性下，水流0.5→0.75秒削减对冰凌锁频率提升很小：自身24秒CD已成为限制。它主要提高普通冻结频率或腾出更多冰锥输出时间。因此第一版保留0.5，避免同时大幅增加控制频率。

这组改变最大的额外收益是控场：完整混合循环的单体控制覆盖，在模型中由上一版约17%增至约25～27%，取决于操作。实际Boss是否能正常完成关键招式，需要实测。30秒冻结抗性位于KL通用冻结逻辑，正式接入时也会影响其他使用该冻结系统的攻击来源。

## 模型边界与复现

- Hypothetical independent player cooldown plus max-one freeze permission; no opening permission. No direct cooldown refunds.
- Lock per-cast damage preserved at21.2B by compensating k when cd changes. Cost100, freeze duration4s retained.
- No haste; cooldown24 is an actual minimum in these scenarios. With haste the cap follows the adjusted cooldown.
- Same stages7/8/13 loadouts and levels35/40/65. Lock only available after planned hardmode blizzard unlock.
- WaterBall uses saved k1.5 and formal15s CD. Five50%-mana elixirs, no life conversion, incoming damage, healing/shield cost or extra buffs.
- Zero-defense single persistent target, all hits land, expected crit, no spectre/titanium procs; not a real boss fight.
- Strategies are fixed representative policies, not globally optimal. Pure ice includes earned IceShardLock but no water skills.
- Pure-ice and no-beam policies reserve mana for a ready earned lock after elixirs run out, instead of spending each recovered point on filler forever.
- mixed uses water after freeze; mixedFrozenIce keeps attacking with ice during freeze; mixedBankedIce also uses ice while permission is banked and lock cooldown remains.
- Control uptime counts union of4-second elemental freeze and lock hold from29th to110th frame; late-period target control is truncated.
- Source stage/deer equipment has five equipped accessories in six slots. See earlier reports for exact snapshots and motion assumptions.

运行 `node Tools/MageBalance/experiments/freeze-charge-stages-2026-10-19/dual-gate.cjs`。检查魔力收支守恒、资格来源、独立冷却间隔、技能伤害预算和配置在运行期间未变化。所有正式参数均只读。
