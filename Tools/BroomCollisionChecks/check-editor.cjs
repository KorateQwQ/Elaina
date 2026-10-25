const fs=require('node:fs'),path=require('node:path'),vm=require('node:vm'),assert=require('node:assert/strict');
const root=path.resolve(__dirname,'../..');
const html=fs.readFileSync(path.join(root,'Tools/MageBalance/index.html'),'utf8');
const context={module:{exports:{}}};vm.createContext(context);
vm.runInContext(html.match(/<script id="balance-engine">([\s\S]*?)<\/script>/)[1],context);
context.Balance=context.module.exports;
for(const name of ['validSaved','keepKnown']){
 const match=html.match(new RegExp('function '+name+'\\([^]*?\\n\\}'));
 assert.ok(match,name);vm.runInContext(match[0],context);
}
const config=JSON.parse(fs.readFileSync(path.join(root,'ElainaModSkills/Data/SkillBalance.json'),'utf8')).configuration;
assert.equal(context.validSaved(config),true,'Current configuration must load in editor');
const restored=context.keepKnown(context.Balance.defaults(),config);
assert.equal(JSON.stringify(restored.skills.find(r=>r.id==='FlightSkill').flight),JSON.stringify(config.skills.find(r=>r.id==='FlightSkill').flight),'Editor load/save must preserve every flight parameter');
for(const [key,value] of [['dashCooldownSeconds',0],['dashCooldownSeconds',.1],['dashManaCost',-1],['boostManaPerSecond',true],['baseSpeed',0],['handlingPercent',0],['handlingPercent',101],['dashRecoverySeconds',0],['dashRecoverySeconds',.6]]){
 const bad=structuredClone(config);bad.skills.find(r=>r.id==='FlightSkill').flight[key]=value;
 assert.equal(context.validSaved(bad),false,key);
}
const legacy=structuredClone(config);delete legacy.skills.find(r=>r.id==='FlightSkill').flight;
assert.equal(context.validSaved(legacy),true,'Older configurations remain readable');
console.log('PASS: 12 flight editor validation and round-trip checks.');

