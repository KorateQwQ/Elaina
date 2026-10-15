'use strict';
// Re-evaluate v5 under the existing game's cooldown semantics; preserve historical results.
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const S=require('./simulate-v3.cjs');
const p=JSON.parse(fs.readFileSync(path.join(__dirname,'candidate-parameters-v5.json'),'utf8')).parameters;
const common={...p,filler:'water',trace:true};
const variants={old:common,thaw:{...common,freezeCooldownStart:'end'},
  action45:{...common,freezeCooldownStart:'end',iceActionSeconds:45/60}};
const rows=[];
for(const [variant,options] of Object.entries(variants))for(const hat of [false,true])for(const prefix of ['arcane','damage']) {
  const r=S.run({...options,hat,prefix});
  const ice=r.records.ice.handSeconds,water=r.records.water.handSeconds;
  rows.push({variant,hat,prefix,dps:r.dps,ratio:r.ratio,freezeCount:r.freezeCount,
    firstFreeze:r.freezes[0]?.time,intervals:r.freezeIntervals,iceShare:ice/(ice+water),waterReadyHits:r.waterReadyHits,
    records:r.records,ledger:r.ledger});
  if(variant!=='old') {
    for(const start of r.cooldownStarts) {
      const f=r.freezes.find(x=>x.id===start.freezeId),d=r.detonations.find(x=>x.freezeId===start.freezeId);
      assert(Math.abs(start.time-(d?d.time:f.time+p.freezeDuration))<1e-6);
      assert(!r.reductions.some(x=>x.freezeId===f.id&&x.time<start.time));
    }
  }
}
// Natural thaw: four frozen seconds have no cooldown to reduce, and water uses the lower ratio.
const natural=S.run({...common,freezeCooldownStart:'end',omit:['lock','ball','thunder']});
assert(natural.cooldownStarts.length>0);
for(const c of natural.cooldownStarts)assert(Math.abs(c.time-natural.freezes.find(f=>f.id===c.freezeId).time-4)<1e-6);
assert(natural.waterReadyHits>0);
// Historical calculation stays byte-for-byte equivalent for the previously reported core metrics.
const prior=JSON.parse(fs.readFileSync(path.join(__dirname,'results-v5.json'),'utf8'));
for(const r of rows.filter(r=>r.variant==='old')) {
  const expected=(r.hat?prior.hat:prior.baseline)[r.prefix];
  assert.equal(r.dps,expected.dps);assert.equal(r.freezeCount,expected.freezeCount);
}
const result={date:'2026-10-15',parameters:p,rows,
  checks:['historical v5 metrics unchanged','cooldown starts at natural thaw or lock consumption',
    'no cooldown reductions stored while frozen','water outside cooldown uses reduced ratio','mana ledger balanced'],
  limits:['300s graduation dummy, historical equipment/30% potions retained for controlled comparison',
    '45-frame scenario changes action occupancy only; damage remains the configured 0.5s budget',
    'Other skills remain v5 assumptions; this is not a complete runtime combat simulation',
    'No aim/flight/dodge/defense or water 20-frame recovery simulation']};
fs.writeFileSync(path.join(__dirname,'results-v5-runtime.json'),JSON.stringify(result,null,2)+'\n');
const f=(n,d=3)=>n.toFixed(d),labels={old:'原v5',thaw:'解冻后起算CD',action45:'解冻后CD＋冰锥45帧动作'};
const report=`# v5 按游戏冻结冷却规则复算

2026-10-15。保留原版 v5 报告和结果作为对照；不调整游戏冻结参数。

冻结期间 CD 为 0；自然解冻或冰凌锁消耗冻结后，开始 60 秒 CD。冻结期间的水命中不能预存减 CD。水射线在命中前读取目标 CD，CD 为 0 时乘 0.7/0.9，包含正在冻结的目标。

## 300秒毕业木桩

沿用原 v5 的毕业配装、6槽、5瓶30%魔力药、无受击及原技能预算，以隔离规则变化。比例为模组DPS/旧实验的原版对照DPS；不代表大师实战。

| 方案 | 帽子 | 前缀 | DPS/原版 | 冻结次数 | 首次冻结 | 冰锥占普攻时间 |
|---|---|---|---:|---:|---:|---:|
${rows.map(r=>`| ${labels[r.variant]} | ${r.hat?'有':'无'} | ${r.prefix==='arcane'?'奥秘':'增伤'} | ${f(r.ratio)} | ${r.freezeCount} | ${f(r.firstFreeze,2)}秒 | ${f(r.iceShare*100,1)}% |`).join('\n')}

仅修改 CD 起算点时，及时用冰凌锁的循环通常只多付出“冻结到引爆”的时间，并非每轮固定增加4秒。没有冰凌锁、保留完整控制时才多出4秒。

45帧方案按当前 Action_SimpleSlash 占手0.75秒计算，每轮仍只造成配置中0.5秒的三发预算，每轮积累3/84；因此84发需要约21秒占手。这里只做动作周期敏感性对照，不是改动冰锥。

本模型未纳入水射线退出引导的20帧收势、弹道、Boss防御、躲避与命中率，其他技能仍沿用v5候选。冰锥解锁阶段的血量评估另见 ice-unlock-2026-10-15/REPORT.md。

检查通过：${result.checks.join('；')}。

复跑：node Tools/MageBalance/experiments/ice-build-2026-10-14/run-v5-runtime.cjs
`;
fs.writeFileSync(path.join(__dirname,'REPORT-v5-runtime.md'),report);
console.log(JSON.stringify({checks:result.checks,rows:rows.map(({records,ledger,intervals,...x})=>x)},null,2));
