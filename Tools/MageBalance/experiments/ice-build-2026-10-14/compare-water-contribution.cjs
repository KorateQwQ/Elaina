'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const S=require('./simulate-v3.cjs');
const proposed={...S.recommended,waterDiscount:.16,ballDiscount:4};
const previous=S.pair(S.recommended),candidate=S.pair(proposed),hat=S.pair({...proposed,hat:true});
function contribution(r) {
  const list=r.reductions.filter(x=>x.freezeId<r.freezeCount);
  const sum=kind=>list.filter(x=>x.kind===kind).reduce((v,x)=>v+x.seconds,0);
  const water=sum('water'),ball=sum('ball'),cycles=r.freezeCount-1;
  return {cycles,waterPerCycle:water/cycles,ballPerCycle:ball/cycles,waterShare:water/(water+ball)};
}
const best=S.search(proposed),hatBest=S.search({...proposed,hat:true});
const rules=['water','mixed','ice'].map(filler=>({filler,result:S.search({...proposed,filler})}));
assert(rules[0].result.arcane.dps>rules[1].result.arcane.dps&&rules[0].result.arcane.dps>rules[2].result.arcane.dps);
assert(rules[0].result.damage.dps>rules[1].result.damage.dps&&rules[0].result.damage.dps>rules[2].result.damage.dps);
assert(contribution(candidate.arcane).waterShare>contribution(previous.arcane).waterShare);
assert(candidate.arcane.freezes.every(f=>f.iceSeconds>=3));
const calibration=S.run({...proposed,unlimitedMana:true,omit:['thunder']});
assert(calibration.freezeIntervals.every(x=>x>33&&x<34));
const faster=S.run({...proposed,unlimitedMana:true,omit:['thunder'],waterInterval:.1});
assert(faster.freezeIntervals.every(x=>x<30));
const result={date:'2026-10-14',duration:300,proposed,previous,candidate,hat,best,hatBest,rules,calibration,
  contribution:{previous:contribution(previous.arcane),candidate:contribution(candidate.arcane)},
  withoutWater:S.search({...proposed,omit:['water']}),withoutBall:S.search({...proposed,omit:['ball']})};
fs.writeFileSync(path.join(__dirname,'results-water-contribution.json'),JSON.stringify(result,(k,v)=>k==='reductions'?undefined:v,2)+'\n');
fs.writeFileSync(path.join(__dirname,'candidate-water-contribution.json'),JSON.stringify({experimentOnly:true,description:'User-endorsed direction: water owns freeze-CD progression, ice3s owns freeze readiness. Formal mod not edited.',parameters:{...S.defaults,...proposed}},null,2)+'\n');
const f=(n,d=2)=>n.toFixed(d),old=result.contribution.previous,next=result.contribution.candidate;
const report=`# 水流主导减CD：0.16秒 / 水球4秒

2026-10-14。用户认可此调整方向后，以第三轮状态模型重测；300秒、不受击、五瓶30%药剂，正式配置未修改。

本次只改变水流每次实际命中减CD为0.16秒，水球每次实际命中减CD为4秒。冻结基础CD60秒、CD后冰锥至少3秒、冻结4秒和其他伤害/耗蓝参数都保持第三轮推荐。

## 分工验证

| 全奥秘正常循环，完整冻结间隔统计 | 原先0.1333 / 6秒 | 候选0.16 / 4秒 |
|---|---:|---:|
| 水流平均每轮减CD | ${f(old.waterPerCycle)}秒 | ${f(next.waterPerCycle)}秒 |
| 水球平均每轮减CD | ${f(old.ballPerCycle)}秒 | ${f(next.ballPerCycle)}秒 |
| 水流占总减CD | ${f(old.waterShare*100)}% | ${f(next.waterShare*100)}% |

水流已从约60%提升到约73%的减CD贡献，水球仍能一次推进4秒，并提供伤害和潮湿。

候选正常循环冻结间隔${candidate.arcane.freezeIntervals.map(x=>f(x)).join('、')}秒；最快水系速度校准约${f(calibration.freezeIntervals[0])}秒，包含随后3秒冰锥积累。约30秒是CD阶段的结果，不是引擎硬限制。测试水流0.1秒周期仍能得到小于30秒的完整冻结间隔。

## 输出结果

| 场景 | 全奥秘DPS / 原版倍数 | 全增伤DPS / 原版倍数 |
|---|---:|---:|
| 原先无帽 | ${f(previous.arcane.dps)} / ${f(previous.arcane.ratio,3)} | ${f(previous.damage.dps)} / ${f(previous.damage.ratio,3)} |
| 候选无帽 | ${f(candidate.arcane.dps)} / ${f(candidate.arcane.ratio,3)} | ${f(candidate.damage.dps)} / ${f(candidate.damage.ratio,3)} |
| 候选带帽 | ${f(hat.arcane.dps)} / ${f(hat.arcane.ratio,3)} | ${f(hat.damage.dps)} / ${f(hat.damage.ratio,3)} |

同一简单循环奥秘差距：无帽${f(candidate.lead*100)}%，带帽${f(hat.lead*100)}%。提高水流价值也帮助少放水球的增伤配装追赶冻结频率，因此不能假定此次分工调整对前缀对比完全中性。

双方各自选择所测更高输出规则后：无帽${f(best.arcane.ratio,3)} / ${f(best.damage.ratio,3)}倍，奥秘差距${f(best.lead*100)}%；带帽${f(hatBest.arcane.ratio,3)} / ${f(hatBest.damage.ratio,3)}倍，奥秘差距${f(hatBest.lead*100)}%。带帽增伤是否超过2倍应以这行结果为准，不能只取参考循环称已完全达标。

## 简单切换是否仍更赚

所有策略CD结束后都至少冰锥3秒，每种CD期普攻倾向分别优化所测资源策略：

| CD期普攻倾向 | 无帽奥秘 / 原版 | 无帽增伤 / 原版 |
|---|---:|---:|
${rules.map(x=>`| ${{water:'持续水流',mixed:'冰锥为主，末段水流',ice:'持续冰锥，补潮湿才用水流'}[x.filler]} | ${f(x.result.arcane.ratio,3)} | ${f(x.result.damage.ratio,3)} |`).join('\n')}

候选方向保留了用户要求的切换：CD期水系、就绪后冰锥、冻结后引爆。

去掉水流并重新选择所测最高输出规则：${f(result.withoutWater.arcane.ratio,3)} / ${f(result.withoutWater.damage.ratio,3)}倍。去掉水球：${f(result.withoutBall.arcane.ratio,3)} / ${f(result.withoutBall.damage.ratio,3)}倍。两者仍有组合价值，但尚未用其他技能或buff替换空槽。

## 复跑

\`\`\`powershell
node Tools/MageBalance/experiments/ice-build-2026-10-14/compare-water-contribution.cjs
\`\`\`

计算沿用simulate-v3.cjs及其每次模拟的魔力守恒、占手时间、逐次冷却、3秒积累、引爆唯一性、持续天雷付款检查；本次另断言两种前缀均以CD期水系为所测更高收益、减CD份额确实移向水流、加速后可低于30秒。本实验仍不模拟真实投射物飞行、随机逐发命中、目标防御、前期成长或游戏联机。
`;
fs.writeFileSync(path.join(__dirname,'REPORT-water-contribution.md'),report);
console.log(JSON.stringify({candidate:S.compact(candidate),hat:S.compact(hat),best:S.compact(best),hatBest:S.compact(hatBest),contribution:result.contribution},null,2));
