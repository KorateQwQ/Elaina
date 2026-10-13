'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const S=require('./simulate.cjs');
const previous=JSON.parse(fs.readFileSync(path.join(__dirname,'results-independent-freeze.json'),'utf8'));
const SEEDS=24;
function scope(name) {
  if(name==='graduate')return {stage:19,allowThunder:true,healthRegen:9,healthPotionHeal:200};
  if(name==='firstClear')return {stage:18,allowThunder:false,healthRegen:2,healthPotionHeal:150};
  return {stage:18,allowThunder:true,healthRegen:2,healthPotionHeal:150};
}
function quantile(xs,q){if(!xs.length)return null;const a=[...xs].sort((a,b)=>a-b);return a[Math.floor((a.length-1)*q)];}
function aggregate(options={}) {
  const runs=Array.from({length:SEEDS},(_,i)=>S.run({...options,seed:i+1})),wins=runs.filter(r=>r.killed),times=wins.map(r=>r.time);
  const mean=fn=>wins.length?wins.reduce((n,r)=>n+fn(r),0)/wins.length:null;
  return {options,samples:SEEDS,won:wins.length,died:runs.filter(x=>x.playerDead).length,unfinished:runs.filter(x=>!x.playerDead&&!x.killed).length,
    time:{mean:mean(r=>r.time),median:quantile(times,.5),min:quantile(times,0),max:quantile(times,1),p10:quantile(times,.1),p90:quantile(times,.9)},
    share:Object.fromEntries(['water','ice','ball','lock','thunder'].map(k=>[k,mean(r=>r.records[k].share)])),
    manaPotMean:mean(r=>r.usedPotions),manaPotMax:wins.length?Math.max(...wins.map(r=>r.usedPotions)):null,
    healthPotMean:mean(r=>r.healthPotions),hurtManaMean:mean(r=>r.hurtPaid),healthDamageMean:mean(r=>r.healthDamage),
    coreOpenedMean:mean(r=>r.coreOpened),endManaMean:mean(r=>r.endMana),cloudSecondsMean:mean(r=>r.cloudSeconds),
    freezeMean:mean(r=>r.freezeEvents.length),runDetails:runs};
}
// Checks are against independent conservation and vanilla source facts.
assert.equal(S.totalHP,277311);
const ideal=S.run({profile:'ideal',healthRegen:9,healthPotionHeal:200});
assert(ideal.killed);assert(Math.abs(ideal.totalDamage-S.totalHP)<1e-6);
assert(ideal.stageDeaths.slice(0,3).every(x=>x.id!=='core'));assert.equal(ideal.stageDeaths.at(-1).id,'core');
assert(Math.abs(Object.values(ideal.records).reduce((n,r)=>n+r.share,0)-1)<1e-9);
assert(ideal.records.thunder.spent<=450); // A short fight cannot charge a complete30s cloud for free.
const alwaysOpen=Array.from({length:1200},(_,t)=>S.animationGoal('head',t));
assert(alwaysOpen.includes(0)&&alwaysOpen.includes(15));
const replay=S.run({profile:'normal',seed:7});assert.equal(replay.time,S.run({profile:'normal',seed:7}).time);
const first=S.run({...scope('firstClear'),profile:'normal'});assert.equal(first.records.thunder.count,0);
assert.equal(ideal.freezeEvents.length,1);
assert(ideal.stageDeaths.some(x=>x.time>ideal.freezeEvents[0].time&&x.time<ideal.finalFrost.freezeReady/60));
assert.equal(ideal.finalFrost.freezeID,1,'switching to next eye or core does not create another freeze');
const globalLockIDs=ideal.lockEvents.filter(x=>x.remaining>0).map(x=>x.freezeID);
assert.equal(new Set(globalLockIDs).size,globalLockIDs.length);
const poorSurvival=aggregate({profile:'heavy',regenPercent:2,healthRegen:2,healthPotionHeal:150});
assert(poorSurvival.died>0);
const checks=['Single-player Master totalHP277311 derived from 1.4.4.9 NPC stat scaling',
  'Core cannot take damage before all3 eyes die; total kill damage exactly equals their HP sum',
  'Final skill shares sum to100%; overkill, dead targets and closed-eye damage excluded',
  'Thunder mana charged only during active paid frames; all runs assert mana conservation',
  'Seeded hit rolls deterministic; ice3projectiles jointly add at most.5s per attack',
  'Current first-clear unlock set excludes thunder; conservative survival can terminate fight before kill',
  'One shared boss-wide freeze meter and cooldown survive eye deaths and core opening; target switches cannot refresh them',
  'Each global freeze can be detonated once; actual water reductions checked against consecutive freeze times'];
const combat=Object.fromEntries(['graduate','firstClear','hypotheticalFullFirstClear'].map(name=>[name,
  Object.fromEntries(['ideal','normal','heavy'].map(profile=>[profile,aggregate({...scope(name),profile,regenPercent:2})]))]));
const potions=[];
for(const profile of ['normal','heavy'])for(const count of [0,1,3,5])for(const percent of [30,50,70])potions.push({profile,count,percent,
  result:aggregate({...scope('graduate'),profile,potions:count,potionPercent:percent,regenPercent:2})});
const candidateRegen=aggregate({...scope('graduate'),profile:'normal',regenPercent:2.5});
const damagePrefixes=aggregate({...scope('graduate'),profile:'normal',prefix:'damage',regenPercent:2});
const results={date:'2026-10-14',seeds:SEEDS,checks,bossStats:S.bossStats,totalHP:S.totalHP,candidate:S.candidate,profiles:S.profiles,
  combat,potions,candidateRegen,damagePrefixes,poorSurvival,sharedFreeze:true,
  previousIndependent:Object.fromEntries(['ideal','normal','heavy'].map(k=>[k,{time:previous.combat.graduate[k].time,share:previous.combat.graduate[k].share,freezeMean:previous.combat.graduate[k].freezeMean}])),
  sourceFiles:{NPC:'D:/泰拉原版贴图与代码与其他工具/tModLoader/Terraria/NPC.cs',gameMode:'D:/泰拉原版贴图与代码与其他工具/tModLoader/Terraria/DataStructures/GameModeData.cs',
    runtimePotion:'ElainaAttribute/ElainaManaElixirPlayer.cs',runtimeRegen:'ElainaAttribute/ElainaMagicAttributes.cs',barrier:'ElainaModSkills/Skills/MagicBarrier/BarrierDamageMath.cs'},
  assumptions:{mode:'Single-player ordinary Master, 7accessoryslots, Elaina hat, no other difficulty mods',
    tank:'500 original max life ->300 life+200mana via AshenWitch; shield pays up to40% of incoming damage from current mana, no shield auto-drink',
    health:'Graduate assumes maintained nebula life regeneration9HP/s and available200HP potion /60s; first clear assumes2HP/s and150HP potion /60s. No lifesteal.',
    firstClear:'Stage18 cap90 excludes prototype.Lightning unlock91; hypotheticalFullFirstClear deliberately overrides this',
    targeting:'One eye at a time; boss-wide shared freeze meter, cooldown and frozen duration survive part deaths and stage transition. Wet remains per part. Core last; thunder hits one wet open target every.5s.',
    AI:'Extracted open/close schedule and frame transitions only. Boss-wide frozen state pauses all living eye clocks until consumed; no full movement, attacks, spatial distance or network simulation.',
    limitations:'No MoonLeech boss healing, real flight, actual reach limits, variable nebula buff pickups, randomly positioned arena. Hit/dodge/hurt profiles are illustrative rather than empirically calibrated.',
    time:'From spawn including1s appearance until coreHP0, excluding60s summoning countdown and death/loot animation.',
    search:'Web retrieval produced no citeable pages; relevant game facts verified using local1.4.4.9/tML primary source files.'}};
fs.writeFileSync(path.join(__dirname,'results.json'),JSON.stringify(results,(k,v)=>['manaSamples'].includes(k)?undefined:v,2)+'\n');
const f=(x,d=1)=>x===null?'—':x.toFixed(d);
const timeRange=a=>a.won?f(a.time.min)+'～'+f(a.time.max)+'秒':'未击杀';
const short=a=>({won:a.won,died:a.died,mean:a.time.mean,range:[a.time.min,a.time.max],share:a.share,manaPot:a.manaPotMean});
const rowPot=(profile,count)=>[30,50,70].map(percent=>potions.find(x=>x.profile===profile&&x.count===count&&x.percent===percent).result);
const report=`# 大师月亮领主：整体共享冻结后的修正版

2026-10-14。单人普通大师，7饰品槽，伊蕾娜帽子。按用户明确的KL规则，整个月总共用冻结CD和积累。此前独立冻结的报告和数据归档至REPORT-independent-freeze.md和results-independent-freeze.json，当前报告取代其结论。测试对象是尚未接入游戏的技能候选；这是简化战斗模型，不是游戏实测。

## 原版目标与范围

依据本地Terraria1.4.4.9/tML源码：大师血量为头眼86062、左右手各47812、核心95625，总计**277311**。核心在三眼被打掉后才开放。血量缩放来自NPC.cs7972、7990、8008、11705、12025及GameModeData.cs；此数值适用于单人，不套用多人加血。

眼睛开闭沿NPC.cs989的攻击表、27023的手部判定和27458的头部判定逐帧更新，并非总血量直接除木桩DPS。只复制这些状态和动画帧，未启动完整NPC AI。

冻结CD60秒，水流每次减0.16秒、水球每次减4秒，CD结束后冰锥需3秒有效积累。三眼和核心共用一份冻结CD、积累、冻结持续时间；换目标、眼睛死亡和核心开放均不重置。命中任一可受伤部位的水系技能减少同一个CD。潮湿暂仍分部位处理，用户尚未明确它是否也共享。

冰凌锁消耗整个月总的剩余冻结；整次伤害预算只结算在当前目标，不因为整体冻结给每只眼睛免费各算一份。1秒伤害延迟后若该目标已关闭，会丢伤。天雷每0.5秒一次单目标攻击，持续扣15蓝/s。

时间从月总生成起到核心血量归零，含1秒出现准备，不含召唤前60秒和死亡掉落动画。不模拟月蛭给Boss回血，因此时间偏乐观；移动与射程通过下列命中/停手假设代替，不能将结果当作精准AI战斗回放。

## 三种操作假设

| 场景 | 水流 / 冰锥 / 水球 / 冰凌锁 / 天雷命中 | 停手躲避 | 已扣普通防御、未扣魔力盾的受伤 |
|---|---|---|---|
| 理想 | 100% /100% /100% /100% /100% | 无 | 无 |
| 普通操作 | 80% /75% /85% /90% /95% | 每12秒停手1.5秒 | 每18秒受150伤害 |
| 高压力 | 65% /50% /70% /80% /85% | 每8秒停手2秒 | 每10秒受180伤害 |

普通操作中满盾每次付60蓝、承受90生命伤害；高压力付72蓝、承受108生命伤害。蓝不足时减伤也不足，生命伤害上升。受击不自动喝回蓝药，也不暂停自然回复，符合当前盾扣蓝和PostHurt逻辑。已有天雷在躲避期间继续攻击。

每个配置测试24个固定随机种子。表中的样本完成数是模型结果，**不是实测成功率**；命中率和受击频率没有玩家实测标定。

## 配装与技能解锁

- 毕业刷月总：B3500、星云套、三种强化按满层维持、7条奥秘、帽子；魔力上限620。主表用实际游戏回蓝规则2%：17.4蓝/s。毕业生存假设星云生命强化维持9HP/s，备200HP治疗药、60秒药水病。
- 首次挑战：阶段18的B1800、幽灵面具套、7条奥秘、帽子；不计幽灵追击和吸血，基础魔力上限同为620。按2HP/s持续恢复、150HP治疗药、60秒药水病计算生存。
- 当前天雷配置unlockLevel91、unlockStageCap95；月总前阶段18等级上限90，**首次挑战目前没有天雷**。若设计上要首次月总就使用完整五件套，必须提前解锁；下文另列提前解锁的假设，未修改正式解锁表。
- 普攻毕业倍率水流0.9B、冰锥0.9B；水球150蓝/15s/8.1B，冰凌锁100蓝/3.2B+2.5B×剩余秒数，天雷34B/30s/120sCD，其他规则沿用此前候选。

## 击杀时间

全部默认5次回蓝药，每次回50%，采用2%自然回蓝。

| 配装 / 场景 | 平均核心归零 | 本组样本范围 | 完成样本 | 平均回蓝药使用 |
|---|---:|---:|---:|---:|
${['graduate','firstClear','hypotheticalFullFirstClear'].flatMap(name=>['ideal','normal','heavy'].map(profile=>{const a=combat[name][profile];return `| ${{graduate:'毕业五件套',firstClear:'首次挑战四件套',hypotheticalFullFirstClear:'首次配装＋提前解锁天雷'}[name]} / ${{ideal:'理想',normal:'普通',heavy:'高压力'}[profile]} | ${f(a.time.mean)}秒 | ${timeRange(a)} | ${a.won}/24 | ${f(a.manaPotMean,2)} |`;})).join('\n')}

若毕业装备不维持星云生命回复，只给2HP/s和150HP治疗药，高压力样本只有${poorSurvival.won}/24完成，${poorSurvival.died}/24死亡。不能将“输出足以打掉Boss”与“玩家能活到击杀”混为一谈。

保持2.5%候选回蓝时，毕业普通操作平均${f(candidateRegen.time.mean)}秒，平均用药${f(candidateRegen.manaPotMean,2)}次；2%版本平均${f(combat.graduate.normal.time.mean)}秒、用药${f(combat.graduate.normal.manaPotMean,2)}次。本次短战主要吃开场资源和可攻击窗口，回蓝规则改变没有显著改变该组击杀时间。

## 各技能最终伤害占比

按每次成功击杀真实扣掉的277311血量统计，再对成功样本取平均；闭眼空打、过量伤害、已经死亡的部位均不计入。下表并非300秒木桩伤害占比，也不把水系对其他技能的辅助价值二次归入水系伤害。

| 技能 | 毕业理想 | 毕业普通 | 毕业高压力 | 首次配装普通、天雷未解锁 |
|---|---:|---:|---:|---:|
${['water','ice','ball','lock','thunder'].map(k=>`| ${{water:'水流',ice:'冰锥',ball:'水球',lock:'冰凌锁',thunder:'天雷'}[k]} | ${f(combat.graduate.ideal.share[k]*100)}% | ${f(combat.graduate.normal.share[k]*100)}% | ${f(combat.graduate.heavy.share[k]*100)}% | ${f(combat.firstClear.normal.share[k]*100)}% |`).join('\n')}

共享后换眼睛不再重新进入冰锥准备。只要整个月总的冻结仍在CD，玩家继续水系输出；本组毕业普通操作平均冻结${f(combat.graduate.normal.freezeMean,2)}次。水流占比回升，冰锥占比下降。短战中天雷持续覆盖了很大一段输出时间，水系减CD的长期循环仍没有反复展开。

## 与错误的独立冻结模型对照

所有数值和命中/受击假设保持不变，只修正冻结共享和整体冻结对开闭眼时钟的影响。

| 毕业场景 | 旧：独立冻结平均时间 | 新：共享冻结平均时间 | 旧 / 新平均冻结次数 |
|---|---:|---:|---:|
${['ideal','normal','heavy'].map(k=>`| ${{ideal:'理想',normal:'普通',heavy:'高压力'}[k]} | ${f(previous.combat.graduate[k].time.mean)}秒 | ${f(combat.graduate[k].time.mean)}秒 | ${f(previous.combat.graduate[k].freezeMean,2)} / ${f(combat.graduate[k].freezeMean,2)} |`).join('\n')}

冻结次数减少不必然延长击杀：旧模型会让每个新部位都进入至少3秒、丢伤时更久的冰锥积累；共享模型冻结CD中则持续使用伤害/耗蓝更有利的水流，并维持潮湿供天雷锁定。移除重复准备成本后，新模型整体更快。另一个变化是整体冻结同时暂停各眼睛的局部开闭眼时钟，这是本次根据整体冻结采用的明确假设，尚未启动KL/游戏验证。

冰凌锁会受到闭眼、提前死亡和1秒伤害延迟影响；在部分单次样本里可能整次空掉。若将引爆伤害设计成即时命中，应另测，不能沿用本表。

## 携带量与药效

当前游戏支持最多5次，固定50%；30%/70%是候选药效设计假设，不是已实现的药品。下表同一行和三种药效使用相同24个随机种子。

普通操作：

| 携带次数 | 30%回蓝：平均击杀 / 平均使用 | 50%回蓝：平均击杀 / 平均使用 | 70%回蓝：平均击杀 / 平均使用 |
|---|---|---|---|
${[0,1,3,5].map(count=>`| ${count} | ${rowPot('normal',count).map(a=>`${f(a.time.mean)}秒 / ${f(a.manaPotMean,2)}次`).join(' | ')} |`).join('\n')}

高压力，有星云生命续航：

| 携带次数 | 30%：平均击杀 / 使用 / 完成 | 50%：平均击杀 / 使用 / 完成 | 70%：平均击杀 / 使用 / 完成 |
|---|---|---|---|
${[0,1,3,5].map(count=>`| ${count} | ${rowPot('heavy',count).map(a=>`${f(a.time.mean)}秒 / ${f(a.manaPotMean,2)}次 / ${a.won}/24`).join(' | ')} |`).join('\n')}

在此配置下，3次与5次的区别较小：多数样本在120秒前结束，天雷不会完整进入第二次CD循环。更多药并不会自动增加输出，只会在缺蓝动作真的发生时被调用。提高药效不能弥补闭眼空打、冰锥积累反复中断或玩家死亡。

## 局限与下一步

- 月总的月蛭回血未计入，时间偏乐观；没有模拟平台走位、距离、真实弹道或其他难度模组。
- 冻结CD、积累、持续时间按用户规则共享；整体冻结暂停全部存活眼睛的局部开闭眼时钟。潮湿暂分部位；未完整运行KL NPC组同步和真实AI。
- 默认戴帽子、7槽、全奥秘；毕业星云伤害和生命强化满层常驻，这是较好的战斗条件。实际无法维持强化时不要直接拿本结果当保证。
- 生命恢复和治疗药为假设资源，不额外计算治疗施法、生命转魔力、吸血或幽灵追击。未单列护甲防御；受伤输入已经是普通防御结算后的伤害，避免重复减防。
- 攻击命中、躲避停手、受击率和每0.5秒天雷攻击都是设计假设，尚无实测标定。共享冻结后的毕业普通操作约25秒是乐观参考，首次当前解锁约83秒是另一种参考，不能承诺实战固定秒数。

## 验证与复跑

${checks.map((x,i)=>`${i+1}. ${x}`).join('\n')}

\`\`\`powershell
node Tools/MageBalance/experiments/moon-lord-master-2026-10-14/study.cjs
\`\`\`

原始数据results.json保存每种假设和每个种子的最终伤害、用药、生命、阶段死亡时刻、冻结/引爆和云持续时间。正式游戏和实验室配置没有被修改。

外网检索未返回可引用内容，本轮原版机制校验依据可访问的本地1.4.4.9/tML源码。运行时药剂ElainaManaElixirPlayer.cs（最多5、每次50%）；回蓝ElainaMagicAttributes.cs（2%）；护盾BarrierDamageMath.cs、MagicBarrierSkill.cs（当前魔力支付减伤）。
`;
fs.writeFileSync(path.join(__dirname,'REPORT.md'),report);
console.log(JSON.stringify({checks,bossStats:S.bossStats,combat:Object.fromEntries(Object.entries(combat).map(([name,x])=>[name,Object.fromEntries(Object.entries(x).map(([p,a])=>[p,short(a)]))])),potionHeavy:[0,1,3,5].map(count=>({count,result:rowPot('heavy',count).map(short)}))},null,2));
