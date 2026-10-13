'use strict';
// Independent design trial: long ice buildup, water during cooldown, no wet upkeep switching.
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const S = require('./simulate-v3.cjs'), V4 = require('./simulate-v4.cjs');
const previous = JSON.parse(fs.readFileSync(path.join(__dirname, 'candidate-water-contribution.json'), 'utf8')).parameters;
const parameters = {...previous, freezeBuildSeconds:14, iceResetGap:2.5, waterDiscount:.5,
  waterRatio:.9, iceRatio:.9, waterWet:5, ballWet:15, thunderBudgetB:48, filler:'water'};
function stats(r) {
  const water=r.records.water.handSeconds, ice=r.records.ice.handSeconds;
  const basics=r.actions.filter(a=>a.key==='water'||a.key==='ice');
  const switches=basics.slice(1).reduce((n,a,i)=>n+Number(a.key!==basics[i].key),0);
  return {ratio:r.ratio,dps:r.dps,water,ice,iceShare:ice/(water+ice),basicSwitches:switches,
    freezeCount:r.freezeCount,freezeIntervals:r.freezeIntervals,thunderPaidSeconds:r.thunderPaidSeconds,
    wetCoverage:r.wetCoverage,thunderWetCoverage:r.thunderWetCoverage,idleSeconds:r.idleSeconds,
    damageShares:Object.fromEntries(Object.entries(r.records).map(([k,v])=>[k,v.damage/(r.dps*300)]))};
}
const livePaths=[path.resolve(__dirname,'../../index.html'),path.resolve(__dirname,'../../../../ElainaModSkills/Data/SkillBalance.json')];
const crypto=require('node:crypto');
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const before=livePaths.map(hash);
const baseline=S.pair({...parameters,trace:true}),hat=S.pair({...parameters,hat:true,trace:true});
const iceDuringCooldown=S.pair({...parameters,filler:'ice',trace:true});
const hatIceDuringCooldown=S.pair({...parameters,hat:true,filler:'ice',trace:true});
const prior=V4.pair({...V4.recommended,trace:true});
const durationOnly=[3,12,24].map(seconds=>({seconds,result:S.pair({...previous,freezeBuildSeconds:seconds,trace:true})}));
for(const [pair,alternative] of [[baseline,iceDuringCooldown],[hat,hatIceDuringCooldown]]) {
  for(const prefix of ['arcane','damage']) {
    const r=pair[prefix],t=stats(r);
    assert(r.ratio>=1.5&&r.ratio<=2,'graduation target range');
    assert(t.iceShare>=.5&&t.iceShare<=.6,'balanced basic hand time');
    assert(t.basicSwitches<=20,'at most two basic transitions per freeze cycle');
    assert(r.dps>alternative[prefix].dps,'water during cooldown beats this ice-filler alternative');
    assert(r.freezes.every(f=>f.iceSeconds>=14&&f.time-f.buildStart>=14-1e-7));
    assert(r.ledger.externalMana===0&&Math.abs(r.ledger.error)<1e-5);
    assert(Math.abs(r.records.thunder.spent-15*r.thunderPaidSeconds)<1e-6);
  }
}
assert(stats(baseline.arcane).basicSwitches<stats(prior.arcane).basicSwitches);
assert(livePaths.every((file,i)=>hash(file)===before[i]),'formal configuration remains unchanged');
const result={experimentOnly:true,duration:300,parameters,
  assumptions:'Single high-HP dummy; actual hits idealized; no damage received; graduation equipment; five 30% mana potions; no external mana. The 2.5s buildup reset tolerance is a design proposal, not combat-validated.',
  baseline,hat,iceDuringCooldown,hatIceDuringCooldown,prior,durationOnly,
  timeStats:{baseline:Object.fromEntries(['arcane','damage'].map(k=>[k,stats(baseline[k])])),
    hat:Object.fromEntries(['arcane','damage'].map(k=>[k,stats(hat[k])])),prior:stats(prior.arcane)},
  checks:['14s buildup and no cooldown precharge','50-60% ice basic hand time in four graduation cases',
    'at most 20 water/ice switches in 300s','water cooldown rotation beats compared ice cooldown rotation',
    'mana conserved and thunder paid continuously','all four graduation cases within 1.5-2x vanilla','formal configuration unchanged']};
fs.writeFileSync(path.join(__dirname,'results-v5.json'),JSON.stringify(result,(k,v)=>['reductions','actions','damageEvents'].includes(k)?undefined:v,2)+'\n');
fs.writeFileSync(path.join(__dirname,'candidate-parameters-v5.json'),JSON.stringify({experimentOnly:true,
  description:'Long ice buildup, per-hit water cooldown reduction. Replaces v4 timed acceleration; wet itself only provides thunder targeting.',
  parameters:{freezeBase:60,freezeDuration:4,freezeBuildSeconds:14,iceAttacksRequired:28,iceProjectilesRequired:84,
    iceResetGap:2.5,waterDiscount:.5,ballDiscount:4,waterWet:5,ballWet:15,waterRatio:.9,iceRatio:.9,
    waterInterval:.2,waterCost:1.5,iceInterval:.5,iceCost:4.5,
    ballCost:150,ballCD:15,ballCast:1.35,ballK:1.8,lockCost:100,lockCD:20,lockCast:.2,lockDelay:1,
    lockBaseB:3.2,lockPerSecondB:2.5,thunderCD:120,thunderDuration:30,thunderCast:2,thunderDelay:1,
    thunderStartup:0,thunderDrain:15,thunderBudgetB:48}},null,2)+'\n');
const f=(x,n=2)=>x.toFixed(n),a=stats(baseline.arcane),d=stats(baseline.damage);
const report=`# 长冰锥阶段、低切换冰法候选

2026-10-14。独立300秒高血量木桩设计实验；没有改动正式模组或实验室页面。

建议每轮在冻结冷却期间持续水流，冷却就绪后切冰锥，完成14秒有效攻击积累，再接冰凌锁、切回水流。水球和天雷优先在水流阶段施放。期间不需要为了续潮湿反复切回水流。

| 参数 | 候选 |
|---|---|
| 冻结CD、持续时间 | 60秒、4秒；从触发冻结起计时 |
| 冰锥冻结门槛 | 14秒有效攻击，28次完整攻击、84发有效命中 |
| 冰锥积累断档 | 超过2.5秒没有有效命中清零；从旧1.2秒放宽。此实战容错未验证 |
| 水流 | 每次1.5蓝、0.2秒、每次0.18B；有效命中立即减冻结CD0.5秒；潮湿5秒 |
| 水球 | 150蓝、CD15秒、施法1.35秒、8.1B；命中减冻结CD4秒；潮湿15秒 |
| 冰锥伤害 | 每次4.5蓝、0.5秒、3发共0.45B；毕业DPS倍率0.9B |
| 冰凌锁 | 100蓝、CD20秒；3.2B+剩余冻结秒数×2.5B；施法0.2秒消耗冻结、再延迟1秒命中 |
| 天雷 | CD120秒；施法2秒、延迟1秒、离手持续30秒；15蓝/秒、整次450蓝；预算48B |

水流、水球的潮湿只用于天雷锁定；取消v4的1.6倍/2倍冻结CD加速，两套规则不能并用。CD期间冰锥不积累；水系不储存负CD；没有30秒硬下限；相关Boss部位共用冻结CD。冰凌锁消耗冻结不会重启CD。

## 300秒结果

条件：毕业6槽，星云伤害3层；满蓝、不受击；5瓶30%回复药；无额外回蓝桥接。实际弹道命中和受击没有模拟。B为原始基准3500，装备倍率另算。

| 项目 | 无帽全奥秘 | 无帽全增伤 |
|---|---:|---:|
| 水流占手时间 | ${f(a.water)}秒 | ${f(d.water)}秒 |
| 冰锥占手时间 | ${f(a.ice)}秒 | ${f(d.ice)}秒 |
| 两种普攻中冰锥占比 | ${f(a.iceShare*100)}% | ${f(d.iceShare*100)}% |
| 水/冰切换次数 | ${a.basicSwitches} | ${d.basicSwitches} |
| 冻结次数 | ${a.freezeCount} | ${d.freezeCount} |
| DPS/原版 | ${f(a.ratio,3)}倍 | ${f(d.ratio,3)}倍 |
| 戴帽DPS/原版 | ${f(hat.arcane.ratio,3)}倍 | ${f(hat.damage.ratio,3)}倍 |

无帽奥秘差${f(baseline.lead*100)}%，戴帽奥秘差${f(hat.lead*100)}%。只代表本次固定策略，未证明全局最优或所有前缀配装优劣。

全奥秘相邻冻结间隔：${baseline.arcane.freezeIntervals.map(x=>f(x,1)).join('、')}秒，包含14秒冰锥积累。天雷实际付费90秒，潮湿覆盖${f(a.wetCoverage*100)}%。

无帽全奥秘伤害占比：${Object.entries(a.damageShares).map(([k,v])=>k+' '+f(v*100)+'%').join('，')}。

对照：v4全奥秘水/冰切换${stats(prior.arcane).basicSwitches}次，本候选${a.basicSwitches}次。CD期间继续冰锥、只补潮湿的对照策略为奥秘${f(iceDuringCooldown.arcane.ratio,3)}倍、增伤${f(iceDuringCooldown.damage.ratio,3)}倍，均低于本候选，但其他策略未穷举。

只延长旧版冰锥时间、保留水流0.16秒/命中和天雷34B的对照：

| 冰锥积累 | 奥秘冰锥时间占比 | 奥秘DPS/原版 |
|---|---:|---:|
${durationOnly.map(x=>'| '+x.seconds+'秒 | '+f(stats(x.result.arcane).iceShare*100)+'% | '+f(x.result.arcane.ratio,3)+'倍 |').join('\n')}

因此14秒门槛需要配合更强水系减CD，不能把本结果归因于只增加冰锥时间。长积累遇到躲避会更难完成，2.5秒断档容错只做了不受击情况下的验证；若实战仍容易清零，可再测试暂停或衰减积累。

验证通过：${result.checks.join('；')}。

复跑：node Tools/MageBalance/experiments/ice-build-2026-10-14/run-v5.cjs
`;
fs.writeFileSync(path.join(__dirname,'REPORT-v5.md'),report);
console.log(JSON.stringify({checks:result.checks,timeStats:result.timeStats,lead:baseline.lead,hatLead:hat.lead},null,2));
