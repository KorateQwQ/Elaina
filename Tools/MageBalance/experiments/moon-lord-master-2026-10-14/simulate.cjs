'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const E=require('../ice-build-2026-10-14/simulate.cjs');
const F=60,EPS=1e-7,frames=s=>Math.round(s*F),clone=x=>JSON.parse(JSON.stringify(x));
const candidate={waterRatio:.9,iceRatio:.9,waterDiscount:.16,ballDiscount:4,freezeCD:60,freezeDuration:4,build:3,
  resetGap:1.2,ballCost:150,ballCD:15,ballCast:1.35,ballB:8.1,lockCost:100,lockCD:20,lockCast:.2,lockDelay:1,lockBaseB:3.2,lockPerSecondB:2.5,
  thunderCD:120,thunderDuration:30,thunderCast:2,thunderDelay:1,thunderDrain:15,thunderB:34};
const profiles={
  ideal:{water:1,ice:1,ball:1,lock:1,thunder:1,dodgeEvery:0,dodgeSeconds:0,hurtEvery:0,hurtDamage:0,react:0},
  normal:{water:.8,ice:.75,ball:.85,lock:.9,thunder:.95,dodgeEvery:12,dodgeSeconds:1.5,hurtEvery:18,hurtDamage:150,react:.25},
  heavy:{water:.65,ice:.5,ball:.7,lock:.8,thunder:.85,dodgeEvery:8,dodgeSeconds:2,hurtEvery:10,hurtDamage:180,react:.5},
};
// Vanilla single-player Master stats: normal life * 3 * .75 * float(.85), truncation.
const bossStats=[{id:'head',normalHP:45000,defense:50},{id:'left',normalHP:25000,defense:40},
  {id:'right',normalHP:25000,defense:40},{id:'core',normalHP:50000,defense:70}].map(x=>({...x,maxHP:Math.floor(x.normalHP*3*.75*Math.fround(.85))}));
const totalHP=bossStats.reduce((s,x)=>s+x.maxHP,0);assert.equal(totalHP,277311);
// Attack tables from local Terraria NPC.cs InitializeMoonLordAttacks(), mode index0.
const schedules={left:[[0,50],[1,70],[2,330],[0,60],[3,90]],right:[[1,70],[0,50],[3,90],[0,60],[2,330]],
  head:[[3,180],[0,30],[2,435],[3,180],[1,375]]};
function animationGoal(id,phaseTick) {
  let t=phaseTick,sequence=schedules[id],period=sequence.reduce((n,x)=>n+x[1],0);t%=period;
  let state,duration;for(const row of sequence){[state,duration]=row;if(t<duration)break;t-=duration;}
  if(id==='head')return state===0||state===2||state===1&&t>=duration-15?15:0;
  if(state===0||state===3)return 21;
  if(state===1)return 0;
  return t<30?0:t<210?7:t<282?0:t<287?7:t<292?14:21;
}
function gear(o) {
  const s=clone(E.source);E.B.applyLoadout(s,o.stage,false);s.slots=o.slots;
  if(o.hat)s.e.items[3].id='elainaHat';
  if(o.prefix==='damage')E.B.applyDamagePrefixes(s,'e');else for(const i of E.B.equippedSlots(s,'e'))s.e.items[i].prefix='arcane';
  const b=E.B.calculate(s);return {s,b};
}
function run(options={}) {
  const o={stage:19,slots:7,hat:true,prefix:'arcane',potions:5,potionPercent:50,regenPercent:2.5,profile:'normal',seed:1,
    maxSeconds:300,thunderFirst:2,allowThunder:true,healthRegen:2,healthPotionHeal:150,healthPotionCD:60,
    simulateSurvival:true,sharedFreeze:true,...options};
  const p=typeof o.profile==='string'?profiles[o.profile]:o.profile,c={...candidate,...o.tune};
  const {s,b}=gear(o),M=b.maxMana,scale=s.baseDps*b.e.multiplier,restore=M*o.potionPercent/100,regen=5+M*o.regenPercent/100;
  let seed=o.seed>>>0;function random(){seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;}
  const yes=prob=>prob===1||random()<prob;
  const parts=bossStats.map(x=>({...x,hp:x.maxHP,aiTime:0,closedFrame:0,wetUntil:-1,freezeUntil:-1,freezeReady:0,iceBuild:0,lastIceHit:-Infinity,
    freezeID:0,freezeStart:-Infinity}));
  // One boss-wide state survives eye deaths, target switches and core opening.
  // Wet remains per NPC: the user correction specifies a unified freeze system.
  const sharedFrost={freezeUntil:-1,freezeReady:0,iceBuild:0,lastIceHit:-Infinity,freezeID:0,freezeStart:-Infinity};
  const frost=part=>o.sharedFreeze?sharedFrost:part;
  const records=Object.fromEntries(['water','ice','ball','lock','thunder'].map(k=>[k,{count:0,damage:0,spent:0,attempted:0,invalidDamage:0,overkill:0}]));
  const events=[],freezeEvents=[],lockEvents=[],hurtEvents=[],partDeaths=[],clouds=[],manaSamples=[],cooldownEvents=[];
  let mana=M,potions=o.potions,potionMana=0,regenEffective=0,regenWaste=0,spent=0,hurtPaid=0,healthDamage=0;
  let health=300,healthPotions=0,healthPotionReady=0,playerDead=false;
  let busy=0,ballReady=0,lockReady=0,thunderReady=frames(o.thunderFirst),cloud=null,target=null;
  let tick=0,elapsed=0,coreOpened=null,killed=false,cloudTicks=0,damagePerPart={};
  for(const part of parts)damagePerPart[part.id]=Object.fromEntries(Object.keys(records).map(k=>[k,0]));
  const isOpen=part=>part&&part.hp>EPS&&tick>=60&&(part.id==='core'?coreOpened!==null&&tick>=coreOpened:part.closedFrame<(part.id==='head'?15:21));
  const wet=part=>part&&part.wetUntil>tick;
  const canPay=cost=>cost<=M+EPS&&mana+potions*restore+EPS>=cost;
  function pay(cost,key) {
    if(!canPay(cost))return false;
    const used=mana+EPS>=cost?0:restore>0?Math.ceil((cost-mana-EPS)/restore):Infinity;
    assert(used<=potions);potions-=used;potionMana+=used*restore;mana+=used*restore-cost;
    assert(mana<=M+EPS);mana=Math.max(0,mana);spent+=cost;records[key].spent+=cost;return true;
  }
  function takeDamage(part,key,budget,hitCount=1) {
    records[key].attempted+=budget;
    if(!isOpen(part)){records[key].invalidDamage+=budget;return false;}
    // The gear multiplier includes crit; defense is applied before the crit roll.
    const defenseLoss=part.defense*.5*(1+b.e.crit/100)*hitCount;
    const defended=Math.max(hitCount,budget-defenseLoss),actual=Math.min(part.hp,defended);
    records[key].overkill+=defended-actual;records[key].damage+=actual;damagePerPart[part.id][key]+=actual;part.hp-=actual;
    if(part.hp<=EPS) {part.hp=0;partDeaths.push({time:tick/F,id:part.id});}
    return true;
  }
  function chooseTarget() {
    // Commit to the current eye while building a freeze; otherwise prefer the
    // easier-to-sustain hand windows, then head. Never hit multiple parts for free.
    if(isOpen(target)&&frost(target).iceBuild>0&&tick>=frost(target).freezeReady)return target;
    const available=parts.filter(isOpen);
    if(isOpen(target)&&frost(target).freezeUntil>tick)return target;
    const frozen=available.find(x=>frost(x).freezeUntil>tick);if(frozen)return frozen;
    const head=available.find(x=>x.id==='head');
    const hands=available.filter(x=>x.id==='left'||x.id==='right');
    if(hands.length) {
      if(hands.includes(target))return target;
      return hands.sort((a,b)=>a.hp-b.hp)[0];
    }
    return head||available.find(x=>x.id==='core')||null;
  }
  function discount(part,seconds){
    const state=frost(part);
    if(state.freezeReady>tick){const amount=Math.min(seconds*F,state.freezeReady-tick);state.freezeReady-=amount;
      cooldownEvents.push({time:tick/F,target:part.id,freezeID:state.freezeID,seconds:amount/F});}
  }
  function iceHit(e,hits) {
    const part=e.part;
    const state=frost(part);
    if(!e.buildEligible||part.hp<=0||tick<state.freezeReady||tick<state.freezeUntil)return;
    if(tick-state.lastIceHit>c.resetGap*F)state.iceBuild=0;
    state.iceBuild+=.5*hits/3;state.lastIceHit=tick;
    if(state.iceBuild+EPS>=c.build) {
      state.iceBuild=0;state.freezeStart=tick;state.freezeUntil=tick+frames(c.freezeDuration);state.freezeReady=tick+frames(c.freezeCD);state.freezeID++;
      freezeEvents.push({time:tick/F,id:part.id,freezeID:state.freezeID});
    }
  }
  function settle() {
    for(let i=0;i<events.length;) {
      const e=events[i];if(e.at>tick){i++;continue;}events.splice(i,1);
      if(e.type==='hit') {
        const hits=e.key==='ice'?[yes(p.ice),yes(p.ice),yes(p.ice)].filter(Boolean).length:Number(yes(p[e.key]));
        if(!hits)continue;
        if(takeDamage(e.part,e.key,e.damage*(e.key==='ice'?hits/3:1),hits)) {
          if(e.key==='water'){e.part.wetUntil=tick+frames(5);discount(e.part,c.waterDiscount);}
          if(e.key==='ball'){e.part.wetUntil=tick+frames(15);discount(e.part,c.ballDiscount);}
          if(e.key==='ice')iceHit(e,hits);
        }
      } else if(e.type==='lock') {
        const state=frost(e.part);
        const remaining=e.part.hp>0?Math.max(0,(state.freezeUntil-tick)/F):0;
        const damage=(c.lockBaseB+c.lockPerSecondB*remaining)*scale;
        lockEvents.push({time:tick/F,id:e.part.id,remaining,freezeID:remaining>0?state.freezeID:null});
        if(remaining>0)state.freezeUntil=tick;
        events.push({type:'hit',key:'lock',part:e.part,at:tick+frames(c.lockDelay),damage});
      } else if(e.type==='cloud') {
        const log={start:tick/F,end:null,reason:null,paidTicks:0};clouds.push(log);
        cloud={end:tick+frames(c.thunderDuration),nextHit:tick+30,log};
      }
    }
  }
  function cast(key,cost,seconds,part) {
    assert(pay(cost,key));records[key].count++;busy=tick+frames(seconds);return busy;
  }
  function dodge(){return p.dodgeEvery>0&&tick>=frames(p.dodgeEvery)&&(tick%frames(p.dodgeEvery))<frames(p.dodgeSeconds);}
  for(tick=0;tick<frames(o.maxSeconds);tick++) {
    for(const part of parts) {
      if(part.id==='core'||part.hp<=0||tick<60)continue;
      if(tick>=frost(part).freezeUntil) {
        const goal=animationGoal(part.id,part.aiTime);
        part.closedFrame+=Math.sign(goal-part.closedFrame);part.aiTime++;
      }
    }
    if(coreOpened===null&&parts.filter(x=>x.id!=='core').every(x=>x.hp<=0))coreOpened=tick+1;
    if(p.hurtEvery>0&&tick>0&&tick%frames(p.hurtEvery)===0) {
      // Max life500 -> life300 and lost200: barrier absorbs up to40% of
      // incoming damage AFTER ordinary defense, pays current mana only, no auto potion.
      const requested=p.hurtDamage*.4,paid=Math.min(mana,requested);mana-=paid;hurtPaid+=paid;healthDamage+=p.hurtDamage-paid;
      health-=p.hurtDamage-paid;
      hurtEvents.push({time:tick/F,incoming:p.hurtDamage,requested,paid,health:p.hurtDamage-paid});
    }
    if(o.simulateSurvival&&health<=0){playerDead=true;elapsed=tick+1;break;}
    if(health<=150&&tick>=healthPotionReady&&o.healthPotionHeal>0){health=Math.min(300,health+o.healthPotionHeal);healthPotions++;healthPotionReady=tick+frames(o.healthPotionCD);}
    settle();
    target=chooseTarget();
    if(cloud&&tick>=cloud.end){cloud.log.end=tick/F;cloud.log.reason='complete';cloud=null;}
    if(cloud) {
      if(pay(c.thunderDrain/F,'thunder')) {
        cloudTicks++;cloud.log.paidTicks++;
        if(tick>=cloud.nextHit) {
          const open=parts.filter(isOpen);const wetTarget=open.filter(wet).sort((a,b)=>a.hp-b.hp)[0];
          const hitTarget=wetTarget||target;
          if(hitTarget&&yes(p.thunder*(wet(hitTarget)?1:.5)))takeDamage(hitTarget,'thunder',c.thunderB*scale/(c.thunderDuration*2));
          cloud.nextHit+=30;
        }
      } else {cloud.log.end=tick/F;cloud.log.reason='mana';cloud=null;}
    }
    if(parts.find(x=>x.id==='core').hp<=0){killed=true;elapsed=tick+1;break;}
    if(tick>=busy&&target&&!dodge()) {
      const state=frost(target);
      const remaining=(state.freezeUntil-tick)/F;
      let acted=false,hold=false;
      if(tick>=lockReady&&remaining>c.lockCast&&tick>=state.freezeStart+frames(p.react)) {
        if(canPay(c.lockCost)){const end=cast('lock',c.lockCost,c.lockCast,target);events.push({type:'lock',part:target,at:end});lockReady=tick+frames(c.lockCD);acted=true;}
        else hold=true;
      }
      if(!acted&&!hold&&tick>=state.freezeReady&&tick>=state.freezeUntil) {
        if(canPay(4.5)) {const end=cast('ice',4.5,.5,target);events.push({type:'hit',key:'ice',part:target,at:end,damage:c.iceRatio*.5*scale,buildEligible:true});acted=true;}
        else hold=true;
      }
      if(!acted&&!hold&&o.allowThunder&&tick>=thunderReady&&!cloud&&canPay(c.thunderDrain/F)) {
        const end=cast('thunder',0,c.thunderCast,target);events.push({type:'cloud',at:end+frames(c.thunderDelay)});thunderReady=tick+frames(c.thunderCD);acted=true;
      }
      if(!acted&&!hold&&tick>=ballReady&&canPay(c.ballCost)) {
        const end=cast('ball',c.ballCost,c.ballCast,target);events.push({type:'hit',key:'ball',part:target,at:end,damage:c.ballB*scale});ballReady=tick+frames(c.ballCD);acted=true;
      }
      if(!acted&&!hold&&canPay(1.5)) {
        const end=cast('water',1.5,.2,target);events.push({type:'hit',key:'water',part:target,at:end,damage:c.waterRatio*.2*scale});
      }
    }
    const gain=Math.min(M-mana,regen/F);mana+=gain;regenEffective+=gain;regenWaste+=regen/F-gain;
    health=Math.min(300,health+o.healthRegen/F);
    if(tick%60===0)manaSamples.push({time:tick/F,mana,potions,totalDamage:Object.values(records).reduce((n,r)=>n+r.damage,0)});
    elapsed=tick+1;
  }
  if(cloud){cloud.log.end=elapsed/F;cloud.log.reason=killed?'kill':'horizon';}
  const totalDamage=Object.values(records).reduce((n,r)=>n+r.damage,0);
  assert(Math.abs(M+regenEffective+potionMana-spent-hurtPaid-mana)<1e-5,'mana ledger');
  if(killed)assert(Math.abs(totalDamage-totalHP)<1e-5,'credit only HP, no overkill');
  assert(Object.values(records).every(x=>x.damage>=0));
  if(o.sharedFreeze) {
    const consumed=lockEvents.filter(x=>x.freezeID!==null).map(x=>x.freezeID);
    assert.equal(new Set(consumed).size,consumed.length,'each global freeze consumed once');
    for(let i=1;i<freezeEvents.length;i++){
      const previous=freezeEvents[i-1],current=freezeEvents[i];
      const reduced=cooldownEvents.filter(x=>x.freezeID===previous.freezeID).reduce((n,x)=>n+x.seconds,0);
      assert(current.time-previous.time+reduced+EPS>=c.freezeCD,'target change cannot reset global cooldown');
    }
  }
  for(const r of Object.values(records))r.share=totalDamage>0?r.damage/totalDamage:0;
  return {options:o,candidate:c,killed,playerDead,time:elapsed/F,coreOpened:coreOpened===null?null:coreOpened/F,totalDamage,totalHP,
    B:s.baseDps,maxMana:M,regen,gearMultiplier:b.e.multiplier,records,usedPotions:o.potions-potions,endMana:mana,
    hurtPaid,healthDamage,endHealth:health,healthPotions,ledger:{initial:M,regenEffective,potionMana,spent,hurtPaid,end:mana},
    stageDeaths:partDeaths,freezeEvents,lockEvents,cooldownEvents,finalFrost:o.sharedFreeze?{...sharedFrost}:null,
    clouds,cloudSeconds:cloudTicks/F,damagePerPart,manaSamples};
}
module.exports={candidate,profiles,bossStats,totalHP,animationGoal,run,gear};
if(require.main===module) {
  for(const profile of ['ideal','normal','heavy'])console.log(JSON.stringify(run({profile}),null,2));
}
