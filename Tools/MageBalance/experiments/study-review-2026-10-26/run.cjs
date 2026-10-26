'use strict';
// Analysis only. Runtime skill prices, grants and recipes are never written.
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../../../..');
const read = p => fs.readFileSync(path.join(root, p), 'utf8');
const ctx = {module: {exports: {}}};
vm.runInNewContext(read('Tools/MageBalance/index.html').match(/<script id="balance-engine">([\s\S]*?)<\/script>/)[1], ctx);
const B = ctx.module.exports;
const config = JSON.parse(read('ElainaModSkills/Data/SkillBalance.json')).configuration;
const entries = [...config.basicAttacks, ...config.skills];
// key, runtime ID, label, discussion learning cost, discussion per-upgrade cost.
const skills = [
  ['missile','MagicMissileSkill','飞弹',0,5],
  ['barrier','MagicBarrierSkill','屏障',10,0],
  ['multi','MultiMissileSkill','多重',20,15],
  ['alchemy','AlchemySkill','炼金',10,15],
  ['doll','DollControlSkill','人偶',20,15],
  ['flight','FlightSkill','飞行',15,10],
  ['wind','WindSlashSkill','风刃',20,5],
  ['heal','HealSkill','治疗',20,15],
  ['stream','WaterLaserSKill','水流',25,5],
  ['ball','WaterBallSkill','水球',25,15],
  ['recall','RecallSkill','回城',20,0],
];
const row = key => entries.find(r => r.id === skills.find(s => s[0] === key)[1]);
function cap(key, level) {
  if (key === 'barrier') return level >= 5 ? 1 : 0;
  if (key === 'recall') return 1; // Conditional: discovered and owns a Return Potion.
  if (key === 'multi' && level < 10) return 0;
  return B.progressionNodes(row(key), config.progression.phaseSize, level).length;
}
const baseSource = read('ElainaModSkills/ElainaSkill.cs');
const upgradeSource = read('ElainaModSkills/ElainaSkill.Constellation.cs');
const recallSource = read('ElainaModSkills/Skills/Recall/RecallSkill.Research.cs');
const runtimeLearn = +baseSource.match(/DefaultUnlockCost\s*=>[\s\S]*?ByItemsAndSkillPoint\((\d+)/)[1];
const runtimeUpgrade = +upgradeSource.match(/GetConstellationUpgradeCondition\(int nextLevel\)\s*=>[\s\S]*?ByItemsAndSkillPoint\((\d+)/)[1];
const runtimeRecall = +recallSource.match(/ByItemsAndSkillPoint\((\d+)/)[1];
function cost(levels, runtime = false) {
  return skills.reduce((sum, [key,,,learn,upgrade]) => {
    const level = levels[key] || 0;
    if (!level) return sum;
    if (runtime) {
      learn = key === 'missile' ? 0 : key === 'recall' ? runtimeRecall : runtimeLearn;
      upgrade = runtimeUpgrade;
    }
    return sum + learn + (level - 1) * upgrade;
  }, 0);
}
const stages = [5,10,15,20,25,30,35];
const currentIncome = [50,120,190,280,380,550,680];
const candidateIncome = [50,110,180,260,360,500,680];
const all = stages.map(level => Object.fromEntries(skills.map(([key]) => [key,cap(key,level)])));
const stageResults = stages.map((level,i) => ({level, levels:all[i], cost:cost(all[i]),
  costWithoutRecall:cost({...all[i],recall:0}), runtimePointCost:cost(all[i],true),
  discussedIncome:currentIncome[i], candidateIncome:candidateIncome[i],
  discussedBalance:currentIncome[i]-cost(all[i]), candidateBalance:candidateIncome[i]-cost(all[i])}));
// Two affordable examples, deliberately preserving past purchases rather than respecing.
const combat = [
  {missile:3,barrier:1},
  {missile:5,barrier:1,multi:2,alchemy:2,doll:1},
  {missile:7,barrier:1,multi:3,alchemy:2,doll:3,flight:1},
  {...all[3],recall:0,alchemy:3,wind:2},
  {...all[4],recall:0,doll:4},
  {...all[5],recall:0,doll:5},
  {...all[6]},
];
const utility = [
  {missile:3,barrier:1},
  {missile:5,barrier:1,alchemy:2,doll:2,recall:1},
  {missile:5,barrier:1,multi:1,alchemy:3,doll:3,flight:1,recall:1},
  {...all[3],missile:7,multi:2},
  {...all[4],missile:10,multi:3},
  {...all[5],missile:12,multi:4},
  {...all[6]},
];
let checks = 0;
const check = (condition,message) => {assert(condition,message); checks++;};
for (const route of [combat,utility]) for (let i=0;i<stages.length;i++) {
  check(cost(route[i]) <= candidateIncome[i], `Budget at ${stages[i]}`);
  for (const [key] of skills) {
    check((route[i][key]||0) <= all[i][key], `Cap ${key} at ${stages[i]}`);
    if (i) check((route[i][key]||0) >= (route[i-1][key]||0), `No respec ${key} at ${stages[i]}`);
  }
  check(!route[i].multi || route[i].missile>=5, 'Missile 5 prerequisite');
}
// Read the actual efficiency constants and guard the formulas being transcribed.
const efficiencySource = read('ElainaModAlchemy/Gameplay/AlchemyEfficiency.cs');
const efficiencyMax = +efficiencySource.match(/MaximumLevel = (\d+)/)[1];
const slowest = +efficiencySource.match(/SlowestDurationTicks = (\d+)/)[1];
const fastest = +efficiencySource.match(/FastestDurationTicks = (\d+)/)[1];
const yieldStep = +efficiencySource.match(/BonusYieldChancePercent\(int level\) => (\d+) \*/)[1];
check(efficiencySource.includes('(MaximumLevel - 1)'), 'Linear efficiency formula unchanged');
check(efficiencySource.includes('(SlowestDurationTicks / (double)BrewingDurationTicks(level) - 1) * 100'), 'Displayed speed formula unchanged');
const progressionSource = read('ElainaModAlchemy/Gameplay/AlchemyProgressionRules.cs');
const neededLevel = id => +progressionSource.match(new RegExp('"'+id+'"\\s*=>\\s*(\\d+)'))[1];
check(neededLevel('featherlight')===4 && neededLevel('painkiller')===5, 'Current recipe milestones');
const alchemy = B.progressionNodes(row('alchemy'),config.progression.phaseSize).slice(0,efficiencyMax).map((level,i) => {
  const actualLevel=i+1;
  const ticks=Math.round(slowest-(slowest-fastest)*i/(efficiencyMax-1));
  return {actualLevel,characterLevel:level,pointCost:10+15*i,ticks,
    speedBonus:100*(slowest/ticks-1),bonusYieldPercent:yieldStep*i,
    experienceFromLevel1:100*i+25*i*(i-1)};
});
const damage = stages.filter(l=>l>=20).map(level => ({level,skills:
  ['missile','wind','stream','ball','multi'].flatMap(key => {
    const nodes=B.progressionNodes(row(key),config.progression.phaseSize,level);
    if (!nodes.length) return [];
    const r=row(key), budget=B.progressionBudget(config,r,nodes.at(-1));
    const interval=r.interval??r.cd;
    const modifier=key==='stream'?0.7/0.9:1;
    return [{key,skillLevel:nodes.length,baseHit:budget.integerHit,
      ordinaryTargetDps:budget.integerHit*modifier/interval,manaPerSecond:r.cost/interval}];
  })}));
check(stageResults.at(-1).cost===650, 'Discussion total 650');
check(stageResults.at(-1).runtimePointCost===1210, 'Runtime total 1210');
check(stageResults[3].discussedIncome===stageResults[3].costWithoutRecall, 'Lv20 all except recall');
check(stageResults.every((r,i)=>!i || candidateIncome[i]>candidateIncome[i-1]), 'Increasing candidate grant budget');
const sourcePaths = ['ElainaModSkills/Data/SkillBalance.json','ElainaModSkills/ElainaSkill.cs',
  'ElainaModSkills/ElainaSkill.Constellation.cs','System/ElainaStatePlayer.cs',
  'ElainaModAlchemy/Gameplay/AlchemyEfficiency.cs','ElainaModAlchemy/Gameplay/AlchemyProgressionRules.cs',
  'ElainaModAlchemy/Gameplay/AlchemyCraftingService.cs','ElainaModAlchemy/Gameplay/AlchemyBrewingPlayer.cs',
  'ElainaModSkills/Skills/Water/WaterLaser.cs'];
const sources=sourcePaths.map(p=>({path:p,sha256:crypto.createHash('sha256').update(read(p)).digest('hex')}));
const results={date:'2026-10-26',status:'analysis only; candidate grants/prices not implemented',checks,
  runtimePrices:{learn:runtimeLearn,upgrade:runtimeUpgrade,recall:runtimeRecall},stageResults,alchemy,damage,
  routes:{combat,utility},sources};
fs.writeFileSync(path.join(__dirname,'results.json'),JSON.stringify(results,null,2)+'\n');
const table=(head,rows)=>[head,head.map(()=>'---'),...rows].map(r=>'| '+r.join(' | ')+' |').join('\n');
const describe=levels=>skills.filter(([key])=>levels[key]).map(([key,,label])=>label+levels[key]).join('、');
const report=`# 研习点与肉前技能配置复评

日期：2026-10-26。读取当前工作区正式源码与 JSON；只生成分析文件，不修改发点、费用、技能或炼金配方。当前讨论收入仍为50/120/190/280/380/550/680；本报告另给一版同总量对照，不代表用户已确认。

## 结论与旧报告修正

680点覆盖650点的终局目标成立。现有讨论分配在Lv20～25仍然接近全满，Lv30即可全满。若目标是肉前后段才全部补齐，先小幅延后发点，无须先加价。

上一份study-choices报告的炼金收益已过时：轻羽现在需炼金4、止痛剂需炼金5；每个实际炼金等级都提高产出概率和炼制速度。不能再剔除4～7级的60点，称其为“无即时收益”的消费。原报告中的显迹尘骨头材料也已过时，当前为闪耀根1＋晶状体1→3。

## 游戏实际实现与讨论方案

- 正式角色升级入口和等级归一化仅结算经验，未接入升级发研习点；KL的SkillPoint初始默认0，仅保存余额不构成发点渠道。调试按钮和忘却退款不是新角色升级收入。
- 正式默认首次学习为20点＋10木材，升级20点；初始飞弹免费，回城50点＋1瓶返回药水。按肉前完整节点计算是1210点，不是讨论价格的650点。
- 本次预算继续使用此前的建议价格：飞弹升级5；屏障10；多重20/15；炼金10/15；人偶20/15；飞行15/10；风刃20/5；治疗20/15；水流25/5；水球25/15；回城20。斜线为首次/每次升级费用。
- 风刃、治疗、水流、水球正式首次学习主要检查阶段上限，不统一检查实际等级。预算按JSON节点对应等级展开；这不等于这些首次等级要求已接入。治疗仅按阶段解锁已有独立说明，不应为了统一外观而直接改掉。
- 多重要求已学飞弹5，最早角色10；JSON首级6不能视为实际可买节点。冰锥和火焰射线配置为阶段40/角色36，冰锥另需寒霜核，不计入普通肉前流程。

## 阶段预算对照

“全满”是该等级下已开放技能全部升到当前上限，含回城；不包含肉后技能。没有发现返回药水时，完整需求减少20。阶段标签沿用KL原版备用上限，Boss Checklist及其他模组可能改变映射。

${table(['角色等级','讨论累计收入','讨论价全满需求','不含回城需求','含回城余额','对照累计收入','对照余额'],stageResults.map(r=>[r.level,r.discussedIncome,r.cost,r.costWithoutRecall,r.discussedBalance,r.candidateIncome,r.candidateBalance]))}

- Lv20：没有回城时，280恰好买满全部其余内容，已包括炼金4、轻羽的等级资格。
- Lv25：没有回城时375点全满，当前380还余5；有回城只需延后一次15点升级。中段压力已很轻。
- Lv30：535点全满，当前550余15。Lv35新增115点需求，收入却增加130，只会继续扩大余额。
- 回城发现条件有随机性，不宜依赖这20点维持全体玩家的取舍。未发现时也应能观察到选择。

同总量对照为50/110/180/260/360/500/680：Lv10～30含回城始终缺35～40点；不含回城仍缺15～20点，约为一两次进修。Lv35再补齐并余30。这是一版温和取舍，不要求长期舍弃完整技能线。

该对照若采用分段平发，可以在Lv2～4各发10、Lv5发20；随后Lv6～10每级12、11～15每级14、16～20每级16、21～25每级20、26～30每级28、31～35每级36。这里只给可复现的候选，不决定最终逐级规则；只在阶段末一次性发放会让阶段中间缺乏反馈。

Lv35是肉前完整覆盖上界，不是强制肉山前练满或补杀独眼巨鹿。Lv30进入困难模式也须可玩；对照此时只缺35点，不应以全满作为挑战肉山的前提。肉后发点规则与旧档迁移另行设计。

## 当前炼金收益重新计价

以下为实际制作等级已练到对应技能上限的数值，花研习点本身只提高上限，不直接获得这些效率。

${table(['实际炼金等级','最早角色节点','累计建议点数','炼制速度加成','额外一批概率','从1级累计制作经验','主要新增资格'],alchemy.map(r=>[r.actualLevel,r.characterLevel,r.pointCost,'+'+r.speedBonus.toFixed(1)+'%',r.bonusYieldPercent+'%',r.experienceFromLevel1,({1:'开启大锅、月露合剂等一级条目',2:'显迹之尘',3:'微光滴管、灰赝尘',4:'轻羽药水（仍需卷轴）',5:'止痛剂（仍需卷轴）',6:'通用效率成长',7:'通用效率成长',8:'肉后职业药剂等'})[r.actualLevel]]))}

产出率是每个成功批次独立判定是否多给一份基础产量，不是每瓶判定；同轮批量使用结算前等级，额外产出不给额外制作经验。炼金3→7的期望成品倍率为1.30/1.10，即同材料约多18.2%，每份成品期望材料成本约少15.4%。随机收益需要足够批次数才接近期望，不能保证每次回本。

炼制速度缩短一次制作操作的等待；当前1～99批一次结算，不能把速度倍率直接解释为资源产量倍率或要求玩家逐批刷动画。炼金6/7没有新增配方，收益偏备药、节材和提前练级，因此有价值但不必强迫每条战斗路线立即点满。

炼金1→5需700制作经验，1→7需1350；只靠每批15经验的月露合剂、忽略分阶段封顶浪费，分别至少47/90批，并会产出大量药剂。中间材料也给经验，可改变实际批数；应实测正常备药是否足够达到新配方等级，避免把研习点问题变成制作熟练度刷取问题。

## 肉前战斗技能的定位

以下使用正式成长引擎和整数基础伤害，零防御、无装备/暴击/急速、全命中。水流对没有冻结冷却的普通目标乘0.7/0.9，按主目标计算；溅射另乘50%。所有周期值都是连续使用参考，不是实际Boss战DPS，也不能直接相加。

${table(['角色等级','飞弹基础DPS / 蓝每秒','风刃基础DPS / 蓝每秒','水流主目标基础DPS / 蓝每秒','水球基础伤害 / 基础CD'],damage.map(r=>{const f=k=>r.skills.find(s=>s.key===k),d=k=>f(k)?f(k).ordinaryTargetDps.toFixed(1)+' / '+f(k).manaPerSecond.toFixed(2):'未开放';return[r.level,d('missile'),d('wind'),d('stream'),f('ball')?f('ball').baseHit+' / '+row('ball').cd+'秒':'未开放'];}))}

- 飞弹：便宜且通用的单体主力；保留每次5点升级合理。多重必须飞弹5，转修其他普攻后仍存在最低20点的前置投入，不应称为完全独立的技能线。
- 风刃：穿透、击退和沿线多目标。Lv20单体55对飞弹96.7，二者耗蓝6.25/6.67；风刃的价值主要来自多目标，不靠单体取代飞弹。先确认关卡中穿透收益，暂无必要仅为制造点数消费而增强或加价。
- 水流：肉前自身尚无冰锥，不能按完整冰水循环宣传收益。Lv30主目标140对飞弹170，且耗蓝更高；Lv35为175对203.3。溅射、瞄准和范围是它的购买理由，不能因为0.9B面板就判断必然替代飞弹，也不能据此直接判废。优先做相同配装下实战命中与多目标比较。
- 水球：一次100魔力、15秒基础冷却，提供爆发和范围；Lv30/35基础伤害1350/1687。与屏障、治疗共享魔力，应观察能否在受伤后正常施放。冰水返还对肉前单人技能组没有完整兑现条件。
- 多重：8魔力、0.16秒基础冷却，按CD连续用会提出50蓝/秒需求，远高于飞弹6.67。动作、法球寻找目标、同时最多5枚和实际回蓝都限制使用，不能拿面板周期DPS当免费叠加输出。建议维持20/15价格，先测真实使用频率。
- 人偶：1/3/5/7级为1/2/3/4只，2/4/6级主要给动作速度。15点进修的数量档通常更有吸引力；允许玩家为了下一只攒点，先保留当前价格，观察速度档是否被长期跳过。
- 飞行：一级已经提供扫帚加速和冲刺；后续主要提高基础速度。15/10的门槛轻，适合作为克眼后直接可取的移动能力。无需为了延长取舍先抬高一级费用。
- 治疗与止痛剂：前者主动支付120魔力，10秒100/120治疗、60秒基础CD；后者消耗备药、持续恢复且需炼金5与卷轴。二者来源和使用时机不同；现有阶段安排提供主动与补给两个选择，暂不相互替换。

## 对照预算下的连续加点样例

均为累计等级、累计花费，未列技能未学，灰之魔女默认已学；只验证阶段末，尚未模拟每一等级到点和每件材料的实际取得时间。炼金数字是技能上限，实际制作等级仍需经验。探索路线假定已经取得返回药水；未取得时可保留这20点，不视为固定开局资源。

${table(['路线','角色等级','累计配置','累计花费','余点'],stages.flatMap((level,i)=>[['战斗优先',level,describe(combat[i]),cost(combat[i]),candidateIncome[i]-cost(combat[i])],['探索便利优先',level,describe(utility[i]),cost(utility[i]),candidateIncome[i]-cost(utility[i])]]))}

Lv20战斗路线保飞弹9、多重4、人偶4，暂缓炼金4和回城、风刃留2；探索路线保炼金4和回城，以飞弹7、多重2交换轻羽资格与便利功能。Lv25战斗路线可补炼金5，但人偶留4；探索路线人偶5，飞弹/多重略低。两条到Lv35都能补齐，且不倒退技能等级。

这证明对照预算存在可行的连续路线，不证明两条收益已经等价或最优。战斗路线选择延后回城属于玩家主动取舍，探索路线回城发现则是外部随机条件。

## 后续决策顺序与验证边界

1. 维持650的建议费用总量及680的肉前完整收入上界，先选现有宽松分配或温和延后分配；当前倾向后者。
2. 把实际发点与建议价格接入后再测闲置点数。跨级、封顶储存后升级、保存加载和旧档已结算记录必须使用同一结算规则；消费后的余额不能充当历史发放记录。
3. 新角色重点记录Lv15/20/25/30的技能、余点、药剂是否能按时研发、人偶速度档使用情况，以及水流/风刃的实际使用率。
4. 若点数有竞争而水流/风刃依然没人购买，再讨论技能本身；不要同时改收入、价格和伤害，以免无法判断原因。

本次${checks}项脚本断言通过，覆盖两条路线的预算、阶段上限、飞弹前置、等级不倒退及关键总价；伤害复用MageBalance节点/定价引擎，炼金效率从正式常量和已核对公式计算。不是新一轮完整模组编译、FNA预览、游戏内流程或双客户端联机验证；本次未修改正式配方或资源。现有炼金预览README还记录了轻羽/止痛剂改门槛后尚未重跑相关场景，不能把本报告当作该验证已补做。

复现：\`node Tools/MageBalance/experiments/study-review-2026-10-26/run.cjs\`。输入来源与SHA-256记录于results.json，便于识别后续源码变化。
`;
fs.writeFileSync(path.join(__dirname,'REPORT.md'),report);
console.log(JSON.stringify({checks,stageResults:stageResults.map(({levels,...r})=>r),report:path.join(__dirname,'REPORT.md')},null,2));
