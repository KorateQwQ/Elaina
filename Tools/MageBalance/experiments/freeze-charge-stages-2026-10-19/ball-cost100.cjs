'use strict';
// Water ball100 mana, preserved single-cast damage; no gameplay edits.
const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm');
const assert=require('node:assert/strict'),crypto=require('node:crypto');
const source=fs.readFileSync(path.join(__dirname,'ball-refund-sensitivity.cjs'),'utf8').split(/\r?\nconst candidates=\[/)[0]
  +'\nglobalThis.adapter={code,rules,sourceSHA256};';
const outer={require,__dirname,console};vm.runInNewContext(source,outer);
const {code,rules,sourceSHA256}=outer.adapter;
const ctx={require,__dirname,console};vm.runInNewContext(code,ctx);
const {simulate,config}=ctx.model;
const ball=config.skills.find(x=>x.id==='WaterBallSkill');
const original={...ball},product=ball.cost*ball.k;
const rows=[];
for(const stage of [8,10,13])for(const prefix of ['damage','arcane'])for(const duration of [120,300,600]){
  for(const cost of [150,100])for(const ballsPerLock of [0,1,2]){
    ball.cost=cost;ball.k=product/cost;
    const r=simulate(stage,{...rules,arcaneCount:prefix==='arcane'?7:0,ballLock:8,ballFreeze:10},
      {duration,potions:0,buffSeconds:duration,ballsPerLock,saveForLock:true,waterUntilLockReady:true});
    const gaps=r.lockTimes.slice(1).map((x,i)=>x-r.lockTimes[i]),tail=gaps.slice(-5);
    rows.push({stage,prefix,duration,cost,k:ball.k,ballsPerLock,dps:r.dps,casts:r.casts,
      gap:r.meanLockInterval,tailGap:tail.reduce((a,b)=>a+b,0)/tail.length,firstGap:gaps[0],
      ballDamage:r.build.skills.ball.damage,ballTimes:r.ballTimes,lockTimes:r.lockTimes,
      recovery:r.build.recovery+.01*r.build.maxMana,spentPerSecond:r.ledger.spent/duration,endMana:r.endMana});
  }
}
for(const r of rows){const old=rows.find(x=>x.stage===r.stage&&x.prefix===r.prefix&&x.duration===r.duration&&x.ballsPerLock===r.ballsPerLock&&x.cost===150);assert.equal(r.ballDamage,old.ballDamage);}
const file=path.resolve(__dirname,'../../../../ElainaModSkills/Data/SkillBalance.json');
assert.equal(crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex'),sourceSHA256);
const assumptions=[
  'Only analytical model parameters change: ball100mana, k2.25 preserves150*1.5pricing product; lock refund8seconds and target freeze refund10seconds.',
  'Replacement Moon Dew1% maximum mana/sec refreshed throughout, no old instant charges or separate0.5% potion.',
  'Strict0/1/2balls per completed lock interval; same ice opening, no pre-opening ball. May wait for mana/quota. Single zero-defense target, full hits, expected crit.',
  'Ball formal15second cooldown, minimum82frame action,20frame water exit, fixed70frame hit delay; no life conversion, armor procs, incoming hits or shield expenses.',
  'Finite starting mana can subsidize early cycles;600second tail intervals are reported separately. Representative policies, not globally optimal boss tests.',
];
fs.writeFileSync(path.join(__dirname,'ball-cost100-results.json'),JSON.stringify({sourceSHA256,original,assumptions,rows},null,2)+'\n');
let report='# 水球100耗蓝、伤害不变\n\n耗蓝150→100，k1.5→2.25；两项返还按8/10秒，只试算。\n\n';
for(const stage of [8,10,13])for(const prefix of ['damage','arcane']){
 report+=`## 阶段${stage} / ${prefix}\n\n| 耗蓝 | 每轮水球 | 120秒DPS/间隔 | 300秒DPS/间隔 | 600秒DPS/后5轮间隔 |\n|---|---:|---:|---:|---:|\n`;
 for(const cost of [150,100])for(const count of [0,1,2]){
 const get=t=>rows.find(x=>x.stage===stage&&x.prefix===prefix&&x.duration===t&&x.cost===cost&&x.ballsPerLock===count);
 const a=get(120),b=get(300),c=get(600),f=n=>n.toFixed(1);
 report+=`| ${cost} | ${count} | ${Math.round(a.dps)} / ${f(a.gap)}秒 | ${Math.round(b.dps)} / ${f(b.gap)}秒 | ${Math.round(c.dps)} / ${f(c.tailGap)}秒 |\n`;
 }
 report+='\n';
}
report+=assumptions.map(x=>'- '+x).join('\n')+'\n';
fs.writeFileSync(path.join(__dirname,'BALL-COST100.md'),report);console.log(report);
