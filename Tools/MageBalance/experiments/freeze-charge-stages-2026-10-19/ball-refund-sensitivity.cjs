'use strict';
// Change only the two water-ball refund amounts inside the analytical model.
const fs=require('node:fs'), path=require('node:path'), vm=require('node:vm');
const assert=require('node:assert/strict'), crypto=require('node:crypto');
const source=fs.readFileSync(path.join(__dirname,'balls-per-lock.cjs'),'utf8').split(/\r?\nconst scenarios = \{/)[0]
  +'\nglobalThis.adapter={code,rules,sourceSHA256};';
const outer={require,__dirname,console}; vm.runInNewContext(source,outer);
const {code,rules,sourceSHA256}=outer.adapter;
const ctx={require,__dirname,console}; vm.runInNewContext(code,ctx);
const {simulate}=ctx.model;
const candidates=[
  {name:'noBall',ballLock:3,ballFreeze:4,ballsPerLock:0},
  {name:'current',ballLock:3,ballFreeze:4,ballsPerLock:1},
  {name:'6/8',ballLock:6,ballFreeze:8,ballsPerLock:1},
  {name:'8/10',ballLock:8,ballFreeze:10,ballsPerLock:1},
  {name:'10/14',ballLock:10,ballFreeze:14,ballsPerLock:1},
  {name:'12/16',ballLock:12,ballFreeze:16,ballsPerLock:1},
];
const rows=[];
for(const stage of [8,13])for(const prefix of ['damage','arcane'])for(const candidate of candidates){
  for(const duration of [120,300,600])for(const freeMana of [false,true]){
    if(freeMana&&duration!==120)continue;
    const r=simulate(stage,{...rules,arcaneCount:prefix==='arcane'?7:0,ballLock:candidate.ballLock,ballFreeze:candidate.ballFreeze},
      {duration,potions:0,buffSeconds:duration,freeMana,ballsPerLock:candidate.ballsPerLock,saveForLock:true,waterUntilLockReady:true});
    const gaps=r.lockTimes.slice(1).map((x,i)=>x-r.lockTimes[i]);
    const tail=gaps.slice(-5);
    rows.push({stage,prefix,candidate:candidate.name,duration,freeMana,dps:r.dps,casts:r.casts,
      gap:r.meanLockInterval,firstGap:gaps[0],tailGap:tail.reduce((a,b)=>a+b,0)/tail.length,
      recovery:r.build.recovery+.01*r.build.maxMana,spentPerSecond:r.ledger.spent/duration,endMana:r.endMana});
  }
}
const file=path.resolve(__dirname,'../../../../ElainaModSkills/Data/SkillBalance.json');
assert.equal(crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex'),sourceSHA256);
const assumptions=[
  'Hypothetical replacement Moon Dew, full-fight1% max mana/sec, zero instant charges, no separate0.5% potion.',
  'Only the two ball refunds change; damage, costs,15second ball CD and60second lock/freeze CD remain saved values.',
  'One ball in each completed lock interval, same ice opening and minimum ball action; no-ball control included.',
  'freeMana diagnoses action/cooldown limits. Finite cases include full starting mana and water recovery; no life conversion, shields or damage taken.',
  'Single zero-defense freezable target, full hits, expected critical damage; same approximate action/flight timings and invariants as balls-per-lock.cjs.',
];
fs.writeFileSync(path.join(__dirname,'ball-refund-sensitivity-results.json'),JSON.stringify({sourceSHA256,candidates,assumptions,rows},null,2)+'\n');
let report='# 水球双冷却返还敏感性\n\n只试算、不改正式参数。候选名称为“冰凌锁CD返还秒数/冻结CD返还秒数”。\n\n';
const f=x=>x.toFixed(1);
for(const stage of [8,13])for(const prefix of ['damage','arcane']){
  report+=`## 阶段${stage} / ${prefix}\n\n| 方案 | 无魔力约束间隔 | 120秒DPS/间隔 | 300秒DPS/间隔 | 600秒DPS/后5轮间隔 |\n|---|---:|---:|---:|---:|\n`;
  for(const c of candidates){
    const get=(duration,freeMana=false)=>rows.find(r=>r.stage===stage&&r.prefix===prefix&&r.candidate===c.name&&r.duration===duration&&r.freeMana===freeMana);
    const a=get(120),b=get(300),d=get(600),free=get(120,true);
    report+=`| ${c.name} | ${f(free.gap)}秒 | ${Math.round(a.dps)} / ${f(a.gap)}秒 | ${Math.round(b.dps)} / ${f(b.gap)}秒 | ${Math.round(d.dps)} / ${f(d.tailGap)}秒 |\n`;
  }
  report+='\n';
}
report+=assumptions.map(s=>'- '+s).join('\n')+'\n';
fs.writeFileSync(path.join(__dirname,'BALL-REFUND-SENSITIVITY.md'),report);
console.log(report);
