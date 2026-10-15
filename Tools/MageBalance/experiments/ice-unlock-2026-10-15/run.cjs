'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {B}=require('../ice-build-2026-10-14/simulate.cjs');
const root=path.resolve(__dirname,'../../../..');
const configPath=path.join(root,'ElainaModSkills/Data/SkillBalance.json');
const configText=fs.readFileSync(configPath,'utf8');
const config=JSON.parse(configText).configuration;
const npc=JSON.parse(fs.readFileSync(path.join(__dirname,'boss-stats.json'),'utf8'));
assert.equal(crypto.createHash('sha256').update(fs.readFileSync(npc.source.dll)).digest('hex'),npc.source.sha256,
  'Game assembly changed; refresh NPC data with probe-npcs.cjs');
const freezeSource=fs.readFileSync(path.resolve(root,'../KL/DamageSystem/ElementBuildUpSystem.cs'),'utf8');
const registration=freezeSource.match(/RegisterHitCount\(\s*ElementType\.Ice,\s*ModContent\.BuffType<ElementalAffliction_Freeze>\(\),\s*\d+,\s*(\d+)f,\s*(\d+)f/);
assert(registration,'Read the current runtime freeze threshold');
const threshold=Number(registration[1])/Number(registration[2]);
const actionSource=fs.readFileSync(path.join(root,'ElainaActions/Action_SimpleSlash.cs'),'utf8');
const actionFrames=Number(actionSource.match(/Action_SimpleSlash\(\)\s*:\s*base\((\d+)\)/)[1]);
const iceConfig=config.basicAttacks.find(x=>x.id==='IceConeSkill');
assert.equal(iceConfig.interval,actionFrames/60,'Budget interval agrees with actual action duration');
const scenarios=[
  {name:'骷髅王头部（双手健在）',type:35,stage:5,defenses:[60,50],note:'平时60防，旋转50防；列出固定防御上下界'},
  {name:'骷髅王头部（双手已毁）',type:35,stage:5,defenses:[10,0],note:'平时10防，旋转0防；此前打手的伤害不计入头部掉血'},
  {name:'骷髅王单只手',type:36,stage:5,defenses:[14],note:'只计算单部位理论伤害；共享积蓄下手死亡后的转火见REPORT-shared-54-hat.md'},
  {name:'独眼巨鹿',type:668,stage:6,defenses:[10],note:'无敌期间不算有效命中'},
  {name:'血肉墙（嘴）',type:113,stage:7,defenses:[18],note:'嘴与眼共享血量；这里假设有效命中都打嘴'},
  {name:'血肉墙（眼）',type:114,stage:7,defenses:[6],note:'嘴与眼共享血量；这里假设有效命中都打眼'},
  {name:'史莱姆皇后',type:657,stage:8,defenses:[26],note:'用首次挑战的肉后阶段配装'},
  {name:'双子魔眼·激光眼',type:125,stage:9,defenses:[10],note:'仅第一形态；两只眼血量与积蓄独立'},
  {name:'双子魔眼·魔焰眼',type:126,stage:9,defenses:[10],note:'仅第一形态；两只眼血量与积蓄独立'},
];
const rows=[];
for(const sc of scenarios)for(const prefix of ['configured','damage'])for(const learned of ['first','cap']) {
  const s=structuredClone(config);B.applyLoadout(s,sc.stage);
  // Master has six pre-hardmode slots and seven after the Demon Heart.
  s.slots=sc.stage>=8?7:6;
  // The exclusive hat is the default equipment, occupying one real accessory slot.
  if(!s.e.items.slice(0,s.slots).some(x=>x.id==='elainaHat')) {
    const slot=s.e.items.slice(0,s.slots).findIndex(x=>x.id==='utility'||x.id==='empty');
    assert(slot>=0,'Need an available utility slot for the hat');
    s.e.items[slot].id='elainaHat';
  }
  assert.equal(s.e.items.slice(0,s.slots).filter(x=>x.id==='elainaHat').length,1);
  if(prefix==='damage')B.applyDamagePrefixes(s,'e');
  const g=B.gear(s,'e'),ice=s.basicAttacks.find(x=>x.id==='IceConeSkill');
  const nodes=B.progressionNodes(ice,s.progression.phaseSize,sc.stage*5);
  const level=learned==='first'?nodes[0]:nodes.at(-1);
  const budget=B.progressionBudget(s,ice,level);
  // Runtime floors after the player's additive damage modifier, before splitting crit/defense.
  const projectileDamage=Math.max(1,Math.floor(Math.fround(Math.fround(budget.total)*Math.fround(1+g.damage/100))/ice.shots));
  const target=npc.rows.find(x=>x.mode===2&&x.type===sc.type);
  const damages=sc.defenses.map(def=>{
    const [normal,crit]=npc.damage[def][projectileDamage];
    const average=normal*(1-g.crit/100)+crit*g.crit/100;
    return {defense:def,normal,crit,expectedPerHit:average,total:average*threshold,percent:average*threshold/target.life*100,
      expectedHitsToKill:Math.ceil(target.life/average),aliveAtThreshold:average*threshold<target.life};
  });
  rows.push({...sc,prefix,learned,hp:target.life,characterCap:sc.stage*5,damageLevel:level,
    skillLevel:learned==='first'?1:nodes.length,armor:B.armors[s.e.armor].name,items:s.e.items.slice(0,s.slots),
    damageBonus:g.damage,critChance:g.crit,projectileDamage,damages});
}
// 20k seeded negative-binomial samples: current hit threshold, three shots per action.
// Includes the actual spawn offsets and 25-frame retreat, excludes forward travel and boss movement.
let seed=0x15c0ffee;
function random(){seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;}
const timing=[1,.75,.5].map(hitChance=>{
  const samples=[];
  for(let n=0;n<20000;n++) {
    let successes=0,index=0;
    while(successes<threshold){if(random()<hitChance)successes++;index++;}
    const last=index-1;
    samples.push((Math.floor(last/3)*actionFrames+[1,4,7][last%3]+25)/60);
  }
  samples.sort((a,b)=>a-b);
  return {hitChance,mean:samples.reduce((a,b)=>a+b,0)/samples.length,p10:samples[2000],p90:samples[18000],
    at80PercentUptime:samples.reduce((a,b)=>a+b,0)/samples.length/.8};
});
assert(Math.abs(timing[0].mean-(Math.floor((threshold-1)/3)*actionFrames+[1,4,7][(threshold-1)%3]+25)/60)<1e-8);
assert(timing[1].mean>timing[0].mean&&timing[2].mean>timing[1].mean);
assert(rows.every(r=>r.projectileDamage>=1&&r.damages.every(d=>d.total>0)));
const master=npc.rows.filter(x=>x.mode===2);
assert.equal(master.find(x=>x.type===35).life,11220);
assert.equal(master.find(x=>x.type===668).life,15172);
assert.equal(master.find(x=>x.type===113).life,14280);
const result={date:'2026-10-15',source:npc.source,configSHA256:crypto.createHash('sha256').update(configText).digest('hex'),
  threshold,actionFrames,interval:iceConfig.interval,hat:true,rows,timing,checks:['NPC defaults and hit damage queried from installed game assembly','threshold read from runtime source',
    'exactly one exclusive hat in every equipped loadout','budget interval agrees with action duration',
    'skill level controls damage baseline','integer damage split before defense/crit','master accessory slots',
    'death before threshold flagged','timing uses 45-frame actions and spawn/retreat delays']};
fs.writeFileSync(path.join(__dirname,`results-${threshold}-hat.json`),JSON.stringify(result,null,2)+'\n');
const f=(v,n=1)=>v.toFixed(n);
function range(r,key,n=1){return [...new Set(r.damages.map(d=>f(d[key],n)))].join('～');}
function rowText(r){return `| ${r.name} | ${r.hp.toLocaleString('en-US')} | Lv.${r.skillLevel}／基准Lv.${r.damageLevel} | ${r.projectileDamage} | ${range(r,'total',0)} | ${range(r,'percent')}%${r.damages.some(d=>!d.aliveAtThreshold)?'（通常先击杀，不能视为实际触发）':''} |`;}
const selected=learned=>rows.filter(r=>r.prefix==='configured'&&r.learned===learned);
const report=`# 冰锥首次冻结：${threshold}次命中、${iceConfig.interval}秒、默认带帽

> 本表仅是固定单部位的理论伤害对照。当前多部位共享积蓄，手死亡不会使冻结失败；实战策略结论以 REPORT-shared-54-hat.md 为准。

2026-10-15。基于当前技能JSON、当前C#施法代码和本机tModLoader数据的离线测算；没有自动操作游戏打Boss，也不代表某次实战录像。

## 主要结论

当前配置为${threshold}次有效命中，每轮三发、${actionFrames}帧，伤害预算周期${iceConfig.interval}秒。连续命中时，第${threshold}发最早约 **${f(timing[0].mean,2)}秒＋向前飞行时间** 到达。75%命中时约 **${f(timing[1].mean)}秒＋飞行时间**；再只有80%的时间能输出时约${f(timing[1].at80PercentUptime)}秒。实际动作调度、攻击速度或受击可能改变帧数。

默认带伊蕾娜专属帽子，占用一个机动/生存饰品槽，提供10%魔法增伤、5个百分点暴击、20特殊魔力，与当前 ElainaHat.UpdateAccessory 一致。所有方案都带帽，另列全增伤前缀及冰锥Lv.1对照。

0.5→0.75秒也使每轮伤害预算提高50%。若装备、防御和取整都不变，54×0.75/(84×0.5)=96.43%，所以54次新命中的未扣防总预算仅比84次旧命中低3.57%，并非低35.7%。每发变强还降低了防御相对影响，再加上帽子的增伤/暴击，实际首次冻结前掉血可以高于旧无帽结果。

## 该阶段升满冰锥

按现有阶段配装（以奥秘为主），大师肉前6槽、肉后吃恶魔之心7槽，默认带专属帽子；不加魔力强化、怒气或暴怒等药物。满角色阶段上限并把冰锥升至允许的最高等级。伤害包括触发冻结的第${threshold}发。先死亡的目标仅列理论伤害预算，不能视为实际触发冻结。

| 目标 | 大师HP | 冰锥等级／伤害基准 | 单发面板伤害 | ${threshold}发理论伤害 | 占该血条 |
|---|---:|---|---:|---:|---:|
${selected('cap').map(rowText).join('\n')}

骷髅王头部的范围分别对应普通/旋转防御，不是随机样本置信区间；双手已毁行从头部满血重新计算，不把打手伤害加入头部血条。双子魔眼的两条血量分别列出，不能把两眼合计当成一个冻结目标。

## 冰锥一直保持刚学会的Lv.1

技能伤害按实际技能等级而非角色等级自动上涨；Lv.1固定映射基准Lv.21。这里仍换成各阶段装备，只不升级冰锥。

| 目标 | 大师HP | 冰锥等级／伤害基准 | 单发面板伤害 | ${threshold}发理论伤害 | 占该血条 |
|---|---:|---|---:|---:|---:|
${selected('first').map(rowText).join('\n')}

## 全增伤饰品前缀敏感性

护甲/饰品种类不变，把可用槽位换成实验室自动选择的险恶/幸运组合，技能按阶段升满。

| 目标 | 大师HP | 冰锥等级／伤害基准 | 单发面板伤害 | ${threshold}发理论伤害 | 占该血条 |
|---|---:|---|---:|---:|---:|
${rows.filter(r=>r.prefix==='damage'&&r.learned==='cap').map(rowText).join('\n')}

超过100%表示期望下目标会先死，不能把第${threshold}发当作实际触发冻结。只用冰锥、同一目标固定防御时，未命中只拖长时间，不改变凑够${threshold}次有效命中需要造成的伤害。

## 时间与假设

| 单发命中率 | 连续施法平均秒数 | 10%～90%样本范围 | 80%输出时间占比 |
|---|---:|---:|---:|
${timing.map(t=>`| ${f(t.hitChance*100,0)}% | ${f(t.mean,2)} | ${f(t.p10,2)}～${f(t.p90,2)} | ${f(t.at80PercentUptime,2)} |`).join('\n')}

每档20000个固定种子样本，包含第1/4/7帧生成及25帧后撤，不含向前飞行、目标无敌和15秒以上断档清零。80%输出时间一列为时间占比折算，不模拟连续长停手。纯冰锥至少${Math.ceil(threshold/iceConfig.shots)}次施法、${Math.ceil(threshold/iceConfig.shots)*iceConfig.cost}魔力。

伤害计算顺序：按实际技能等级取基准 → ${iceConfig.ratio}×${iceConfig.interval}秒预算 → 装备增伤（含帽子） → 除${iceConfig.shots}向下取整 → 本机NPC.HitModifiers处理防御与暴击 → 按暴击率求${threshold}发期望。未模拟±15%伤害浮动和幸运值，因此表中为近似均值，尤其低伤害打高防时实际会略有偏差。未计水球/水射线/其他伤害，它们会让首次冻结时Boss损血更多；未模拟冰锥穿透多个部位、重复碰撞或Boss AI全流程。

骷髅王头：存活手每只增加25防，旋转再减10防。血肉墙嘴/眼共享血量但防御18/6；列出全程只命中一个部位的两种情况。独眼巨鹿无敌阶段不计有效命中。

阶段配装：${selected('cap').filter((r,i,a)=>a.findIndex(x=>x.stage===r.stage)===i).map(r=>`阶段${r.stage}／角色上限${r.characterCap}：${r.armor}`).join('；')}。

当前KL的骷髅王头和手共享积蓄，手死亡后可转火继续累计；完整转火结果见REPORT-shared-54-hat.md。这里没有为了拟合数据改动游戏参数。

本机数据DLL SHA256：${npc.source.sha256}。NPC初始化为非特殊种子、单人，探针没有加载其他模组的Boss血量修改。

复跑：node Tools/MageBalance/experiments/ice-unlock-2026-10-15/probe-npcs.cjs（需要本机tModLoader与dotnet）；node Tools/MageBalance/experiments/ice-unlock-2026-10-15/run.cjs。
`;
fs.writeFileSync(path.join(__dirname,`REPORT-${threshold}-hat.md`),report);
console.log(JSON.stringify({timing,rows:selected('cap').map(r=>({name:r.name,hp:r.hp,skillLevel:r.skillLevel,damage:r.projectileDamage,
  lost:r.damages.map(d=>Math.round(d.total)),percent:r.damages.map(d=>Number(f(d.percent)))})),checks:result.checks},null,2));
