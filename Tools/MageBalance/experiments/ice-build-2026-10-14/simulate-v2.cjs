'use strict';
// Explicit freeze state and paid thunder ticks. Separate from the live mod.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const E = require('./simulate.cjs');
const {B,ids} = E;
const FPS=60, EPS=1e-7;
const frames=s=>Math.max(1,Math.round(s*FPS));
const defaults={
  duration:300, freezeBase:45, freezeDuration:4,
  // 5 hits/s * 91% available hand time / 15 + 3s / 15s ~= 0.503s/s
  // extra cooldown progress: 45 / 1.503 ~= 29.93s under maximal water spam.
  // These per-hit values are proposed, not measured game constants. No 30s clamp.
  waterDiscount:1/15, ballDiscount:3, waterWet:5, ballWet:15, waterBurst:5,
  filler:'ice',waterWindow:15,
  ballCost:150,ballCD:15,ballCast:1.35,ballK:1.8,
  lockCost:100,lockCD:20,lockCast:.2,lockDelay:1,lockBaseB:3.2,lockPerSecondB:4,
  thunderCD:120,thunderDuration:30,thunderCast:2,thunderDelay:1,
  thunderStartup:0,thunderDrain:10,thunderK:.75,dryThunderHit:.5,
  policy:'reserve',reserveHorizon:4,lockWait:0,omit:[],
};
function run(options={}) {
  const o={...defaults,...options},s=E.setup(options),b=B.calculate(s),M=b.maxMana;
  const T=o.duration,N=frames(T),regen=b.recovery,restore=M*s.combat.potionPercent/100;
  const scale=s.baseDps*b.skillMultiplier;
  const water=s.basicAttacks.find(x=>x.id===ids.water),ice=s.basicAttacks.find(x=>x.id===ids.ice);
  if(o.waterInterval!==undefined)water.interval=o.waterInterval;
  const ballDamage=s.baseDps*(s.rate/100/20)*o.ballCost*o.ballCD*o.ballK*b.skillMultiplier;
  const thunderTotalCost=o.thunderStartup+o.thunderDrain*o.thunderDuration;
  const thunderTotalDamage=o.thunderBudgetB!==undefined?o.thunderBudgetB*scale:s.baseDps*(s.rate/100/20)*thunderTotalCost*o.thunderCD*o.thunderK*b.skillMultiplier;
  const enabled=k=>!o.omit.includes(k);
  const records=Object.fromEntries(['water','ice','ball','lock','thunder'].map(k=>[k,{count:0,damage:0,spent:0,handSeconds:0}]));
  let mana=M,potions=s.combat.potions,potionMana=0,natural=0,waste=0,busyUntil=0,idleTicks=0,waterQuota=0,externalMana=0;
  let waterUntil=-1,ballUntil=-1,freezeUntil=-1,freezeStart=-Infinity,freezeReady=0,freezeId=0;
  let ballReady=0,lockReady=0,thunderReady=frames(options.thunderFirst??2),cloud=null,tick=0;
  let frozenExpired=0,thunderPaidTicks=0,wetTicks=0,cloudWetTicks=0,cloudDryTicks=0,controlSacrificed=0;
  const events=[],actions=[],freezes=[],detonations=[],clouds=[],damageEvents=[],reductions=[];
  const wet=()=>waterUntil>tick||ballUntil>tick;
  const affordable=cost=>cost<=M+EPS && (o.unlimitedMana||mana+potions*restore+EPS>=cost);
  function pay(cost,key) {
    if(!affordable(cost))return false;
    if(o.unlimitedMana&&mana<cost){externalMana+=cost-mana;mana=cost;}
    const used=mana+EPS>=cost?0:restore>0?Math.ceil((cost-mana-EPS)/restore):Infinity;
    assert(used<=potions);
    potions-=used;mana+=used*restore-cost;potionMana+=used*restore;
    // Each restored fraction is <= M and the smallest sufficient number is used.
    assert(mana<=M+EPS);mana=Math.max(0,mana);records[key].spent+=cost;
    return true;
  }
  function discount(kind) {
    if(!Number.isFinite(freezeStart)||freezeReady<=tick)return;
    const amount=Math.min(freezeReady-tick,(kind==='water'?o.waterDiscount:o.ballDiscount)*FPS);
    freezeReady-=amount;
    reductions.push({time:tick/FPS,kind,seconds:amount/FPS,freezeId});
  }
  function startFreeze() {
    if(freezeUntil>tick||tick<freezeReady)return;
    freezeStart=tick;freezeUntil=tick+frames(o.freezeDuration);freezeReady=tick+frames(o.freezeBase);freezeId++;
    freezes.push({id:freezeId,time:tick/FPS,earliestNext:freezeReady/FPS});
  }
  function credit(key,damage){records[key].damage+=damage;if(options.trace)damageEvents.push({time:tick/FPS,key,damage});}
  function settle() {
    for(let i=0;i<events.length;) {
      const e=events[i];if(e.at>tick){i++;continue;}events.splice(i,1);
      if(e.type==='hit') {
        credit(e.key,e.damage);
        if(e.key==='water'){waterUntil=tick+frames(o.waterWet);discount('water');}
        if(e.key==='ball'){ballUntil=tick+frames(o.ballWet);discount('ball');}
        if(e.key==='ice')startFreeze();
      } else if(e.type==='detonate') {
        const remaining=Math.max(0,(freezeUntil-tick)/FPS);
        const damageB=o.lockBaseB+o.lockPerSecondB*remaining;
        detonations.push({time:tick/FPS,freezeId:remaining>0?freezeId:null,remaining,damageB});
        controlSacrificed+=remaining;
        if(remaining>0)freezeUntil=tick;
        events.push({type:'hit',key:'lock',at:tick+frames(o.lockDelay),damage:damageB*scale});
      } else if(e.type==='cloud') {
        assert(!cloud);
        const log={start:tick/FPS,scheduledEnd:(tick+frames(o.thunderDuration))/FPS,end:null,reason:null,paidTicks:0};
        clouds.push(log);cloud={end:tick+frames(o.thunderDuration),log};
      }
    }
  }
  function cast(key,cost,seconds) {
    assert(tick>=busyUntil);assert(pay(cost,key));
    const n=frames(seconds);busyUntil=tick+n;
    records[key].count++;records[key].handSeconds+=Math.min(n,N-tick)/FPS;
    actions.push({key,time:tick/FPS,end:(tick+n)/FPS});
    return busyUntil;
  }
  function protectedMana() {
    if(o.policy!=='reserve'||!enabled('lock')||!enabled('ice'))return 0;
    const until=Math.max(0,(freezeReady-tick)/FPS);
    if(until>o.reserveHorizon)return 0;
    const net=regen-(cloud?o.thunderDrain:0);
    return Math.max(0,o.lockCost-Math.max(0,net)*until);
  }
  function canSpend(cost) {return affordable(cost+protectedMana());}
  for(tick=0;tick<N;tick++) {
    if(freezeUntil===tick&&freezeUntil>=0){frozenExpired++;freezeUntil=-1;}
    settle();
    if(wet())wetTicks++;
    if(cloud&&tick>=cloud.end){cloud.log.end=tick/FPS;cloud.log.reason='complete';cloud=null;}
    if(cloud) {
      if(pay(o.thunderDrain/FPS,'thunder')) {
        const hit=wet()?1:o.dryThunderHit;
        credit('thunder',thunderTotalDamage/o.thunderDuration/FPS*hit);
        thunderPaidTicks++;cloud.log.paidTicks++;if(wet())cloudWetTicks++;else cloudDryTicks++;
      } else {cloud.log.end=tick/FPS;cloud.log.reason='mana';cloud=null;}
    }
    if(tick>=busyUntil) {
      let acted=false,hold=false;
      const remaining=(freezeUntil-tick)/FPS;
      if(enabled('lock')&&tick>=lockReady&&remaining>o.lockCast+EPS&&tick>=freezeStart+Math.round(o.lockWait*FPS)) {
        if(affordable(o.lockCost)) {
          const end=cast('lock',o.lockCost,o.lockCast);lockReady=tick+frames(o.lockCD);
          events.push({type:'detonate',at:end});acted=true;
        } else hold=true; // Keep this scarce freeze window rather than spend on basics.
      }
      // Refresh wet before spending a long animation on a large spell. Strong
      // wet from a ball also covers targeting, so it can spare water upkeep.
      if(!acted&&!hold&&enabled('water')&&Math.max(waterUntil,ballUntil)-tick<=frames(.5))waterQuota=Math.max(waterQuota,o.waterBurst);
      if(!acted&&!hold&&waterQuota>0&&enabled('water')&&Math.max(waterUntil,ballUntil)-tick<=frames(.5)) {
        if(canSpend(water.cost)) {
          const end=cast('water',water.cost,water.interval);waterQuota--;
          events.push({type:'hit',key:'water',at:end,damage:B.basicBudget(s,water).total*b.e.multiplier});acted=true;
        }
      }
      const order=o.order||['thunder','ball'];
      for(const key of order) {
        if(acted||hold||!enabled(key))continue;
        if(key==='thunder'&&tick>=thunderReady&&!cloud&&tick+frames(o.thunderCast+o.thunderDelay)<N) {
          // Only the activation cost and next tick are needed, never the whole 30s budget.
          if(canSpend(o.thunderStartup+o.thunderDrain/FPS)) {
            const end=cast('thunder',o.thunderStartup,o.thunderCast);thunderReady=tick+frames(o.thunderCD);
            events.push({type:'cloud',at:end+frames(o.thunderDelay)});acted=true;
          } else if(o.policy==='hold')hold=true;
        }
        if(key==='ball'&&tick>=ballReady&&tick+frames(o.ballCast)<N) {
          if(canSpend(o.ballCost)) {
            const end=cast('ball',o.ballCost,o.ballCast);ballReady=tick+frames(o.ballCD);
            events.push({type:'hit',key:'ball',at:end,damage:ballDamage});acted=true;
          } else if(o.policy==='hold')hold=true;
        }
      }
      if(!acted&&!hold) {
        const iceDue=enabled('ice')&&tick>=freezeReady&&tick>=freezeUntil;
        const waterFiller=o.filler==='water'||o.filler==='mixed'&&freezeReady-tick<=o.waterWindow*FPS;
        const key=iceDue?'ice':waterQuota>0&&enabled('water')?'water':waterFiller&&enabled('water')?'water':enabled('ice')?'ice':enabled('water')?'water':null;
        if(key) {
          const basic=key==='water'?water:ice;
          if(canSpend(basic.cost)) {
            const end=cast(key,basic.cost,basic.interval);if(key==='water')waterQuota=Math.max(0,waterQuota-1);
            events.push({type:'hit',key,at:end,damage:B.basicBudget(s,basic).total*b.e.multiplier});acted=true;
          }
        }
      }
      if(!acted)idleTicks++;
    }
    const gain=Math.min(M-mana,regen/FPS);natural+=gain;waste+=regen/FPS-gain;mana+=gain;
    assert(mana>=-EPS&&mana<=M+EPS);
  }
  settle();
  if(cloud){cloud.log.end=T;cloud.log.reason=cloud.end<=N?'complete':'horizon';}
  const spent=Object.values(records).reduce((v,r)=>v+r.spent,0),damage=Object.values(records).reduce((v,r)=>v+r.damage,0);
  const error=M+natural+potionMana+externalMana-spent-mana;
  assert(Math.abs(error)<1e-5,`ledger ${error}`);
  assert(actions.every((a,i)=>!i||a.time+EPS>=actions[i-1].end));
  assert(Math.abs(Object.values(records).reduce((sum,r)=>sum+r.handSeconds,0)+idleTicks/FPS-T)<1e-6);
  for(let i=1;i<freezes.length;i++) {
    const previous=freezes[i-1],elapsed=freezes[i].time-previous.time;
    const reduced=reductions.filter(r=>r.freezeId===previous.id).reduce((sum,r)=>sum+r.seconds,0);
    assert(elapsed+reduced+EPS>=o.freezeBase,'freeze fired before actual cooldown progress');
  }
  assert(detonations.every(d=>d.remaining>=0&&d.remaining<=o.freezeDuration));
  assert(new Set(detonations.filter(d=>d.freezeId!==null).map(d=>d.freezeId)).size===detonations.filter(d=>d.freezeId!==null).length);
  assert(Math.abs(records.thunder.spent-records.thunder.count*o.thunderStartup-thunderPaidTicks*o.thunderDrain/FPS)<1e-5);
  return {options:o,dps:damage/T,ratio:damage/T/b.vanilla,vanilla:b.vanilla,M,regen,endMana:mana,usedPotions:s.combat.potions-potions,idleSeconds:idleTicks/FPS,
    ledger:{initial:M,natural,potion:potionMana,externalMana,spent,end:mana,error},records,freezeCount:freezes.length,detonationCount:detonations.filter(x=>x.remaining>0).length,
    averageRemaining:detonations.length?controlSacrificed/detonations.length:0,wetCoverage:wetTicks/N,
    thunderPaidSeconds:thunderPaidTicks/FPS,thunderWetCoverage:thunderPaidTicks?cloudWetTicks/thunderPaidTicks:0,cloudDrySeconds:cloudDryTicks/FPS,
    freezeIntervals:freezes.slice(1).map((f,i)=>f.time-freezes[i].time),
    freezes,detonations,clouds,reductions,...(options.trace?{actions,damageEvents}:{})};
}
function pair(options={}) {const arcane=run({...options,prefix:'arcane'}),damage=run({...options,prefix:'damage'});return {arcane,damage,lead:arcane.dps/damage.dps-1};}
function compact(p) {return {lead:p.lead,...Object.fromEntries(['arcane','damage'].map(k=>[k,{dps:p[k].dps,ratio:p[k].ratio,freeze:p[k].freezeCount,lock:p[k].detonationCount,remaining:p[k].averageRemaining,thunderSeconds:p[k].thunderPaidSeconds,wet:p[k].wetCoverage,thunderWet:p[k].thunderWetCoverage,casts:Object.fromEntries(Object.entries(p[k].records).map(([id,r])=>[id,r.count])),idle:p[k].idleSeconds,potions:p[k].usedPotions,end:p[k].endMana}]))};}
const recommended={ballK:1.8,lockBaseB:3.2,lockPerSecondB:2.5,thunderK:.4,thunderDrain:15,filler:'mixed'};
function search(options={}) {
  const all={arcane:[],damage:[]};
  for(const prefix of ['arcane','damage']) {
    for(const filler of ['ice','mixed','water'])for(const policy of ['reserve','flow','hold'])
      for(const order of [['thunder','ball'],['ball','thunder']])for(const reserveHorizon of(policy==='reserve'?[2,4,8]:[0])) {
        all[prefix].push(run({...recommended,...options,prefix,filler,policy,order,reserveHorizon}));
      }
    all[prefix].sort((a,b)=>b.dps-a.dps);
  }
  return {arcane:all.arcane[0],damage:all.damage[0],lead:all.arcane[0].dps/all.damage[0].dps-1,casesPerPrefix:all.arcane.length};
}
function verify() {
  const near=(a,b)=>assert(Math.abs(a-b)<1e-6,`${a} != ${b}`);
  const noWater=run({...recommended,omit:['water','ball','thunder','lock'],unlimitedMana:true});
  noWater.freezeIntervals.forEach(x=>near(x,45));
  const maxWater=run({...recommended,unlimitedMana:true,filler:'water',omit:['thunder']});
  maxWater.freezeIntervals.forEach(x=>assert(x>=29&&x<=32));
  const faster=run({...recommended,unlimitedMana:true,filler:'water',omit:['thunder'],waterInterval:.1});
  assert(faster.freezeIntervals.every(x=>x<30),'30s must not be a hard cap');
  const noIce=run({...recommended,omit:['ice']});assert.equal(noIce.freezeCount,0);assert.equal(noIce.records.lock.count,0);
  const baseline=run({...recommended,trace:true}),delayed=run({...recommended,lockWait:2});
  near(baseline.detonations[0].remaining,3.8);near(delayed.detonations[0].remaining,1.8);
  near(baseline.detonations[0].damageB-delayed.detonations[0].damageB,5);
  assert(baseline.detonations.every(x=>x.damageB<=13.2+EPS));
  assert.equal(baseline.records.thunder.count,3);near(baseline.thunderPaidSeconds,90);near(baseline.records.thunder.spent,1350);
  assert(baseline.clouds.every(c=>c.reason==='complete'&&c.paidTicks===1800));
  const expensive=run({...recommended,thunderDrain:20,thunderBudgetB:43.2,prefix:'damage'});
  assert(expensive.clouds.some(c=>c.reason==='mana'));
  assert(expensive.thunderPaidSeconds<90);near(expensive.records.thunder.spent,expensive.thunderPaidSeconds*20);
  const dry=run({...recommended,omit:['water','ball']});near(dry.thunderWetCoverage,0);
  near(dry.records.thunder.damage/dry.thunderPaidSeconds,43.2*E.setup({}).baseDps*B.calculate(E.setup({})).skillMultiplier/30*.5);
  // One isolated fully wet cloud: all paid slices earn exactly the same total
  // budget; start at t+2s cast+1s delay, no up-front 450-mana requirement.
  const isolated=run({...recommended,duration:60,omit:['ice','ball','lock'],filler:'water'});
  near(isolated.thunderPaidSeconds,30);near(isolated.records.thunder.spent,450);
  near(isolated.records.thunder.damage,43.2*E.setup({}).baseDps*B.calculate(E.setup({})).skillMultiplier);
  return {checks:['45s freeze CD without water hits','Maximal current water spam gives about 30s without a cap; faster attack speed breaks below 30s',
    'Freeze requires an ice hit; each frozen state can be detonated at most once',
    '2s later detonation loses exactly 5B at the same snapshot',
    '3 complete clouds pay 1350 mana over 90s; interrupted clouds pay and deal damage only for actual uptime',
    'Wet/dry thunder accuracy and full-cloud budget verified independently',
    'Every run checks mana conservation, exclusive hand time, actual CD reductions, and remaining-freeze bounds'],
    calibration:maxWater,accelerated:faster,baseNoWater:noWater};
}
module.exports={defaults,recommended,run,pair,compact,search,verify};
if(require.main===module) {
  const verified=verify();
  const results={date:'2026-10-14',version:2,duration:300,assumptions:{
    cooldown:'Starts when freeze is applied. Every water hit subtracts 1/15s, each ball impact subtracts 3s. No cap or one-reduction-per-cycle limit.',
    freeze:'After cooldown expires, the next ice-cone cast impact freezes for 4s; no additional unknown accumulation threshold.',
    detonation:'0.2s cast; at completion consume all remaining freeze and snapshot bonus. Damage lands 1s later. Lock CD stays 20s.',
    wet:'Water lasts 5s, ball 15s; either gives full thunder targeting; dry thunder realizes 50% as an explicit unmeasured assumption.',
    thunder:'120s cooldown from cast start, 2s cast + 1s delay, then off-hand 30s; 0 up-front mana, pay per 1/60s slice, auto-potion allowed; terminate on inability to pay.',
    equipment:'Same archived source-config.json as first experiment, 300s no hits, five 30% potions, 2.5% max mana regen +5/s, no nebula mana bridge.',
    status:'Design hypotheses, not implemented game mechanics. Coefficients and timing assumptions require in-game confirmation.'},
    recommended:{...defaults,...recommended},checks:verified.checks,
    sourceConfigSha256:require('node:crypto').createHash('sha256').update(fs.readFileSync(path.join(__dirname,'source-config.json'))).digest('hex'),
    engineSha256:require('node:crypto').createHash('sha256').update(fs.readFileSync(path.resolve(__dirname,'../../index.html'))).digest('hex'),
    calibration:verified.calibration,accelerated:verified.accelerated,
    baseline:pair({...recommended}),hat:pair({...recommended,hat:true}),bestTested:search(),hatBestTested:search({hat:true}),
    detonationDelay:[0,1,2,3].map(lockWait=>({lockWait,result:pair({...recommended,lockWait})})),
    ablations:['water','ice','ball','lock','thunder'].map(omitted=>({omitted,result:search({omit:[omitted]})})),
    sensitivity:[
      {label:'No potions',options:{potions:0}},
      {label:'20 mana/s; hold thunder damage budget at 43.2B',options:{thunderDrain:20,thunderBudgetB:43.2}},
      {label:'Per-hit freeze reduction -25%',options:{waterDiscount:1/20,ballDiscount:2.25}},
      {label:'Per-hit freeze reduction +25%',options:{waterDiscount:1/12,ballDiscount:3.75}},
      {label:'All water filler',options:{filler:'water'}},
      {label:'Ice filler except wet upkeep',options:{filler:'ice'}},
      {label:'Ball wet duration 10s instead of 15s',options:{ballWet:10}},
    ].map(x=>({...x,result:pair({...recommended,...x.options})})),
    shorterWetAblation:{full:pair({...recommended,ballWet:10}),withoutWater:search({ballWet:10,omit:['water']})},
  };
  fs.writeFileSync(path.join(__dirname,'results-v2.json'),JSON.stringify(results,(key,value)=>['reductions','actions','damageEvents'].includes(key)?undefined:value,2)+'\n');
  fs.writeFileSync(path.join(__dirname,'candidate-parameters-v2.json'),JSON.stringify({experimentOnly:true,...results.assumptions,parameters:results.recommended},null,2)+'\n');
  console.log(JSON.stringify({checks:results.checks,baseline:compact(results.baseline),hat:compact(results.hat),bestTested:compact(results.bestTested),hatBestTested:compact(results.hatBestTested),delay:results.detonationDelay.map(x=>({seconds:x.lockWait,A:x.result.arcane.ratio,D:x.result.damage.ratio})),ablations:results.ablations.map(x=>({omit:x.omitted,A:x.result.arcane.ratio,D:x.result.damage.ratio})),sensitivity:results.sensitivity.map(x=>({label:x.label,A:x.result.arcane.ratio,D:x.result.damage.ratio,thunderSecondsA:x.result.arcane.thunderPaidSeconds,thunderSecondsD:x.result.damage.thunderPaidSeconds}))},null,2));
}
