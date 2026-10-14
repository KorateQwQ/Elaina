'use strict';
// Independent design trial: long ice buildup, water during cooldown, no wet upkeep switching.
const fs = require('node:fs'), path = require('node:path'), assert = require('node:assert/strict');
const S = require('./simulate-v3.cjs'), V4 = require('./simulate-v4.cjs');
const previous = JSON.parse(fs.readFileSync(path.join(__dirname, 'candidate-water-contribution.json'), 'utf8')).parameters;
const original = {...previous, freezeBuildSeconds:14, iceResetGap:2.5, waterDiscount:.5,
  waterRatio:.9, iceRatio:.9, waterWet:5, ballWet:15, thunderBudgetB:48, filler:'water'};
// User tweak after the first v5 run: 15s buildup tolerance, water 0.7B outside freeze cooldown.
const parameters = {...original, iceResetGap:15, waterReadyRatio:.7};
function stats(r) {
  const water=r.records.water.handSeconds, ice=r.records.ice.handSeconds;
  const basics=r.actions.filter(a=>a.key==='water'||a.key==='ice');
  const switches=basics.slice(1).reduce((n,a,i)=>n+Number(a.key!==basics[i].key),0);
  return {ratio:r.ratio,dps:r.dps,water,ice,iceShare:ice/(water+ice),basicSwitches:switches,
    freezeCount:r.freezeCount,freezeIntervals:r.freezeIntervals,thunderPaidSeconds:r.thunderPaidSeconds,
    wetCoverage:r.wetCoverage,thunderWetCoverage:r.thunderWetCoverage,idleSeconds:r.idleSeconds,waterReadyHits:r.waterReadyHits,
    damageShares:Object.fromEntries(Object.entries(r.records).map(([k,v])=>[k,v.damage/(r.dps*300)]))};
}
const livePaths=[path.resolve(__dirname,'../../index.html'),path.resolve(__dirname,'../../../../ElainaModSkills/Data/SkillBalance.json')];
const crypto=require('node:crypto');
const hash=file=>crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const before=livePaths.map(hash);
const baseline=S.pair({...parameters,trace:true}),hat=S.pair({...parameters,hat:true,trace:true});
const iceDuringCooldown=S.pair({...parameters,filler:'ice',trace:true});
const hatIceDuringCooldown=S.pair({...parameters,hat:true,filler:'ice',trace:true});
const originalBaseline=S.pair({...original,trace:true}),originalHat=S.pair({...original,hat:true,trace:true});
const waterOnly=S.pair({...parameters,omit:['ice','lock'],trace:true});
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
for(const prefix of ['arcane','damage']) {
  assert(baseline[prefix].dps>waterOnly[prefix].dps,'skipping ice and lock is worse');
  assert(waterOnly[prefix].waterReadyHits>0&&baseline[prefix].waterReadyHits<waterOnly[prefix].waterReadyHits);
}
assert(livePaths.every((file,i)=>hash(file)===before[i]),'formal configuration remains unchanged');
const result={experimentOnly:true,duration:300,parameters,original,
  assumptions:'Single high-HP dummy; actual hits idealized; no damage received; graduation equipment; five 30% mana potions; no external mana. Buildup never pauses on a dummy, so the 15s reset tolerance is not exercised here.',
  baseline,hat,iceDuringCooldown,hatIceDuringCooldown,prior,durationOnly,originalBaseline,originalHat,waterOnly,
  timeStats:{baseline:Object.fromEntries(['arcane','damage'].map(k=>[k,stats(baseline[k])])),
    hat:Object.fromEntries(['arcane','damage'].map(k=>[k,stats(hat[k])])),prior:stats(prior.arcane)},
  checks:['14s buildup and no cooldown precharge','50-60% ice basic hand time in four graduation cases',
    'at most 20 water/ice switches in 300s','water cooldown rotation beats compared ice cooldown rotation',
    'mana conserved and thunder paid continuously','all four graduation cases within 1.5-2x vanilla',
    'water outside freeze cooldown uses 0.7B; skipping ice and lock is worse','formal configuration unchanged']};
fs.writeFileSync(path.join(__dirname,'results-v5.json'),JSON.stringify(result,(k,v)=>['reductions','actions','damageEvents'].includes(k)?undefined:v,2)+'\n');
fs.writeFileSync(path.join(__dirname,'candidate-parameters-v5.json'),JSON.stringify({experimentOnly:true,
  description:'Long ice buildup, per-hit water cooldown reduction. Replaces v4 timed acceleration; wet itself only provides thunder targeting.',
  parameters:{freezeBase:60,freezeDuration:4,freezeBuildSeconds:14,iceAttacksRequired:28,iceProjectilesRequired:84,
    iceResetGap:15,waterDiscount:.5,ballDiscount:4,waterWet:5,ballWet:15,waterRatio:.9,waterReadyRatio:.7,iceRatio:.9,
    waterInterval:.2,waterCost:1.5,iceInterval:.5,iceCost:4.5,
    ballCost:150,ballCD:15,ballCast:1.35,ballK:1.8,lockCost:100,lockCD:20,lockCast:.2,lockDelay:1,
    lockBaseB:3.2,lockPerSecondB:2.5,thunderCD:120,thunderDuration:30,thunderCast:2,thunderDelay:1,
    thunderStartup:0,thunderDrain:15,thunderBudgetB:48}},null,2)+'\n');
const f=(x,n=2)=>x.toFixed(n),a=stats(baseline.arcane),d=stats(baseline.damage);
const SKILLS=['water','ice','ball','lock','thunder'];
const SKILL_NAMES={water:'水流',ice:'冰锥',ball:'水球',lock:'冰凌锁',thunder:'天雷'};
const baselineScale=r=>r.scale;
for(const r of [baseline.arcane,baseline.damage,hat.arcane,hat.damage]) {
  // Converting damage to B needs skills and basics on the same equipment multiplier.
  assert(Math.abs(r.scale-3500*r.basicMultiplier)<1e-6,'skill and basic attacks share the B scale');
  assert(Math.abs(SKILLS.reduce((n,k)=>n+stats(r).damageShares[k],0)-1)<1e-9,'damage shares sum to 100%');
}
const report=`# 长冰锥阶段、低切换冰法候选

2026-10-14。独立300秒高血量木桩设计实验；没有改动正式模组或实验室页面。

建议每轮在冻结冷却期间持续水流，冷却就绪后切冰锥，完成14秒有效攻击积累，再接冰凌锁、切回水流。水球和天雷优先在水流阶段施放。期间不需要为了续潮湿反复切回水流。

| 参数 | 候选 |
|---|---|
| 冻结CD、持续时间 | 60秒、4秒；从触发冻结起计时 |
| 冰锥冻结门槛 | 14秒有效攻击，28次完整攻击、84发有效命中 |
| 冰锥积累断档 | 超过15秒没有有效冰锥命中清零 |
| 水流 | 每次1.5蓝、0.2秒；目标处于冻结CD（含冻结中的4秒）时0.9B、每次0.18B，否则0.7B、每次0.14B；有效命中立即减冻结CD0.5秒；潮湿5秒 |
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

全奥秘相邻冻结间隔：${baseline.arcane.freezeIntervals.map(x=>f(x,1)).join('、')}秒，包含14秒冰锥积累。天雷实际付费${f(a.thunderPaidSeconds,0)}秒，潮湿覆盖${f(a.wetCoverage*100)}%。

## 各技能伤害占比

按300秒内每个技能实际造成的伤害统计（已含装备倍率），四种配装使用同一推荐循环：

| 技能 | 无帽全奥秘 | 无帽全增伤 | 戴帽全奥秘 | 戴帽全增伤 |
|---|---:|---:|---:|---:|
${SKILLS.map(k=>`| ${SKILL_NAMES[k]} | ${[baseline.arcane,baseline.damage,hat.arcane,hat.damage].map(r=>f(stats(r).damageShares[k]*100)+'%').join(' | ')} |`).join('\n')}

无帽全奥秘的明细。“平均每秒伤害”是300秒平均值，已除去装备倍率，单位为B（毕业B=3500）：

| 技能 | 释放次数 | 占手时间 | 耗蓝 | 平均每秒伤害 | 伤害占比 |
|---|---:|---:|---:|---:|---:|
${SKILLS.map(k=>{const r=baseline.arcane.records[k];return `| ${SKILL_NAMES[k]} | ${r.count} | ${f(r.handSeconds,1)}秒 | ${f(r.spent,0)} | ${f(r.damage/300/baselineScale(baseline.arcane),3)}B | ${f(a.damageShares[k]*100)}% |`;}).join('\n')}
| 合计 | — | ${f(SKILLS.reduce((n,k)=>n+baseline.arcane.records[k].handSeconds,0)+baseline.arcane.idleSeconds,1)}秒（含空闲${f(baseline.arcane.idleSeconds,1)}秒） | ${f(SKILLS.reduce((n,k)=>n+baseline.arcane.records[k].spent,0),0)} | ${f(SKILLS.reduce((n,k)=>n+baseline.arcane.records[k].damage,0)/300/baselineScale(baseline.arcane),3)}B | 100% |

天雷离手攻击，每次只占手施法2秒；冰凌锁每次占手0.2秒。水流和冰锥每秒伤害相同（0.9B），两者的占比差别只来自占手时间。

对照：v4全奥秘水/冰切换${stats(prior.arcane).basicSwitches}次，本候选${a.basicSwitches}次。CD期间继续冰锥、只补潮湿的对照策略为奥秘${f(iceDuringCooldown.arcane.ratio,3)}倍、增伤${f(iceDuringCooldown.damage.ratio,3)}倍，均低于本候选，但其他策略未穷举。

只延长旧版冰锥时间、保留水流0.16秒/命中和天雷34B的对照：

| 冰锥积累 | 奥秘冰锥时间占比 | 奥秘DPS/原版 |
|---|---:|---:|
${durationOnly.map(x=>'| '+x.seconds+'秒 | '+f(stats(x.result.arcane).iceShare*100)+'% | '+f(x.result.arcane.ratio,3)+'倍 |').join('\n')}

因此14秒门槛需要配合更强水系减CD，不能把本结果归因于只增加冰锥时间。

## 首次运行后的微调

首次运行用2.5秒积累断档、水流固定0.9B。看过结果后改为15秒断档，水流只在目标冻结CD中保持0.9B，其余时间0.7B。其他参数不变。

| 场景 | 首次运行 奥秘 / 增伤 | 微调后 奥秘 / 增伤 |
|---|---:|---:|
| 无帽 | ${f(originalBaseline.arcane.ratio,3)} / ${f(originalBaseline.damage.ratio,3)}倍 | ${f(baseline.arcane.ratio,3)} / ${f(baseline.damage.ratio,3)}倍 |
| 戴帽 | ${f(originalHat.arcane.ratio,3)} / ${f(originalHat.damage.ratio,3)}倍 | ${f(hat.arcane.ratio,3)} / ${f(hat.damage.ratio,3)}倍 |

木桩上两项微调几乎不改变结果。推荐循环在冻结CD就绪后一直用冰锥，300秒内只有${a.waterReadyHits}次（增伤${d.waterReadyHits}次）水流命中落在CD外、按0.7B结算；木桩不躲避，积累从不中断，15秒断档在这里没有被触发。它在有停手躲避的战斗里才起作用，见moon-lord-master-2026-10-14/REPORT.md的断档对照：高压力下2.5秒平均每场清零1～4次，15秒不清零。

0.7B规则的作用是惩罚不放冰锥的打法。不放冰锥和冰凌锁、全程水流时，冻结CD从不启动，水流全部按0.7B结算：奥秘${f(waterOnly.arcane.ratio,3)}倍、增伤${f(waterOnly.damage.ratio,3)}倍；水流固定0.9B时同一打法是奥秘${f(S.pair({...original,omit:['ice','lock']}).arcane.ratio,3)}倍。推荐循环比它高${f((baseline.arcane.dps/waterOnly.arcane.dps-1)*100,1)}%。

验证通过：${result.checks.join('；')}。

复跑：node Tools/MageBalance/experiments/ice-build-2026-10-14/run-v5.cjs
`;
fs.writeFileSync(path.join(__dirname,'REPORT-v5.md'),report);
console.log(JSON.stringify({checks:result.checks,timeStats:result.timeStats,lead:baseline.lead,hatLead:hat.lead},null,2));
