'use strict';
const assert=require('node:assert/strict');
const S=require('./simulate.cjs');
const left={id:'left',hp:10000},right={id:'right',hp:47000},head={id:'head',hp:80000};
const candidate=(part,safeImpact=true)=>({part,safeImpact,usefulDamage:Math.min(part.hp,74000),lethal:part.hp<=74000});
assert.equal(S.chooseLockTarget([candidate(left),candidate(right)]),right,
  'left-hand buildup can be detonated on the healthier right hand');
assert.equal(S.chooseLockTarget([candidate(left),candidate(right),candidate(head)]),head,
  'the shared frozen head can absorb the larger finisher budget');
assert.equal(S.chooseLockTarget([candidate(right),candidate(head,false)]),right,
  'prefer a target whose eye remains vulnerable at the known delayed impact');
assert.equal(S.chooseLockTarget([]),null);
const core={id:'core',hp:95000};
assert.equal(S.chooseLockTarget([candidate(core)]),core,'open core can receive the finisher');
const options={stage:18,allowThunder:false,preBuild:['ball'],regenPercent:2,healthRegen:2,healthPotionHeal:150,
  profile:'normal',tune:{ballB:6.75,lockPerSecondB:4.5}};
let cross=0;
for(const prefix of ['arcane','damage'])for(let seed=1;seed<=24;seed++) {
  const r=S.run({...options,prefix,seed,lockPolicy:'adaptive',buildPolicy:'spread'});
  assert(r.killed);
  assert(Math.abs(r.totalDamage-S.totalHP)<1e-5);
  assert.equal(r.records.thunder.count,0);
  for(const event of r.lockEvents) {
    const freeze=r.freezeEvents.find(f=>f.freezeID===event.freezeID);
    assert(freeze,'one existing shared freeze funds each lock');
    assert(event.remaining>0);
    if(event.id!==event.buildupTarget)cross++;
    if(event.id==='core')assert(event.castTime>=r.coreOpened,'locked core cannot be selected');
    assert(event.castTime>=freeze.time+.25-1e-7,'configured reaction latency preserved');
  }
  assert.equal(new Set(r.lockEvents.map(e=>e.freezeID)).size,r.lockEvents.length,'one detonation per global freeze');
}
assert(cross>0,'complete fights exercise cross-part detonation');
console.log(JSON.stringify({checks:6,integrationSamples:48,crossPartDetonations:cross}));
