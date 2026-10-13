'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const S=require('./simulate-v2.cjs');
const oldOptions={...S.recommended};
const newOptions={...S.recommended,freezeBase:60,waterDiscount:2/15,ballDiscount:6};
function contributions(r) {
  const complete=r.reductions.filter(x=>x.freezeId<r.freezeCount);
  const water=complete.filter(x=>x.kind==='water').reduce((n,x)=>n+x.seconds,0);
  const ball=complete.filter(x=>x.kind==='ball').reduce((n,x)=>n+x.seconds,0);
  const cycles=r.freezeCount-1;
  return {cycles,waterTotal:water,ballTotal:ball,waterPerCycle:water/cycles,ballPerCycle:ball/cycles,waterShare:water/(water+ball),ballShare:ball/(water+ball)};
}
const old=S.pair(oldOptions),updated=S.pair(newOptions),hat=S.pair({...newOptions,hat:true});
const maximal=S.run({...newOptions,unlimitedMana:true,filler:'water',omit:['thunder']});
assert(maximal.freezeIntervals.every(x=>x>29&&x<32));
assert(updated.arcane.ledger.externalMana===0&&updated.damage.ledger.externalMana===0);
const comparison={date:'2026-10-14',duration:300,oldOptions,newOptions,old,updated,hat,
  contributions:{old:contributions(old.arcane),updated:contributions(updated.arcane),maximal:contributions(maximal)},
  maximal,search:S.search(newOptions),withoutWater:S.search({...newOptions,omit:['water']}),
  waterWindows:[5,15,25,35,45,60].map(waterWindow=>({waterWindow,result:S.pair({...newOptions,waterWindow})})),
  shorterWet:{full:S.pair({...newOptions,ballWet:10}),withoutWater:S.search({...newOptions,ballWet:10,omit:['water']})}};
fs.writeFileSync(path.join(__dirname,'results-freeze-60.json'),JSON.stringify(comparison,(key,value)=>key==='reductions'?undefined:value,2)+'\n');
const f=(n,d=2)=>n.toFixed(d),oc=comparison.contributions.old,nc=comparison.contributions.updated,mc=comparison.contributions.maximal;
const report=`# 冻结60秒、水系减CD翻倍：300秒对照

2026-10-14。沿用第二轮的伤害、资源、潮湿时长和施法规则，只改：冻结基础CD 45→60秒；水流单次减CD 1/15→2/15秒；水球单次减CD 3→6秒。正式模组配置未修改。

## 最快水系循环

每轮135次水流命中、2次水球命中：

| 来源 | 原先 | 改后 |
|---|---:|---:|
| 水流减少 | 9秒 | 18秒 |
| 水球减少 | 6秒 | 12秒 |
| 合计减少 | 15秒 | 30秒 |
| 自然计时所需 | 约30秒 | 约30秒 |
| 水系占基础CD | 33.3% | 50% |

独立无限魔力、连续水系校准的实际冻结间隔仍约${f(maximal.freezeIntervals[0])}秒。没有30秒硬限制。这是纯频率校准，不是正式有限资源输出结果。

## 正常循环的减CD贡献（全奥秘）

只统计从一次冻结到下次冻结的完整间隔，不混入开场首次冻结、结束未完成计时。

| 项目 | 原先45秒 | 改后60秒 |
|---|---:|---:|
| 水流平均每轮减CD | ${f(oc.waterPerCycle)}秒 | ${f(nc.waterPerCycle)}秒 |
| 水球平均每轮减CD | ${f(oc.ballPerCycle)}秒 | ${f(nc.ballPerCycle)}秒 |
| 水流占总减CD | ${f(oc.waterShare*100)}% | ${f(nc.waterShare*100)}% |
| 水球占总减CD | ${f(oc.ballShare*100)}% | ${f(nc.ballShare*100)}% |
| 300秒冻结次数 | ${old.arcane.freezeCount} | ${updated.arcane.freezeCount} |

改后全奥秘实际冻结间隔：${updated.arcane.freezeIntervals.map(x=>f(x)).join('、')}秒。与最快水系循环不同，因为正常输出仍需给冰锥、天雷等让出施法时间。

两者的绝对减CD量都提高；水球占比进一步增加。基础CD变长让按15秒CD放水球有机会多命中一轮，而水流只在维护潮湿和临近冻结窗口时使用。减CD系数翻倍不保证两者在最佳输出中的相对价值都翻倍。

## 输出结果

原版基准${f(updated.arcane.vanilla)} DPS；300秒不受击，五瓶30%药剂。

| 场景 | 全奥秘 | 全增伤 |
|---|---:|---:|
| 原先无帽 | ${f(old.arcane.dps)} / ${f(old.arcane.ratio,3)}倍 | ${f(old.damage.dps)} / ${f(old.damage.ratio,3)}倍 |
| 改后无帽 | ${f(updated.arcane.dps)} / ${f(updated.arcane.ratio,3)}倍 | ${f(updated.damage.dps)} / ${f(updated.damage.ratio,3)}倍 |
| 改后带帽 | ${f(hat.arcane.dps)} / ${f(hat.arcane.ratio,3)}倍 | ${f(hat.damage.dps)} / ${f(hat.damage.ratio,3)}倍 |

改后无帽奥秘相对增伤${f(updated.lead*100)}%，带帽${f(hat.lead*100)}%。本次没有为抵消冻结次数下降而提高冰凌锁伤害。

## 水流是否更值得保留

双方分别取所测30种规则中最高值：

| 场景 | 全奥秘 / 原版 | 全增伤 / 原版 |
|---|---:|---:|
| 改后完整组 | ${f(comparison.search.arcane.ratio,3)} | ${f(comparison.search.damage.ratio,3)} |
| 移除水流 | ${f(comparison.withoutWater.arcane.ratio,3)} | ${f(comparison.withoutWater.damage.ratio,3)} |

全奥秘移除水流后仅下降约${f((1-comparison.withoutWater.arcane.dps/comparison.search.arcane.dps)*100)}%，因此“水系整体贡献更大”已实现，但“水流本身更不可替代”尚未明显实现。未替换为空槽buff，也未搜索所有技能组。

附加试验：水球潮湿持续从15秒缩为10秒，其他新参数不变，完整参考循环为${f(comparison.shorterWet.full.arcane.ratio,3)} / ${f(comparison.shorterWet.full.damage.ratio,3)}倍；去掉水流后各自择优为${f(comparison.shorterWet.withoutWater.arcane.ratio,3)} / ${f(comparison.shorterWet.withoutWater.damage.ratio,3)}倍。这能加强水流补潮湿的价值，但属于另一个改动，没有混入主结果。

运行：

\`\`\`powershell
node Tools/MageBalance/experiments/ice-build-2026-10-14/compare-freeze-60.cjs
\`\`\`

其他机制假设与验证沿用REPORT-v2.md和simulate-v2.cjs；这仍是独立理论试算，没有接入游戏代码或页面。
`;
fs.writeFileSync(path.join(__dirname,'REPORT-freeze-60.md'),report);
console.log(JSON.stringify({old:S.compact(old),updated:S.compact(updated),hat:S.compact(hat),contributions:comparison.contributions},null,2));
