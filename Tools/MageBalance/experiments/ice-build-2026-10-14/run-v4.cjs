'use strict';
const fs=require('node:fs'),path=require('node:path'),assert=require('node:assert/strict');
const S=require('./simulate-v4.cjs'),old=require('./simulate-v3.cjs');
const latest=JSON.parse(fs.readFileSync(path.join(__dirname,'candidate-water-contribution.json'),'utf8')).parameters;
const p=S.pair(S.recommended),hat=S.pair({...S.recommended,hat:true});
function stats(r){const water=r.records.water.handSeconds,ice=r.records.ice.handSeconds;return {water,ice,waterShare:water/(water+ice),iceShare:ice/(water+ice),damageShares:Object.fromEntries(Object.entries(r.records).map(([k,v])=>[k,v.damage/(r.dps*300)]))};}
const original=old.pair(latest);
const sustainedWater=S.pair({...S.recommended,filler:'water'});
const noWater=S.run({...S.recommended,omit:['water','ball','lock','thunder'],unlimitedMana:true});
assert(noWater.freezeIntervals.every(x=>Math.abs(x-63)<1e-6));
assert(stats(p.arcane).iceShare>.7&&stats(p.damage).iceShare>.65);
assert(p.arcane.dps>sustainedWater.arcane.dps&&p.damage.dps>sustainedWater.damage.dps);
assert(p.arcane.reductions.every(x=>!['waterState','ballState'].includes(x.kind)||x.seconds<=1/60+1e-7),'statuses never add their accelerations');
for(const r of [p.arcane,p.damage,hat.arcane,hat.damage]){
  assert(r.ledger.externalMana===0);assert(r.ratio>=1.5&&r.ratio<=2);
  assert(r.freezes.every(x=>x.iceSeconds>=3));assert(Math.abs(r.records.thunder.spent-r.thunderPaidSeconds*15)<1e-6);
}
const withoutThunder=S.pair({...S.recommended,omit:['thunder']});
assert(p.arcane.dps>withoutThunder.arcane.dps&&p.damage.dps>withoutThunder.damage.dps);
const ablations=['water','ball','ice','lock','thunder'].map(omit=>({omit,result:S.pair({...S.recommended,omit:[omit]})}));
const checks=['3s ice build still required after cooldown; no precharge',
  'Basic hand time: ice>70% arcane and>65% damage; remaining time includes costly active animations and actual waiting',
  'Sustained water filler loses to ice filler for both prefixes',
  'Wet effects select highest cooldown acceleration, never add them; repeated hits only refresh duration',
  'Formal runs conserve mana and hand time; thunder charged per paid frame; no external mana',
  '300s complete rotation within1.5-2x vanilla for both prefixes, with and without hat; thunder beats simply removing it'];
const result={date:'2026-10-14',duration:300,checks,parameters:{...S.defaults,...S.recommended},previous:original,
  baseline:p,hat,sustainedWater,ablations,timeStats:{previous:stats(original.arcane),arcane:stats(p.arcane),damage:stats(p.damage)}};
fs.writeFileSync(path.join(__dirname,'results-v4.json'),JSON.stringify(result,(k,v)=>['reductions','actions','damageEvents'].includes(k)?undefined:v,2)+'\n');
fs.writeFileSync(path.join(__dirname,'candidate-parameters-v4.json'),JSON.stringify({experimentOnly:true,description:'Latest proposed support water / main ice rotation; timed statuses replace old per-hit water reduction.',parameters:result.parameters},null,2)+'\n');
const f=(n,d=2)=>n.toFixed(d),a=p.arcane,d=p.damage;
const report=`# 冰锥主攻、水流辅助：300秒木桩

2026-10-14。高血量、单一目标，固定300秒，排除月总开闭眼和转阶段。正式模组和实验室页面未修改。

核心修正：不再要求冻结CD期间一直打水。改为短暂水流建立减CD状态，随后持续冰锥；潮湿将断时补水；冻结就绪后冰锥3秒有效积累，随即冰凌锁引爆。旧的“CD期水流本体始终比冰锥赚”要求，与3秒准备及冰锥时间占多数不相容，应改为“CD期补水值得，状态已建立后继续冰锥值得”。

## 配置

| 技能 | 耗蓝 | 周期 / CD | 伤害 | 联动 |
|---|---:|---:|---|---|
| 水流 | 每次1.5 | 0.2秒 | **0.55B DPS**；每次0.11B | 连续有效命中累计1秒（5次攻击）施加4秒弱潮湿；弱潮湿时冻结CD按**1.6倍自然速度**流逝 |
| 水球 | 150 | 15秒 | K1.8；8.1B总伤害；占手1.35秒 | 命中直接减CD4秒，并施加8秒强潮湿；强潮湿时CD按**2倍自然速度**流逝 |
| 冰锥 | 每次4.5 | 0.5秒 | **0.9B DPS**；三发合计0.45B，每发0.15B | CD期仍是主要持续伤害；CD就绪后每次完整三发贡献0.5秒，六次/18发/至少3秒触发冻结 |
| 冰凌锁 | 100 | 20秒 | 3.2B＋剩余冻结秒数×2.5B | 0.2秒施法结束消耗冻结，伤害再延迟1秒命中 |
| 天雷 | 15/秒 | 120秒 | 持续30秒，整次预算**48B** | 满程450蓝；离手攻击；潮湿目标完整锁定，断蓝提前终止 |

基础冻结CD60秒、冻结持续4秒、积累断档1.2秒清零、CD期间不预存积累。月总等关联NPC按整体共享CD/积累，本木桩只存在一个目标。无30秒硬下限。

弱、强潮湿的冷却加速**取较高值，不叠加**；重复水流只刷新持续时间，不能不断叠高加速倍率。水流原来的每次命中减0.16秒被这份状态效果替代，不能两者同时使用。水流累计施加状态也暂用1.2秒未命中则清零，不另加一个技能CD。

弱潮湿加速：真实1秒消掉1.6秒CD；强潮湿为2秒。水球直接减4秒另计。弱潮湿与强潮湿都算潮湿供天雷锁定。CD已就绪时水系不继续储存负冷却。

天雷从34B提高到48B，是本轮额外的伤害预算调整：采用冰锥主要输出后，原先34B/450蓝在增伤配装里可能不值得使用。现有rate4%、CD120秒、总耗蓝450的等效K约0.4444；付款仍需逐帧，不能一次性扣450蓝。水球K和冰凌锁公式保持上一版。

此数值是毕业基准；不代表已经完成前期倍率曲线。模拟中的水流、冰锥持续时长均是命中预算，未模拟真实弹道。

## 时间与输出

使用第一轮保存的毕业无帽配装：6槽、星云伤害强化3层，全奥秘魔力上限580，回蓝19.5/s；增伤上限460，回蓝16.5/s。5瓶30%药剂、满蓝开场、不受击、无星云特殊魔力回蓝桥接。

| 项目 | 旧全奥秘 | 新全奥秘 | 新全增伤 |
|---|---:|---:|---:|
| 水流占手时间 | ${f(result.timeStats.previous.water)}秒 | ${f(result.timeStats.arcane.water)}秒 | ${f(result.timeStats.damage.water)}秒 |
| 冰锥占手时间 | ${f(result.timeStats.previous.ice)}秒 | ${f(result.timeStats.arcane.ice)}秒 | ${f(result.timeStats.damage.ice)}秒 |
| 普攻时间中冰锥占比 | ${f(result.timeStats.previous.iceShare*100)}% | ${f(result.timeStats.arcane.iceShare*100)}% | ${f(result.timeStats.damage.iceShare*100)}% |

时间占比的分母仅为两种普攻占手时间，不含主动技能和等待。次数因两种普攻周期不同不能直接代替时间。

| 配装 | DPS | 相对原版 |
|---|---:|---:|
| 新全奥秘，无帽 | ${f(a.dps)} | ${f(a.ratio,3)}倍 |
| 新全增伤，无帽 | ${f(d.dps)} | ${f(d.ratio,3)}倍 |
| 新全奥秘，戴帽 | ${f(hat.arcane.dps)} | ${f(hat.arcane.ratio,3)}倍 |
| 新全增伤，戴帽 | ${f(hat.damage.dps)} | ${f(hat.damage.ratio,3)}倍 |

无帽同一参考打法奥秘领先${f(p.lead*100)}%，带帽奥秘差距${f(hat.lead*100)}%。这是特定打法对照，未重新穷举策略，不能宣称前缀全局优劣已经解决。

全奥秘冻结${a.freezeCount}次，间隔${a.freezeIntervals.map(x=>f(x)).join('、')}秒，含3秒积累；天雷实际付费持续${a.thunderPaidSeconds}秒。潮湿覆盖${f(a.wetCoverage*100)}%，施加与积累阶段可能存在短空档，没有免费假设100%常驻。

继续把普攻空档全部拿来打水时，奥秘${f(sustainedWater.arcane.ratio,3)}倍、增伤${f(sustainedWater.damage.ratio,3)}倍，均低于冰锥主攻的同一参考打法。

## 拆件与验证

| 移除技能，其他策略不变 | 奥秘 / 原版 | 增伤 / 原版 |
|---|---:|---:|
${ablations.map(x=>`| ${{water:'水流',ball:'水球',ice:'冰锥',lock:'冰凌锁',thunder:'天雷'}[x.omit]} | ${f(x.result.arcane.ratio,3)} | ${f(x.result.damage.ratio,3)} |`).join('\n')}

水流当前是小幅但正向的辅助收益，冰锥承担主体普攻时间；尚未用其他buff替换空槽或检验所有流派。

${checks.map((x,i)=>`${i+1}. ${x}`).join('\n')}

复跑：

\`\`\`powershell
node Tools/MageBalance/experiments/ice-build-2026-10-14/run-v4.cjs
\`\`\`

代码simulate-v4.cjs；候选参数candidate-parameters-v4.json；原始结果results-v4.json。真实命中、敌人移动、受击、护盾、前期成长与游戏接入仍未验证。
`;
fs.writeFileSync(path.join(__dirname,'REPORT-v4.md'),report);
console.log(JSON.stringify({checks,baseline:S.compact(p),hat:S.compact(hat),timeStats:result.timeStats,sustainedWater:S.compact(sustainedWater)},null,2));
