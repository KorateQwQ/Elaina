'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const S=require('./simulate-v3.cjs');
const f=(x,d=2)=>x.toFixed(d),near=(a,b)=>assert(Math.abs(a-b)<1e-6,`${a} != ${b}`);
const base=S.run({...S.recommended,trace:true});
near(base.freezes[0].time,3);near(base.freezes[0].iceSeconds,3);
assert.equal(base.actions.filter(a=>a.key==='ice'&&a.time<3).length,6);
assert.equal(base.actions.filter(a=>a.key==='ice'&&a.phase==='cooldown').length,0);
assert(base.freezes.every(a=>a.iceSeconds>=3));
const thirds=S.run({...S.recommended,iceHitFraction:1/3});near(thirds.freezes[0].time,9);
const missed=S.run({...S.recommended,iceHitFraction:0});assert.equal(missed.freezeCount,0);
const noWater=S.run({...S.recommended,omit:['water','ball','lock','thunder'],unlimitedMana:true});
near(noWater.freezes[0].time,3);noWater.freezeIntervals.forEach(x=>near(x,63));
const fast=S.run({...S.recommended,unlimitedMana:true,omit:['thunder']});
assert(fast.freezeIntervals.every(x=>x>=33&&x<35));
const accelerated=S.run({...S.recommended,unlimitedMana:true,omit:['thunder'],waterInterval:.1});
assert(accelerated.freezeIntervals.every(x=>x<30));
const neverSwitch=S.run({...S.recommended,readyBasic:'water'});assert.equal(neverSwitch.freezeCount,0);
const isolatedOptions={...S.recommended,omit:['water','ball','thunder'],unlimitedMana:true};
const immediate=S.run(isolatedOptions),delayed=S.run({...isolatedOptions,lockWait:2});
near(immediate.detonations[0].damageB-delayed.detonations[0].damageB,5);
near(base.thunderPaidSeconds,90);near(base.records.thunder.spent,1350);
const checks=['First freeze at 3s from six .5s attacks; all three projectiles share one contribution',
  'No cooldown-phase ice attacks in intended rotation; no build-up can precharge during cooldown',
  'One-third ice hits require 9s; zero hits never freeze',
  'Without water, cooldown60s plus ice build3s gives63s between freezes',
  'Water-frequency calibration plus3s accumulation gives33s+, accelerated water can still get below30s',
  'Staying on water after cooldown never freezes; immediate detonation beats late detonation',
  '90s thunder uptime pays1350 mana; all runs assert resources, hand-time and consumed-freeze conservation'];
const options={...S.defaults,...S.recommended};
const result={date:'2026-10-14',version:3,checks,options,
  oldDamageSimple:S.pair({waterRatio:.7,ballK:1.8,thunderBudgetB:43.2}),
  baseline:S.pair(S.recommended),hat:S.pair({...S.recommended,hat:true}),best:S.search(),hatBest:S.search({hat:true}),
  cooldownChoices:['water','mixed','ice'].map(filler=>({filler,normal:S.search({filler}),hat:S.search({filler,hat:true})})),
  stayWater:S.pair({...S.recommended,readyBasic:'water'}),
  buildTimes:[2,3,4].map(freezeBuildSeconds=>({seconds:freezeBuildSeconds,result:S.pair({...S.recommended,freezeBuildSeconds})})),
  misses:[1,2/3,1/3].map(iceHitFraction=>({iceHitFraction,result:S.pair({...S.recommended,iceHitFraction})})),
  ablations:['water','ball','ice','lock','thunder'].map(omit=>({omit,result:S.search({omit:[omit]})})),
  calibration:fast,accelerated,baseNoWater:noWater};
fs.writeFileSync(path.join(__dirname,'results-v3.json'),JSON.stringify(result,(k,v)=>['reductions','actions','damageEvents'].includes(k)?undefined:v,2)+'\n');
fs.writeFileSync(path.join(__dirname,'candidate-parameters-v3.json'),JSON.stringify({experimentOnly:true,
  iceBuild:'6 fully hitting .5s ice-cone attacks after cooldown. All3 projectiles together add .5s per attack. Attack gaps>1.2s reset accumulation; no cooldown precharge.',
  rotation:'Water and ball during cooldown; prioritize ice for3s after ready, detonate immediately, repeat; thunder off-hand30s/120s.',
  parameters:options},null,2)+'\n');
const a=result.baseline.arcane,d=result.baseline.damage;
const names={water:'水流',ball:'水球',ice:'冰锥',lock:'冰凌锁',thunder:'天雷'};
const report=`# 冰法第三轮：冷却打水，就绪打冰

2026-10-14。300秒、不受击、同一毕业配装与五瓶30%药剂。独立理论试算，正式配置和游戏代码未修改。

主要目标先保证简单的状态切换：冻结CD中用水流/水球；CD就绪后连续用冰锥至少3秒；冻结后立即引爆冰凌锁。天雷仍120秒CD，离手持续30秒，每秒15蓝。

## 修正上一轮模型

上一轮CD完成后只需下一次冰锥命中即可冻结。本轮补上3秒冰锥积累，这会增加时间和魔力成本。

- 普攻周期0.5秒，一整次三发全部命中共同贡献0.5秒积累；**6次攻击、18发命中共3秒**才触发4秒冻结。不能三发各贡献0.5秒。
- 冻结CD期间不积累。最后一击若从CD阶段开始，即便在CD结束后落地，也不给整次预存的积累。
- CD就绪后需要切到冰锥；推荐打法在3秒积累中优先冰锥，水球/天雷的起手等积累完成后再尝试，已有天雷继续离手攻击。
- 连续命中允许短暂中断：相邻有效冰锥命中间隔超过1.2秒则积累清零。这个容错值是本次建议，未由用户指定。
- 部分丢伤按有效命中比例同时降低伤害和积累速度。例如每次只命中三发中的一发，最早需要9秒；完全不命中则不会冻结。这里是连续平均命中率，尚未模拟随机逐发丢失。
- 不提前假设30秒硬下限。60秒基础CD，水流每次命中减0.1333秒，水球每次减6秒，按实际命中累计。CD起算仍从冻结触发时开始。

初次冻结没有开场CD，故最早在3秒出现。之后最快水系CD阶段约30秒，还要再加冰锥阶段至少3秒：速度校准约${f(Math.min(...fast.freezeIntervals))}～${f(Math.max(...fast.freezeIntervals))}秒一轮；正常打法全奥秘实际间隔${a.freezeIntervals.map(x=>f(x)).join('、')}秒。

实际投射物飞行、回撤动画和敌人移动可能进一步延长冻结时刻。本模型把每次冰锥整次命中放在0.5秒动作完成时，不表示游戏内发射3秒便保证冻结。

## 为什么还需要调普攻倍率

沿用水流0.7B、冰锥0.9B时，某些场景CD中继续冰锥仍能赚更多面板伤害，违背用户把状态切换放在首位的目标。推荐毕业阶段把水流提高到**0.9B**，冰锥保持**0.9B**。

这并不是让冰锥平时更高伤：两者独立DPS相同，但水流每秒7.5蓝、冰锥每秒9蓝；CD中水流额外推进冷却、维持潮湿，因此更值。CD就绪后水系减CD不再有可用收益，冰锥的积累才产生下一次冻结/引爆，所以此时切冰更赚。

## 推荐参数

| 技能 | 耗蓝 | CD / 周期 | 输出与作用 |
|---|---:|---:|---|
| 水流 | 1.5 | 0.2秒 | **毕业倍率0.9B**；每次减CD0.1333秒 |
| 水球 | 150 | 15秒 | K=1.8，8.1B整次伤害；每次减CD6秒 |
| 冰锥 | 4.5 | 0.5秒 | 0.9B；CD就绪后六次完整命中积累3秒冻结条件 |
| 冰凌锁 | 100 | 自身20秒 | 3.2B + 剩余冻结秒数×2.5B；0.2秒施法后即时引爆，伤害1秒后命中 |
| 天雷 | 15/秒 | 120秒 | 持续30秒、满程450蓝；总预算**34B**，作用期间约1.133B/秒 |

天雷从上一轮43.2B下调至34B，为提高水流伤害留出预算。若仍用现有rate4%的定价公式，天雷完整450蓝、120秒CD的K约**0.3148**，但付款必须另做持续消耗。冰凌锁的基础K等效0.8，冻结增伤独立读取剩余时长，不能额外叠旧版全额K。

此处水流倍率是毕业平衡建议。若不希望同时抬高中期水流，需要在ratioCurve里另设成长节点，不能直接把一条固定0.9倍率当作全流程最终数值；本轮未测试前期。

## 简单状态循环的300秒结果

原版基准${f(a.vanilla)}DPS。

| 配装 | 奥秘DPS / 原版倍数 | 增伤DPS / 原版倍数 |
|---|---:|---:|
| 无帽 | ${f(a.dps)} / ${f(a.ratio,3)} | ${f(d.dps)} / ${f(d.ratio,3)} |
| 带帽 | ${f(result.hat.arcane.dps)} / ${f(result.hat.arcane.ratio,3)} | ${f(result.hat.damage.dps)} / ${f(result.hat.damage.ratio,3)} |

全奥秘水流${a.records.water.count}次，冰锥${a.records.ice.count}次，水球${a.records.ball.count}次，冻结/引爆${a.freezeCount}/${a.detonationCount}次，天雷${a.records.thunder.count}次且共维持${a.thunderPaidSeconds}秒。每次及时引爆平均剩${f(a.averageRemaining)}秒，冰凌锁单次12.7B。

冷却结束后一直打水、不切冰时：奥秘${f(result.stayWater.arcane.ratio,3)}倍、增伤${f(result.stayWater.damage.ratio,3)}倍，冻结与引爆均为0；切冰的收益来自完整组合。没有完整命中或足够魔力时，实际会更低。

## CD期间打法比较

每种固定普攻倾向内分别比较资源/优先级规则，避免用同一不合适策略压低某一前缀。所有方案在CD就绪后都优先积累冰锥3秒。

| CD期间的普攻倾向 | 无帽奥秘 / 增伤 | 带帽奥秘 / 增伤 |
|---|---:|---:|
${result.cooldownChoices.map(x=>`| ${{water:'水流',mixed:'冰锥为主、末段水流',ice:'冰锥为主、补潮湿才用水流'}[x.filler]} | ${f(x.normal.arcane.ratio,3)} / ${f(x.normal.damage.ratio,3)} | ${f(x.hat.arcane.ratio,3)} / ${f(x.hat.damage.ratio,3)} |`).join('\n')}

在本轮所测规则中，水系CD阶段胜出；这是有限候选检验，不是任意动作序列的最优证明。提高水流的单独输出也有助于此结论，因此减CD本身并不是唯一收益来源。

双方分别取所测最高输出后：无帽${f(result.best.arcane.ratio,3)} / ${f(result.best.damage.ratio,3)}倍，奥秘落后${f(-result.best.lead*100)}%；带帽${f(result.hatBest.arcane.ratio,3)} / ${f(result.hatBest.damage.ratio,3)}倍，奥秘落后${f(-result.hatBest.lead*100)}%。同一简单循环的差距更小，但不能因此声称奥秘胜过合理增伤操作。防御端尚未验证。

## 冰锥积累时间与命中敏感性

| 最低积累时间 | 无帽奥秘 / 增伤 |
|---|---:|
${result.buildTimes.map(x=>`| ${x.seconds}秒 | ${f(x.result.arcane.ratio,3)} / ${f(x.result.damage.ratio,3)} |`).join('\n')}

3秒相当于6次完整攻击、27蓝；固定窗口结束后马上引爆更容易形成稳定节奏。延长到4秒会增加站桩与丢失潮湿的风险；降低到2秒则弱化准备阶段。本轮建议保留用户提出的3秒。

| 冰锥平均有效命中比例 | 首次冻结时刻 | 无帽奥秘 / 增伤 |
|---|---:|---:|
${result.misses.map(x=>`| ${f(x.iceHitFraction*100)}% | ${f(x.result.arcane.freezes[0]?.time??NaN)}秒 | ${f(x.result.arcane.ratio,3)} / ${f(x.result.damage.ratio,3)} |`).join('\n')}

## 拆件检查

每次去掉一项后重新选所测资源策略：

| 移除 | 无帽奥秘 / 增伤 |
|---|---:|
${result.ablations.map(x=>`| ${names[x.omit]} | ${f(x.result.arcane.ratio,3)} / ${f(x.result.damage.ratio,3)} |`).join('\n')}

整组在这些单项移除试验中仍更强，但增伤侧天雷净收益偏小，完整组合的优势不是所有组件都同样稳固。尚未用buff替换空槽，也未测试其他流派。

## 验证与复跑

${checks.map((x,i)=>`${i+1}. ${x}`).join('\n')}

另外每次模拟验证魔力守恒、无重叠占手、总时间300秒、冷却减量真实发生、冻结不可重复引爆、天雷按实际付费帧兑现伤害。正式结果外部魔力补充为0；无限魔力只用于隔离速度校准。

\`\`\`powershell
node Tools/MageBalance/experiments/ice-build-2026-10-14/run-v3.cjs
\`\`\`

模型：simulate-v3.cjs；参数：candidate-parameters-v3.json；结果：results-v3.json。运行脚本会重新生成本报告。

其他假设仍沿用第二轮：水流潮湿5秒、水球潮湿15秒，任一都让天雷满锁定，无潮湿按50%伤害；天雷释放后离手、断蓝终止；不模拟目标防御、真实飞行、随机逐发命中、多人同步或前期平衡。上述未知项必须在接入实际游戏前校准。
`;
fs.writeFileSync(path.join(__dirname,'REPORT-v3.md'),report);
console.log(JSON.stringify({checks,baseline:S.compact(result.baseline),hat:S.compact(result.hat),best:S.compact(result.best),hatBest:S.compact(result.hatBest),cooldownChoices:result.cooldownChoices.map(x=>({filler:x.filler,A:x.normal.arcane.ratio,D:x.normal.damage.ratio,HA:x.hat.arcane.ratio,HD:x.hat.damage.ratio}))},null,2));
