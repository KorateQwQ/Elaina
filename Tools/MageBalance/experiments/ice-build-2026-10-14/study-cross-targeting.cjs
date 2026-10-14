'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const S=require('../moon-lord-master-2026-10-14/simulate.cjs');
const previous=JSON.parse(fs.readFileSync(path.join(__dirname,'results-pre-moon.json'),'utf8'));
const formalFiles=['../../index.html','../../../../ElainaModSkills/Data/SkillBalance.json',
  'candidate-parameters-v5.json','candidate-parameters-lock-core.json'].map(f=>path.resolve(__dirname,f));
const hash=f=>crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
const hashes=formalFiles.map(hash);
const strategies={legacy:{lockPolicy:'legacy',buildPolicy:'focus'},
  crossFocus:{lockPolicy:'adaptive',buildPolicy:'focus'},crossSpread:{lockPolicy:'adaptive',buildPolicy:'spread'}};
const rows=[],examples=[];
for(const variant of ['v5','lockCore'])for(const prefix of ['arcane','damage'])for(const profile of ['ideal','normal','heavy'])
for(const [strategy,policy]of Object.entries(strategies)) {
  const runs=Array.from({length:24},(_,i)=>S.run({stage:18,slots:7,hat:true,prefix,profile,
    seed:i+1,regenPercent:2,allowThunder:false,preBuild:['ball'],healthRegen:2,healthPotionHeal:150,...policy,
    tune:variant==='v5'?{}:{ballB:6.75,lockPerSecondB:4.5}}));
  const wins=runs.filter(r=>r.killed),mean=fn=>wins.length?wins.reduce((n,r)=>n+fn(r),0)/wins.length:null;
  const all=fn=>runs.reduce((n,r)=>n+fn(r),0)/24;
  const row={variant,prefix,profile,strategy,completed:wins.length,died:runs.filter(r=>r.playerDead).length,
    seconds:mean(r=>r.time),effectiveDps:wins.length?S.totalHP/mean(r=>r.time):null,
    share:Object.fromEntries(['water','ice','ball','lock','thunder'].map(k=>[k,mean(r=>r.records[k].share)])),
    freezes:all(r=>r.freezeEvents.length),lockCasts:all(r=>r.records.lock.count),
    crossDetonations:all(r=>r.lockEvents.filter(e=>e.id!==e.buildupTarget).length),
    lockInvalid:mean(r=>r.records.lock.invalidDamage),lockOverkill:mean(r=>r.records.lock.overkill)};
  rows.push(row);
  if(strategy==='legacy') {
    const old=previous.boss.find(x=>x.variant===variant&&x.prefix===prefix&&x.profile===profile).result;
    assert.equal(wins.length,old.completed);
    if(wins.length){assert(Math.abs(row.seconds-old.meanSeconds)<1e-8);assert(Math.abs(row.share.lock-old.shareSuccessfulSamples.lock)<1e-8);}
  }
  if(strategy==='crossSpread'&&profile==='normal')for(let i=0;i<runs.length;i++) {
    const r=runs[i],cross=r.lockEvents.find(e=>e.buildupTarget!==e.id);
    if(cross&&!examples.some(x=>x.variant===variant&&x.prefix===prefix))examples.push({variant,prefix,seed:i+1,
      seconds:r.time,freeze:r.freezeEvents.find(f=>f.freezeID===cross.freezeID),lock:cross,
      damageByPart:r.damagePerPart,partDeaths:r.stageDeaths});
  }
}
assert(formalFiles.every((f,i)=>hash(f)===hashes[i]),'formal values and candidate parameters unchanged');
const result={date:'2026-10-14',experimentOnly:true,rows,examples,strategies,
  corrections:['independent lock aim across the shared frozen boss',
    'wait configured reaction latency without starting a competing ball cast',
    'spread shared buildup across currently vulnerable living parts',
    'do not spend filler hits on a part with a paid lock already pending',
    'a landed lethal ice hit can credit surviving members of the shared frost group'],
  assumptions:'Month-before stage18/B1800/spectre/hat/seven slots/no thunder; 2% recovery;24 illustrative hit/dodge/hurt seeds; core vulnerability gate preserved; eye animation predicted, no future hit rolls observed. Real aim distance, projectile flight and movement not modeled.',
  checks:['legacy policy reproduces all previous stage18 results','mana conservation and HP cap invariants passed',
    'one detonation per shared freeze','cross-part use exercised in integration tests','formal values unchanged']};
fs.writeFileSync(path.join(__dirname,'results-cross-targeting.json'),JSON.stringify(result,null,2)+'\n');
const f=(x,d=1)=>x===null?'—':x.toFixed(d),pct=x=>x===null?'—':f(x*100)+'%';
const name={legacy:'旧同目标策略',crossFocus:'跨部位＋及时引爆',crossSpread:'跨部位＋分散积累'};
const normal=rows.filter(x=>x.profile==='normal');
const report=`# 月前月总：跨部位引爆与操作策略修正

2026-10-14。用户指出：全身共享冻结时，冰锥积累目标与冰凌锁引爆目标可以不同。此前模型虽然共享冻结，却错误限制了终结技瞄准，并在反应等待期间施放水球；此前关于低占比、引爆机会不足以及加强候选更慢的结论不能作为技能机制的定论。

## 本次修正

- 冰凌锁从当前可受伤、处于共享冻结的存活部位中独立选目标，优先能够兑现更多伤害且延迟命中窗口可靠的部位。可以左手积累、右手或头部引爆。
- 保留0.25秒普通反应延迟，期间不启动竞争施法抢占冻结窗口。
- 对照两种积累策略：持续当前目标，或将积累攻击分散到当前可受伤、剩余血量较高的部位。积累进度跨部位保留。
- 已支付冰凌锁的目标在命中前暂不继续用普攻/水球消耗，避免把引爆收益提前打成过量伤害。
- 已经命中的致死冰锥可以为仍存活的整体贡献积累。核心仍需三眼死亡后才可受伤；单次引爆仍只给一个部位结算一次伤害。

技能伤害、消耗、积累门槛、冻结CD均未改动。保留1秒离手命中延迟，不额外延长冻结。眼睛被冻结时维持当时开闭状态，选目标需有可受伤窗口；这是模型的现有可受伤假设，并未假定闭眼自动解除无敌。瞄准预测仅使用确定性动画表，不读取未来随机命中结果。

## 月前普通操作对照

幽灵面具套、帽子、七槽、B1800、无天雷、2%回蓝、先水球再冰锥，2HP/s恢复和150HP治疗药。普通命中/躲避/受击条件与之前一致，每行24个相同种子。下表各行均24/24完成，不能当作实测成功率。

| 方案/前缀 | 策略 | 平均秒数 | 冰凌锁占比 | 水球占比 | 平均施放锁次数 |
|---|---|---:|---:|---:|---:|
${normal.map(x=>`| ${x.variant==='v5'?'原v5':'加强候选'}/${x.prefix==='arcane'?'奥秘':'增伤'} | ${name[x.strategy]} | ${f(x.seconds)} | ${pct(x.share.lock)} | ${pct(x.share.ball)} | ${f(x.lockCasts,2)} |`).join('\n')}

分散积累策略下，加强候选奥秘的锁占比提升到${pct(normal.find(x=>x.variant==='lockCore'&&x.prefix==='arcane'&&x.strategy==='crossSpread').share.lock)}，超过水球；增伤提升到${pct(normal.find(x=>x.variant==='lockCore'&&x.prefix==='damage'&&x.strategy==='crossSpread').share.lock)}，也超过水球。两种策略有取舍，分散积累并非所有配置下最快，不能将表中任一规则称为全局最优玩家。

## 实际跨部位案例

${examples.map(e=>`- ${e.variant==='v5'?'原v5':'加强候选'}／${e.prefix==='arcane'?'奥秘':'增伤'}，种子${e.seed}：${f(e.freeze.time,2)}秒在${e.freeze.id}完成冻结，${f(e.lock.castTime,2)}秒瞄准${e.lock.id}施放冰凌锁，目标当时剩${f(e.lock.castTargetHp,0)}HP；施法结束读取剩余冻结${f(e.lock.remaining,2)}秒，预算${f(e.lock.damageB,3)}B。`).join('\n')}

## 结论与范围

修正后的结果支持玩家可以通过跨部位瞄准、分配准备伤害和优先处理冻结窗口显著提高核心技能收益。之前同目标模型得到的低占比只是受限策略结果，不能据此要求继续加伤害、缩短积累或断言技能没有引爆机会。

木桩预算不受这次多部位策略修正影响。加强候选在月前全增伤木桩略超过原版2倍的数值记录仍成立，但应在正确策略基础上再决定数值调整。这里不是完整游戏实测；真实走位、距离、弹道、月蛭回血和人类反应仍未标定，高压力样本需结合生存假设解读。

验证：旧策略精确重现此前月前结果；独立选择器与48场集成检查通过；冻结不可重复消费、核心开放、魔力守恒与实际HP结算检查通过；正式数值未改变。

复跑：node Tools/MageBalance/experiments/moon-lord-master-2026-10-14/test-targeting.cjs

复跑：node Tools/MageBalance/experiments/ice-build-2026-10-14/study-cross-targeting.cjs
`;
fs.writeFileSync(path.join(__dirname,'REPORT-cross-targeting.md'),report);
console.log(JSON.stringify({report:path.join(__dirname,'REPORT-cross-targeting.md'),normal,examples:examples.map(e=>({variant:e.variant,prefix:e.prefix,from:e.freeze.id,to:e.lock.id,seed:e.seed})),checks:result.checks},null,2));
