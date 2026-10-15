'use strict';
// Freeze cooldown -> at least 3s of actual ice attack -> 4s freeze -> detonate.
// Separate from the live mod and earlier experiments.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const E = require('./simulate.cjs');
const {B,ids} = E;
const FPS=60, EPS=1e-7;
const frames=s=>Math.max(1,Math.round(s*FPS));
const defaults={
  duration:300, freezeBase:60, freezeDuration:4,freezeBuildSeconds:3,iceResetGap:1.2,
  // Historical experiments start at application; runtime starts after thaw/consumption.
  freezeCooldownStart:'application',
  // Per actual water attack / ball impact. No 30s hard cap, no ice accumulation during CD.
  waterDiscount:2/15, ballDiscount:6, waterWet:5, ballWet:15, waterBurst:5,
  filler:'water',waterWindow:15,prioritizeBuild:true,readyBasic:'ice',
  ballCost:150,ballCD:15,ballCast:1.35,ballK:1.8,
  lockCost:100,lockCD:20,lockCast:.2,lockDelay:1,lockBaseB:3.2,lockPerSecondB:2.5,
  thunderCD:120,thunderDuration:30,thunderCast:2,thunderDelay:1,
  thunderStartup:0,thunderDrain:15,thunderK:.4,dryThunderHit:.5,
  policy:'reserve',reserveHorizon:4,lockWait:0,omit:[],
};
function run(options={}) {
  const o={...defaults,...options},s=E.setup(options),b=B.calculate(s),M=b.maxMana;
  const T=o.duration,N=frames(T),regen=b.recovery,restore=M*s.combat.potionPercent/100;
  const scale=s.baseDps*b.skillMultiplier;
  const water=s.basicAttacks.find(x=>x.id===ids.water),ice=s.basicAttacks.find(x=>x.id===ids.ice);
  if(o.waterInterval!==undefined)water.interval=o.waterInterval;
  if(o.waterRatio!==undefined){water.ratio=o.waterRatio;water.ratioCurve=[];}
  if(o.iceRatio!==undefined){ice.ratio=o.iceRatio;ice.ratioCurve=[];}
  // Water outside the freeze cooldown uses waterReadyRatio instead of waterRatio.
  assert(o.waterReadyRatio===undefined||o.waterRatio!==undefined);
  let waterReadyHits=0;
  const ballDamage=s.baseDps*(s.rate/100/20)*o.ballCost*o.ballCD*o.ballK*b.skillMultiplier;
  const thunderTotalCost=o.thunderStartup+o.thunderDrain*o.thunderDuration;
  const thunderTotalDamage=o.thunderBudgetB!==undefined?o.thunderBudgetB*scale:s.baseDps*(s.rate/100/20)*thunderTotalCost*o.thunderCD*o.thunderK*b.skillMultiplier;
  const enabled=k=>!o.omit.includes(k);
  const records=Object.fromEntries(['water','ice','ball','lock','thunder'].map(k=>[k,{count:0,damage:0,spent:0,handSeconds:0}]));
  let mana=M,potions=s.combat.potions,potionMana=0,natural=0,waste=0,busyUntil=0,idleTicks=0,waterQuota=0,externalMana=0;
  let waterUntil=-1,ballUntil=-1,freezeUntil=-1,freezeStart=-Infinity,freezeReady=0,freezeId=0;
  let iceBuild=0,lastIceHit=-Infinity,buildStart=null;
  let ballReady=0,lockReady=0,thunderReady=frames(options.thunderFirst??2),cloud=null,tick=0;
  let frozenExpired=0,thunderPaidTicks=0,wetTicks=0,cloudWetTicks=0,cloudDryTicks=0,controlSacrificed=0;
  const events=[],actions=[],freezes=[],detonations=[],clouds=[],damageEvents=[],reductions=[],cooldownStarts=[];
  const cooldownOnEnd=o.freezeCooldownStart==='end';
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
  function endFreeze() {
    freezeUntil=-1;
    if(cooldownOnEnd) {
      freezeReady=tick+frames(o.freezeBase);
      cooldownStarts.push({time:tick/FPS,freezeId});
    }
  }
  function startFreeze() {
    if(freezeUntil>tick||tick<freezeReady)return;
    if(iceBuild+EPS<o.freezeBuildSeconds)return;
    freezeStart=tick;freezeUntil=tick+frames(o.freezeDuration);freezeReady=cooldownOnEnd?0:tick+frames(o.freezeBase);freezeId++;
    if(!cooldownOnEnd)cooldownStarts.push({time:tick/FPS,freezeId});
    freezes.push({id:freezeId,time:tick/FPS,earliestNext:cooldownOnEnd?null:freezeReady/FPS,buildStart:buildStart/FPS,iceSeconds:iceBuild});
    iceBuild=0;buildStart=null;
  }
  function credit(key,damage){records[key].damage+=damage;if(options.trace)damageEvents.push({time:tick/FPS,key,damage});}
  function settle() {
    for(let i=0;i<events.length;) {
      const e=events[i];if(e.at>tick){i++;continue;}events.splice(i,1);
      if(e.type==='hit') {
        if(e.key==='water'&&o.waterReadyRatio!==undefined&&freezeReady<=tick){e.damage*=o.waterReadyRatio/o.waterRatio;waterReadyHits++;}
        credit(e.key,e.damage);
        if(e.key==='water'){waterUntil=tick+frames(o.waterWet);discount('water');}
        if(e.key==='ball'){ballUntil=tick+frames(o.ballWet);discount('ball');}
        if(e.key==='ice'&&e.buildEligible&&tick>=freezeReady&&tick>=freezeUntil) {
          // Six complete .5s attack cycles = 3s; three projectiles share that
          // .5s contribution, never each adding .5s. Missed attacks contribute 0.
          if(e.at-lastIceHit>o.iceResetGap*FPS){iceBuild=0;buildStart=e.attackStart;}
          if(buildStart===null)buildStart=e.attackStart;
          iceBuild+=e.buildSeconds*(o.iceHitFraction??1);lastIceHit=tick;startFreeze();
        }
      } else if(e.type==='detonate') {
        const remaining=Math.max(0,(freezeUntil-tick)/FPS);
        const damageB=o.lockBaseB+o.lockPerSecondB*remaining;
        detonations.push({time:tick/FPS,freezeId:remaining>0?freezeId:null,remaining,damageB});
        controlSacrificed+=remaining;
        if(remaining>0)endFreeze();
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
    actions.push({key,time:tick/FPS,end:(tick+n)/FPS,phase:tick<freezeUntil?'frozen':tick<freezeReady?'cooldown':'ready'});
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
    if(freezeUntil===tick&&freezeUntil>=0){frozenExpired++;endFreeze();}
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
      const building=enabled('ice')&&tick>=freezeReady&&tick>=freezeUntil&&o.readyBasic==='ice';
      if(!acted&&!hold&&building&&o.prioritizeBuild) {
        if(canSpend(ice.cost)) {
          const start=tick,end=cast('ice',ice.cost,o.iceActionSeconds??ice.interval);
          events.push({type:'hit',key:'ice',at:end,damage:B.basicBudget(s,ice).total*b.e.multiplier*(o.iceHitFraction??1),
            attackStart:start,buildEligible:true,buildSeconds:ice.interval});acted=true;
        } else hold=true;
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
        const iceDue=enabled('ice')&&tick>=freezeReady&&tick>=freezeUntil&&o.readyBasic==='ice';
        const waterFiller=o.filler==='water'||o.filler==='mixed'&&freezeReady-tick<=o.waterWindow*FPS;
        const waterDue=o.readyBasic==='water'&&tick>=freezeReady&&enabled('water');
        const key=waterDue?'water':iceDue?'ice':waterQuota>0&&enabled('water')?'water':waterFiller&&enabled('water')?'water':enabled('ice')?'ice':enabled('water')?'water':null;
        if(key) {
          const basic=key==='water'?water:ice;
          if(canSpend(basic.cost)) {
            const start=tick,end=cast(key,basic.cost,key==='ice'?(o.iceActionSeconds??basic.interval):basic.interval);if(key==='water')waterQuota=Math.max(0,waterQuota-1);
            events.push({type:'hit',key,at:end,damage:B.basicBudget(s,basic).total*b.e.multiplier*(key==='ice'?(o.iceHitFraction??1):1),
              attackStart:start,buildEligible:tick>=freezeReady&&tick>=freezeUntil,buildSeconds:basic.interval});acted=true;
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
    const previous=freezes[i-1];
    const reduced=reductions.filter(r=>r.freezeId===previous.id).reduce((sum,r)=>sum+r.seconds,0);
    const cooldownStart=cooldownStarts.find(c=>c.freezeId===previous.id);
    assert(cooldownStart&&freezes[i].time-cooldownStart.time+reduced+EPS>=o.freezeBase,'freeze fired before actual cooldown progress');
  }
  assert(detonations.every(d=>d.remaining>=0&&d.remaining<=o.freezeDuration));
  assert(freezes.every(f=>f.iceSeconds+EPS>=o.freezeBuildSeconds&&f.time-f.buildStart+EPS>=o.freezeBuildSeconds));
  assert(new Set(detonations.filter(d=>d.freezeId!==null).map(d=>d.freezeId)).size===detonations.filter(d=>d.freezeId!==null).length);
  assert(Math.abs(records.thunder.spent-records.thunder.count*o.thunderStartup-thunderPaidTicks*o.thunderDrain/FPS)<1e-5);
  return {options:o,dps:damage/T,ratio:damage/T/b.vanilla,vanilla:b.vanilla,M,regen,endMana:mana,usedPotions:s.combat.potions-potions,idleSeconds:idleTicks/FPS,waterReadyHits,scale,basicMultiplier:b.e.multiplier,
    ledger:{initial:M,natural,potion:potionMana,externalMana,spent,end:mana,error},records,freezeCount:freezes.length,detonationCount:detonations.filter(x=>x.remaining>0).length,
    averageRemaining:detonations.length?controlSacrificed/detonations.length:0,wetCoverage:wetTicks/N,
    thunderPaidSeconds:thunderPaidTicks/FPS,thunderWetCoverage:thunderPaidTicks?cloudWetTicks/thunderPaidTicks:0,cloudDrySeconds:cloudDryTicks/FPS,
    freezeIntervals:freezes.slice(1).map((f,i)=>f.time-freezes[i].time),
    freezes,detonations,clouds,reductions,cooldownStarts,...(options.trace?{actions,damageEvents}:{})};
}
function pair(options={}) {const arcane=run({...options,prefix:'arcane'}),damage=run({...options,prefix:'damage'});return {arcane,damage,lead:arcane.dps/damage.dps-1};}
function compact(p) {return {lead:p.lead,...Object.fromEntries(['arcane','damage'].map(k=>[k,{dps:p[k].dps,ratio:p[k].ratio,freeze:p[k].freezeCount,lock:p[k].detonationCount,remaining:p[k].averageRemaining,thunderSeconds:p[k].thunderPaidSeconds,wet:p[k].wetCoverage,thunderWet:p[k].thunderWetCoverage,casts:Object.fromEntries(Object.entries(p[k].records).map(([id,r])=>[id,r.count])),idle:p[k].idleSeconds,potions:p[k].usedPotions,end:p[k].endMana}]))};}
const recommended={waterRatio:.9,iceRatio:.9,ballK:1.8,lockBaseB:3.2,lockPerSecondB:2.5,thunderBudgetB:34,thunderDrain:15,filler:'water'};
function search(options={}) {
  const all={arcane:[],damage:[]};
  for(const prefix of ['arcane','damage']) {
    for(const filler of options.filler?[options.filler]:['ice','mixed','water'])for(const policy of ['reserve','flow','hold'])
      for(const order of [['thunder','ball'],['ball','thunder']])for(const reserveHorizon of(policy==='reserve'?[2,4,8]:[0])) {
        all[prefix].push(run({...recommended,...options,prefix,filler,policy,order,reserveHorizon}));
      }
    all[prefix].sort((a,b)=>b.dps-a.dps);
  }
  return {arcane:all.arcane[0],damage:all.damage[0],lead:all.arcane[0].dps/all.damage[0].dps-1,casesPerPrefix:all.arcane.length};
}
module.exports={defaults,recommended,run,pair,compact,search};
