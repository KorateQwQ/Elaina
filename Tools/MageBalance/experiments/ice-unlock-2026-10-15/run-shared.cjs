'use strict';
// Current multi-NPC freeze rules: effective damage belongs to each part; buildup belongs to the boss.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const {B}=require('../ice-build-2026-10-14/simulate.cjs');
const root=path.resolve(__dirname,'../../../..');
const text=fs.readFileSync(path.join(root,'ElainaModSkills/Data/SkillBalance.json'),'utf8');
const config=JSON.parse(text).configuration;
const npc=JSON.parse(fs.readFileSync(path.join(__dirname,'boss-stats.json'),'utf8'));
assert.equal(crypto.createHash('sha256').update(fs.readFileSync(npc.source.dll)).digest('hex'),npc.source.sha256);
const freezeSource=fs.readFileSync(path.resolve(root,'../KL/DamageSystem/ElementBuildUpSystem.cs'),'utf8');
const threshold=Number(freezeSource.match(/RegisterHitCount\([\s\S]*?\b240,\s*(\d+)f/)[1]);
assert.equal(threshold,54);
const anchorSource=fs.readFileSync(path.resolve(root,'../KL/Utils/NPCExtentions.cs'),'utf8');
assert(anchorSource.includes('npc.type == NPCID.SkeletronHand'));
const ice=config.basicAttacks.find(x=>x.id==='IceConeSkill');
assert.equal(ice.interval,.75);assert.equal(ice.shots,3);
function gear(stage,prefix='configured',learned='cap') {
 const s=structuredClone(config);B.applyLoadout(s,stage);s.slots=stage>=8?7:6;
 if(!s.e.items.slice(0,s.slots).some(x=>x.id==='elainaHat')) {
  const slot=s.e.items.slice(0,s.slots).findIndex(x=>x.id==='utility'||x.id==='empty');assert(slot>=0);s.e.items[slot].id='elainaHat';
 }
 if(prefix==='damage')B.applyDamagePrefixes(s,'e');
 const g=B.gear(s,'e'),nodes=B.progressionNodes(ice,s.progression.phaseSize,stage*5);
 const level=learned==='first'?nodes[0]:nodes.at(-1),budget=B.progressionBudget(s,s.basicAttacks.find(x=>x.id===ice.id),level);
 return {stage,prefix,learned,hat:true,skillLevel:learned==='first'?1:nodes.length,damageLevel:level,
  damage:Math.max(1,Math.floor(Math.fround(Math.fround(budget.total)*Math.fround(1+g.damage/100))/3)),
  crit:g.crit/100,armor:B.armors[s.e.armor].name,items:s.e.items.slice(0,s.slots)};
}
const hp=type=>npc.rows.find(r=>r.mode===2&&r.type===type).life;
const headHP=hp(35),handHP=hp(36),groupHP=headHP+handHP*2;
const policies={head:'全程打头',hands:'先左手，打掉后转右手',handHead:'先左手，打掉后转头',alternate:'双手交替命中'};
const N=50000;
function simulate(g,policy,spin) {
 let seed=0x15b055,firstDeaths=0,allDeaths=0,overkill=0;
 const random=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};
 const sums={head:0,left:0,right:0,firstHandKillHit:0},totals=[];
 for(let trial=0;trial<N;trial++) {
  const life={head:headHP,left:handHP,right:handHP};let firstKill=0;
  for(let hit=1;hit<=threshold;hit++) {
   let target=policy==='head'?'head':policy==='handHead'?(life.left>0?'left':'head'):
    policy==='alternate'?(hit%2?'left':'right'):(life.left>0?'left':life.right>0?'right':'head');
   if(life[target]<=0)target=life.left>0?'left':life.right>0?'right':'head';
   const defense=target==='head'?10+25*Number(life.left>0)+25*Number(life.right>0)-(spin?10:0):14;
   const damage=npc.damage[defense][g.damage][random()<g.crit?1:0];
   const actual=Math.min(life[target],damage);overkill+=damage-actual;life[target]-=actual;
   if(target!=='head'&&life[target]===0&&!firstKill)firstKill=hit;
   // Every hit, including a lethal limb hit, credits the still-living head. Never reset on target switch.
   assert(life.head>0,'The head must survive through the first freeze in these fixtures');
  }
  for(const key of ['head','left','right'])sums[key]+=(key==='head'?headHP:handHP)-life[key];
  if(firstKill){firstDeaths++;sums.firstHandKillHit+=firstKill;}
  if(life.left===0&&life.right===0)allDeaths++;
  totals.push(groupHP-life.head-life.left-life.right);
 }
 const mean={head:sums.head/N,left:sums.left/N,right:sums.right/N};
 const total=mean.head+mean.left+mean.right;totals.sort((a,b)=>a-b);
 return {gear:g,policy,spin,samples:N,successfulHits:threshold,freezeTriggeredRate:1,
  lost:mean,totalLost:total,groupLostPercent:total/groupHP*100,headLostPercent:mean.head/headHP*100,
  anyHandDeadRate:firstDeaths/N,bothHandsDeadRate:allDeaths/N,firstHandKillHit:firstDeaths?sums.firstHandKillHit/firstDeaths:null,
  overkill:overkill/N,totalP10:totals[Math.floor(.1*N)],totalP90:totals[Math.floor(.9*N)]};
}
const rows=[];
for(const prefix of ['configured','damage'])for(const learned of ['cap','first'])for(const policy of Object.keys(policies))
 for(const spin of [false,true])rows.push(simulate(gear(5,prefix,learned),policy,spin));
const others=[['独眼巨鹿',668,6,10],['血肉墙（嘴）',113,7,18],['血肉墙（眼）',114,7,6],
 ['史莱姆皇后',657,8,26],['双子激光眼',125,9,10],['双子魔焰眼',126,9,10]].map(([name,type,stage,defense])=>{
  const g=gear(stage),[normal,crit]=npc.damage[defense][g.damage];
  const lost=threshold*(normal*(1-g.crit)+crit*g.crit);
  return {name,hp:hp(type),gear:g,lost,percent:lost/hp(type)*100};
 });
assert(rows.every(r=>r.successfulHits===54&&r.totalLost<=groupHP));
assert(rows.filter(r=>r.policy==='head').every(r=>r.lost.left===0&&r.lost.right===0));
assert(rows.filter(r=>r.policy==='hands'||r.policy==='alternate').every(r=>r.lost.head===0));
const same=(r,prefix,learned,policy)=>r.gear.prefix===prefix&&r.gear.learned===learned&&r.policy===policy;
const main=policy=>rows.filter(r=>same(r,'configured','cap',policy));
const f=(n,d=1)=>n.toFixed(d),range=(rs,get,d=0)=>[...new Set(rs.map(r=>f(get(r),d)))].join('～');
const table=(prefix,learned)=>Object.keys(policies).map(policy=>{
 const rs=rows.filter(r=>same(r,prefix,learned,policy));
 return `| ${policies[policy]} | ${range(rs,r=>r.lost.head)}（${range(rs,r=>r.headLostPercent,1)}%） | ${range(rs,r=>r.lost.left+r.lost.right)} | ${range(rs,r=>r.totalLost)}（${range(rs,r=>r.groupLostPercent,1)}%） | ${range(rs,r=>r.anyHandDeadRate*100,1)}% |`;
}).join('\n');
const result={date:'2026-10-15',threshold,interval:ice.interval,source:npc.source,
 configSHA256:crypto.createHash('sha256').update(text).digest('hex'),headHP,handHP,groupHP,rows,others,
 assumptions:['Single-player Master, no special seed, exclusive hat in every loadout',
  'One successful NPC collision per projectile; no incidental piercing through multiple parts',
  'Seeded critical hits; no damage variation or luck; no whole AI/aim/path simulation',
  'Instant retargeting after limb death; surviving boss retains accumulated hits',
  'Head defense follows surviving hands, idle/spinning evaluated as separate bounds; no full rotation schedule',
  'Only ice damage, no other spells, no 15-second reset gaps'],
 checks:['54 total successful hits across parts trigger freeze','lethal limb hit contributes to live head',
  'limb damage capped at remaining HP, overkill excluded','head HP separated from optional limb HP',
  'switching targets does not reset buildup','every scenario wears hat']};
fs.writeFileSync(path.join(__dirname,'results-shared-54-hat.json'),JSON.stringify(result,null,2)+'\n');
const report=`# 冰锥首次冻结修订报告：多部位共享积蓄

2026-10-15。单人大师、非特殊种子、专属帽子默认佩戴、0.75秒周期、54次有效命中。本报告替代旧报告对“骷髅王单只手先死便无法冻结”的结论。

## 修订结论

命中头和任一只手都累加到骷髅王头部的同一条进度。手被打掉不清空积蓄，剩余命中可打另一只手或头，合计54次就冻结头和所有存活部位。54次指实际命中事件，不要求54发都命中同一部位。

因此必须分别报告“头部掉血”和“双手掉血”。打手不会直接扣头部HP；把手伤害称作主体掉血，或者把手的死亡当作整轮冻结失败，都不适用于当前规则。

## 骷髅王：阶段升满、现有奥秘为主配装

蜂王后阶段，陨石套＋专属帽子，冰锥Lv.3（基准Lv.25），单发面板${gear(5).damage}，暴击率${f(gear(5).crit*100,0)}%。头部${headHP}HP、每手${handHP}HP。用于描述整组损血的分母为${groupHP}=头＋双手；它不是共享血池，也不是必须打掉的总生命，击杀头即可结束战斗。

| 攻击策略 | 头部掉血／头部比例 | 双手合计掉血 | 整组实际掉血／整组比例 | 至少一手已毁的样本比例 |
|---|---:|---:|---:|---:|
${table('configured','cap')}

每个策略/防御情形50000个固定种子样本。范围是普通/旋转防御情形，不是置信区间。暴击逐次抽样；目标死亡后下一次有效命中立刻切换，溢出伤害不计入掉血。所有样本在头存活时累计到54次，均可触发冻结。

先集中打一手，平均约第${f(main('hands')[0].firstHandKillHit)}次命中打掉；余下约${f(54-main('hands')[0].firstHandKillHit)}次可转打另一只手或头。切换不会重置。手临死时的溢出伤害不会转移给头。

## 刚学会Lv.1以及全增伤前缀

刚学会Lv.1，同阶段装备、仍带帽，单发面板${gear(5,'configured','first').damage}：

| 攻击策略 | 头部掉血／头部比例 | 双手合计掉血 | 整组实际掉血／整组比例 | 至少一手已毁的样本比例 |
|---|---:|---:|---:|---:|
${table('configured','first')}

阶段升满、全部饰品使用实验室选择的增伤前缀，仍带帽：

| 攻击策略 | 头部掉血／头部比例 | 双手合计掉血 | 整组实际掉血／整组比例 | 至少一手已毁的样本比例 |
|---|---:|---:|---:|---:|
${table('damage','cap')}

## 其他早期Boss对照

这些目标的归属规则没有因本次骷髅王等映射补全而改变。仍为阶段升满、现有奥秘为主配装、带帽，仅冰锥伤害：

| 目标 | 血量 | 首次冻结前预计掉血 | 比例 |
|---|---:|---:|---:|
${others.map(r=>`| ${r.name} | ${r.hp} | ${f(r.lost,0)} | ${f(r.percent)}% |`).join('\n')}

血肉墙嘴/眼共用血量；双子魔眼仍是两个独立目标。机械骷髅王、石巨人现在也采用主体共享积蓄，但本次没有用骷髅王数值代替它们各自的伤害测算。

## 时间和模型边界

按三发一轮、45帧动作，连续全部命中合计54次约13.3秒＋向前飞行时间；75%单发命中率约18秒＋飞行时间；仅80%时间能输出时约22.5秒＋飞行时间。共享积蓄消除了切部位造成的重新积累，但手死亡后弹道落空、重新瞄准和15秒断档仍会延迟。

冰锥可以穿透，且免疫按NPC部位独立计时；同一发若穿过多个部位，可能贡献多个命中事件。本表按每发只命中一个部位计算，不把18轮当作所有站位下的硬性下限。

这是基于本机NPC数据及当前代码的离线模型，未进行游戏内全AI实战；包含防御、随机暴击、手部死亡、目标切换及过量伤害截断，不含±15%伤害浮动、幸运、其他技能输出和意外多部位穿透。读取了当前配置和冻结门槛；没有修改游戏参数。

检查：${result.checks.join('；')}。运行时路由测试：KL/Tools/test-boss-freeze.cjs，覆盖主体累计、整组冻结、死亡部位、CD共享、错误主体和同类Boss隔离。

复跑：node Tools/MageBalance/experiments/ice-unlock-2026-10-15/run-shared.cjs。
`;
fs.writeFileSync(path.join(__dirname,'REPORT-shared-54-hat.md'),report);
console.log(JSON.stringify({main:Object.keys(policies).map(policy=>({policy,results:main(policy).map(r=>({spin:r.spin,head:r.lost.head,hands:r.lost.left+r.lost.right,total:r.totalLost,groupPercent:r.groupLostPercent,firstKill:r.firstHandKillHit}))})),others:others.map(({gear,...r})=>r),checks:result.checks},null,2));
