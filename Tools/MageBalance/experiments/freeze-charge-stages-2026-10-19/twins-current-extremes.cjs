'use strict';
// Current saved stage10, four skills and deliberate degenerate strategies. Gameplay is read-only.
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict'),crypto=require('node:crypto');
const source=fs.readFileSync(path.join(__dirname,'prefix-comparison.cjs'),'utf8').split(/\r?\nconst scenarios = \{/)[0]
  +'\nglobalThis.adapter={code,rules,sourceSHA256};';
const outer={require,__dirname,console};vm.runInNewContext(source,outer);
let {code,rules,sourceSHA256}=outer.adapter;
function replace(a,b){assert(code.includes(a),a);code=code.replace(a,b);}
replace('const maxMana = B.calculate(s).maxMana;', `const maxMana = B.calculate(s).maxMana;
  const maxLife = s.ashenWitch ? s.maxLife - B.ashenWitchMana(s.maxLife) : s.maxLife;`);
replace('return { prefixAllocation, stage, variant, level,', 'return { maxLife, prefixAllocation, stage, variant, level,');
replace('firstPotion = null, seed = 20261019;', 'firstPotion = null, seed = options.seed ?? 20261019;');
replace('let mana = maxMana, minMana = mana, potions = options.potions ?? 5;', `
  let mana = maxMana, minMana = mana, potions = options.potions ?? 0;
  let life = build.maxLife, lifeSpent = 0, lifeConversions = 0, lifeMana = 0, firstConversion = null;
  let ballsThisCycle = 0, cycleThaw = -1;
  const cycleCounts = [], cycleEndMana = [];`);
replace('function canPay(cost) { return mana + potions * maxMana * .5 + 1e-6 >= cost; }', `
  function conversion(cost) {
    const deficit = Math.max(0, cost - mana);
    const steps = Math.ceil(deficit / (maxMana * .01));
    return { lifeCost: steps * Math.ceil(build.maxLife * .01), gain: steps * maxMana * .01 };
  }
  function canPay(cost) {
    if (cost > maxMana) return false;
    return mana + 1e-6 >= cost || (options.lifeConversion && conversion(cost).lifeCost < life);
  }`);
replace('if (!canPay(cost)) return false;', `if (!canPay(cost)) return false;
    if (mana + 1e-6 < cost && options.lifeConversion) {
      const c=conversion(cost); life-=c.lifeCost;lifeSpent+=c.lifeCost;lifeConversions++;lifeMana+=c.gain;
      ledger.potion+=c.gain;mana+=c.gain;firstConversion??=t/60;
    }`);
// Current Moon Dew implementation grants1% each60ticks, rather than smoothing it.
const regenLine='const regen = (recovery + (t < (options.buffSeconds ?? 0) * 60 ? maxMana * .01 : 0)) / 60; ledger.regen += regen;';
replace(regenLine,`const regen = recovery / 60 + (t < (options.buffSeconds ?? 0) * 60 && (t + 1) % 60 === 0 ? maxMana * .01 : 0); ledger.regen += regen;`);
replace('const wantBall = options.noBall !== true', 'const wantBall = options.noBall !== true');
replace('const wantIce = !fundingLock &&', 'const wantIce = options.noIce !== true && !fundingLock &&');
replace('&& ballAffordableWithReserve', `&& ballAffordableWithReserve
      && (options.ballsPerCycle == null || (casts.lock > 0 && ballsThisCycle < options.ballsPerCycle && t + 70 >= cycleThaw))`);
replace('const prioritizeWater = options.waterUntilLockReady && lockCharge > 0 && lockCD > 0;',
  'const prioritizeWater = options.waterUntilLockReady && lockCharge > 0;');
replace("lockCharge = 0; lockCD = 3600; recoverySinceCast = 0;", `
      if (casts.lock > 0) {cycleCounts.push(ballsThisCycle);cycleEndMana.push(mana);}
      ballsThisCycle=0;cycleThaw=frozenUntil;
      lockCharge = 0; lockCD = 3600; recoverySinceCast = 0;`);
replace('casts.ball++; ballTimes.push(t / 60);', 'casts.ball++;ballsThisCycle++; ballTimes.push(t / 60);');
replace("} else {\n      if (!channel) { channel = true; nextPulse = t; }", `} else {
      if (options.noWater) {ledger.idleTicks++; continue;}
      if (!channel) { channel = true; nextPulse = t; }`);
replace('return { build, options, duration, dps:', 'return { life, lifeSpent, lifeConversions, lifeMana, firstConversion, cycleCounts, cycleEndMana, build, options, duration, dps:');
const ctx={require,__dirname,console};vm.runInNewContext(code,ctx);
const {simulate,config}=ctx.model;
const policies={
 fullOneBall:{saveForLock:true,waterUntilLockReady:true,ballsPerCycle:1},
 fullUpToTwo:{saveForLock:true,waterUntilLockReady:true,ballsPerCycle:2},
 fullFrequentBall:{saveForLock:true,waterUntilLockReady:true},
 noBall:{noBall:true,saveForLock:true,waterUntilLockReady:true},
 noLock:{noLock:true},
 iceWater:{noLock:true,noBall:true},
 iceOnly:{noLock:true,noBall:true,noWater:true,alwaysIce:true},
 waterOnly:{noLock:true,noBall:true,noIce:true},
 ballOnly:{noLock:true,noIce:true,noWater:true},
 lockOnly:{noIce:true,noWater:true,noBall:true},
 iceLock:{noBall:true,noWater:true,alwaysIce:true,reserveLock:true},
 iceBallLock:{noWater:true,alwaysIce:true,reserveLock:true},
 iceBall:{noLock:true,noWater:true,alwaysIce:true},
 waterBall:{noLock:true,noIce:true},
};
const labels={fullOneBall:'完整循环·每轮最多1球',fullUpToTwo:'完整循环·每轮最多2球',fullFrequentBall:'完整循环·水球可用即尝试',noBall:'冰锥＋水流＋冰凌锁',noLock:'冰锥＋水流＋水球',iceWater:'冰锥＋水流',iceOnly:'仅冰锥',waterOnly:'仅水流',ballOnly:'仅水球',lockOnly:'仅冰凌锁',iceLock:'冰锥＋冰凌锁',iceBallLock:'冰锥＋水球＋冰凌锁',iceBall:'冰锥＋水球',waterBall:'水流＋水球'};
const rows=[];let build;
function run(prefixCount,duration,potion,policy,options={},overrides={}){
 const r=simulate(10,{...rules,arcaneCount:prefixCount,...overrides},{potions:0,buffSeconds:potion?duration:0,duration,...policies[policy],...options});
 build??=r.build;
 const gaps=r.lockTimes.slice(1).map((x,i)=>x-r.lockTimes[i]),tail=gaps.slice(-5);
 const row={arcaneCount:prefixCount,duration,potion,policy,...options,overrides,dps:r.dps,damage:r.damage,shares:r.shares,casts:r.casts,hits:r.hits,
   gap:r.meanLockInterval,tailGap:tail.length?tail.reduce((a,b)=>a+b,0)/tail.length:null,
   freezeTimes:r.freezeTimes,lockTimes:r.lockTimes,ballTimes:r.ballTimes,firstFreeze:r.firstFreeze,
   maxMana:r.build.maxMana,recovery:r.build.recovery+(potion?.01*r.build.maxMana:0),maxLife:r.build.maxLife,
   endMana:r.endMana,minMana:r.minMana,spentPerSecond:r.ledger.spent/duration,overflow:r.ledger.overflow,
   idleSeconds:r.ledger.idleTicks/60,firstDry:r.firstDry,lifeSpent:r.lifeSpent,life:r.life,firstConversion:r.firstConversion,
   cycleCounts:r.cycleCounts,stats:r.build.gear,prefixAllocation:r.build.prefixAllocation,handTicks:r.handTicks};
 rows.push(row);return row;
}
for(const prefix of [7,0])for(const duration of [60,120,300,600])for(const potion of [true,false]){
 for(const policy of Object.keys(policies))run(prefix,duration,potion,policy);
}
// Prefix sweep using each of the representative cycle choices.
for(let prefix=1;prefix<7;prefix++)for(const duration of [120,300])for(const policy of ['fullOneBall','fullUpToTwo','fullFrequentBall','noBall'])run(prefix,duration,true,policy);
// Finite life conversion sensitivity, with full250HP and no healing or incoming hits.
for(const prefix of [7,0])for(const policy of ['fullOneBall','fullUpToTwo','iceOnly','iceLock'])run(prefix,300,true,policy,{lifeConversion:true});
// Hit-rate sensitivity uses several seeded runs; lock damage remains full by model assumption.
const hitSamples=[];
for(const seed of Array.from({length:16},(_,i)=>20261020+i)){
 const r=simulate(10,{...rules,arcaneCount:7},{potions:0,buffSeconds:300,duration:300,...policies.fullOneBall,hitChance:.75,seed});
 hitSamples.push({seed,dps:r.dps,casts:r.casts,freezeCount:r.freezeTimes.length,gap:r.meanLockInterval});
}
// Ball test override is isolated below; formal balance always uses15seconds.
const ball=config.skills.find(x=>x.id==='WaterBallSkill'),formalBallCD=ball.cd,formalK=ball.k;
ball.cd=1;ball.k=formalK*formalBallCD;
for(const policy of ['ballOnly','waterBall','fullFrequentBall'])run(7,120,true,policy,{testBallCooldown:true});
ball.cd=formalBallCD;ball.k=formalK;
assert(rows.filter(r=>r.policy==='lockOnly').every(r=>r.dps===0&&r.casts.lock===0),'Sealed lock cannot cast alone');
for(const r of rows){
 const allowed={iceOnly:['ice'],waterOnly:['water'],ballOnly:['ball'],lockOnly:[]}[r.policy];
 if(allowed)for(const key of Object.keys(r.casts))assert(allowed.includes(key)||r.casts[key]===0,`${r.policy} leaked${key}`);
 assert(r.life>0&&r.endMana>=-1e-5&&r.endMana<=r.maxMana+1e-5);
 if(r.policy==='fullOneBall')assert(r.cycleCounts.every(n=>n<=1));
 if(r.policy==='fullUpToTwo')assert(r.cycleCounts.every(n=>n<=2));
}
const file=path.resolve(__dirname,'../../../../ElainaModSkills/Data/SkillBalance.json');
assert.equal(crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex'),sourceSHA256);
const assumptions=[
 'Current stage10saved equipment at character50, all four skills at stage cap. Stage10is the earlier comparison convention, not a claim that a player fighting Twins already owns Hallowed armor.',
 'Primary tables have full opening mana, maintained120second Moon Dew granting1% each second, no instant elixir charges and no other potions. No-potion alternatives included.',
 'Single persistent zero-defense freezable target, all hits and expected standard crit. No Twins AI, movement, armor procs, damage taken, shields or healing. These numbers are comparable skill DPS, not actual boss DPS.',
 'Formal water-ball15second cooldown in primary rows; current icon callback1second override is an isolated stress case, preserving actual per-cast damage instead of repricing it at1second.',
 'Ice/lock45frame action; ball82frame minimum action with70frame fixed hit; water20frame exit. Freeze lasts4seconds, then60second cooldown; lock60seconds plus one freeze permission.',
 'Primary results disable life conversion to isolate sustainable mana. Additional cases consume a finite250HP pool at actual1%life/1%mana step, rounded as runtime, no healing or hits.',
 'One/two balls per cycle mean maximum limits; a ready affordable lock is never delayed only to fulfill the quota. No opening ball for quota policies.',
 'Long-window results remain affected by finite opening mana and action timing. No global optimization; prefix sweep is over listed policies only.',
];
fs.writeFileSync(path.join(__dirname,'twins-current-extremes-results.json'),JSON.stringify({date:'2026-10-20',sourceSHA256,rules,build,assumptions,rows,hitSamples},null,2)+'\n');
const f=(v,d=0)=>v==null?'—':v.toFixed(d);
let report='# 当前双子时期冰水循环与极端打法\n\n2026-10-20。只读分析，不修改正式配置或HTML。\n\n';
report+='## 保存配装\n\n'+JSON.stringify({level:build.level,loadout:build.loadout,maxMana:build.maxMana,maxLife:build.maxLife,gear:build.gear,skills:Object.fromEntries(Object.entries(build.skills).map(([k,v])=>[k,{level:v.skillLevel,damageLevel:v.damageLevel,cost:v.cost,cd:v.cd,interval:v.interval,damage:v.damage,freezeBuildup:v.freezeBuildup}]))},null,2)+'\n\n';
for(const prefix of [7,0]){
 report+=`## ${prefix===7?'全奥秘':'全增伤'}，月露全程覆盖\n\n| 打法 | 60秒DPS | 120秒DPS | 300秒DPS | 600秒DPS | 300秒锁/球 | 300秒锁间隔 |\n|---|---:|---:|---:|---:|---|---:|\n`;
 for(const policy of Object.keys(policies)){
 const get=t=>rows.find(r=>r.arcaneCount===prefix&&r.duration===t&&r.potion&&r.policy===policy&&!r.lifeConversion&&!r.testBallCooldown);
 const a=get(300);
 report+=`| ${labels[policy]} | ${f(get(60).dps)} | ${f(get(120).dps)} | ${f(a.dps)} | ${f(get(600).dps)} | ${a.casts.lock}/${a.casts.ball} | ${f(a.gap,1)}秒 |\n`;
 }
 report+='\n';
}
report+='## 全奥秘300秒：伤害分配、月露影响\n\n| 打法 | 冰锥% | 水流% | 水球% | 冰凌锁% | 无药DPS | 有药DPS | 有药结束蓝 |\n|---|---:|---:|---:|---:|---:|---:|---:|\n';
for(const policy of ['fullOneBall','fullUpToTwo','fullFrequentBall','noBall','iceOnly','iceLock','waterBall']){
 const get=p=>rows.find(r=>r.arcaneCount===7&&r.duration===300&&r.potion===p&&r.policy===policy&&!r.lifeConversion&&!r.testBallCooldown);
 const a=get(true),b=get(false);
 report+=`| ${labels[policy]} | ${f(a.shares.ice,1)} | ${f(a.shares.water,1)} | ${f(a.shares.ball,1)} | ${f(a.shares.lock,1)} | ${f(b.dps)} | ${f(a.dps)} | ${f(a.endMana)} |\n`;
}
report+='\n## 有限生命转魔力（无受伤无治疗，300秒）\n\n| 前缀 | 打法 | DPS | 生命消耗 | 剩余生命 | 首次转化 |\n|---|---|---:|---:|---:|---:|\n';
for(const r of rows.filter(r=>r.lifeConversion))report+=`| ${r.arcaneCount===7?'奥秘':'增伤'} | ${labels[r.policy]} | ${f(r.dps)} | ${r.lifeSpent} | ${r.life} | ${f(r.firstConversion,1)}秒 |\n`;
report+='\n## 水球测试CD1秒（120秒，全奥秘有月露）\n\n';
for(const r of rows.filter(r=>r.testBallCooldown))report+=`- ${labels[r.policy]}：${f(r.dps)}DPS，${r.casts.ball}球/${r.casts.lock}锁。\n`;
report+='\n## 前缀及循环策略扫描\n\n';
for(const duration of [120,300]){
 const best=rows.filter(r=>r.duration===duration&&r.potion&&!r.lifeConversion&&!r.testBallCooldown&&['fullOneBall','fullUpToTwo','fullFrequentBall','noBall'].includes(r.policy)).reduce((a,b)=>a.dps>b.dps?a:b);
 report+=`- ${duration}秒所测最高：${best.arcaneCount}奥秘，${labels[best.policy]}，${f(best.dps)}DPS，${best.casts.lock}锁/${best.casts.ball}球。\n`;
}
const sampleMean=hitSamples.reduce((a,r)=>a+r.dps,0)/hitSamples.length;
report+=`\n## 75%普通攻击/水球命中率敏感性\n\n16个固定种子，冰凌锁仍全中：完整每轮最多1球平均${f(sampleMean)}DPS，范围${f(Math.min(...hitSamples.map(r=>r.dps)))}～${f(Math.max(...hitSamples.map(r=>r.dps)))}。只代表独立丢失命中的模型，未模拟首领移动/连续空窗。\n`;
report+='\n'+assumptions.map(a=>'- '+a).join('\n')+'\n';
fs.writeFileSync(path.join(__dirname,'TWINS-CURRENT-EXTREMES.md'),report);console.log(report);
