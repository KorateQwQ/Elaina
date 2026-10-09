'use strict';
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const vm = require('node:vm');
const assert = require('node:assert/strict');
const {spawnSync} = require('node:child_process');

const root = path.resolve(__dirname, '../..');
const html = fs.readFileSync(path.join(__dirname, 'index.html'), 'utf8');
const ctx = {module:{exports:{}}};
vm.runInNewContext(html.match(/<script id="balance-engine">([\s\S]*?)<\/script>/)[1], ctx);
const B = ctx.module.exports;
function fixture(s) {
 const phaseSize = s.progression.phaseSize;
 const expected = [...s.basicAttacks,...s.skills].flatMap(row => B.progressionNodes(row,phaseSize).slice(0,20).map((characterLevel,i) => {
  const r = B.progressionBudget(s,row,characterLevel);
  return {id:row.id,skillLevel:i+1,characterLevel,dps:r.dps,total:r.total,hit:r.integerHit};
 }));
 return {json:JSON.stringify({schema:'elaina-mage-balance-v2',configuration:s}),expected};
}
const cases = [1,2,5,7,20].map(phaseSize => {
 const s = B.defaults();
 s.progression.phaseSize = phaseSize;
 s.basicAttacks.push({...B.newBasic('IceConeSkill','Ice'),unlockStageCap:10,unlockLevel:6,levelsPerPhase:2,ratio:1.1,interval:.5,shots:3});
 s.basicAttacks.push({...B.newBasic('DenseSkill','Dense'),unlockStageCap:3,unlockLevel:10,levelsPerPhase:10});
 return fixture(s);
});
cases.push(fixture(JSON.parse(fs.readFileSync(path.join(root,'ElainaModSkills/Data/SkillBalance.json'),'utf8').replace(/^\uFEFF/, '')).configuration));

const parent = fs.readFileSync(path.join(root,'ElainaModSkills/ElainaSkill.cs'),'utf8');
assert.match(parent,/public override int MaxLevel => GetConfiguredMaxLevel\(\)/);
assert.match(parent,/GetPlayerDamage\(Player, BalanceId, skillLevel \?\? Math.Max\(1, Level\)/);
assert.match(parent,/TryGetPlayerBalance\(Player, BalanceId, skillLevel \?\? Math.Max\(1, Level\)/);
assert.doesNotMatch(fs.readFileSync(path.join(root,'ElainaModSkills/Skills/MagicMissile/MagicMissileSkill.cs'),'utf8'),/override int MaxLevel/);
console.log('PASS skill instances inherit the configured cap and forward their actual learned level');

const scratch = fs.mkdtempSync(path.join(os.tmpdir(),'mage-balance-runtime-'));
function xml(value) {return value.replaceAll('&','&amp;').replaceAll('"','&quot;').replaceAll('<','&lt;');}
try {
 const sources = ['ElainaModSkills/SkillBalanceConfig.cs','ElainaModSkills/SkillBalanceSystem.cs','../KL/Utils/KLDpsHelper.cs'];
 fs.writeFileSync(path.join(scratch,'Checks.csproj'),`<Project Sdk="Microsoft.NET.Sdk">
 <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><LangVersion>latest</LangVersion></PropertyGroup>
 <ItemGroup>${sources.map(file=>`<Compile Include="${xml(path.resolve(root,file))}" Link="${path.basename(file)}" />`).join('')}</ItemGroup>
</Project>`);
 fs.writeFileSync(path.join(scratch,'fixtures.json'),JSON.stringify(cases));
 fs.writeFileSync(path.join(scratch,'Program.cs'),String.raw`
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using KL.Utils;
using Terraria;
using Terraria.ModLoader;
using 伊蕾娜.ElainaModSkills;

var fixtures = JsonDocument.Parse(File.ReadAllText(args[0])).RootElement;
Mod.Json = fixtures[0].GetProperty("json").GetString();
new SkillBalanceSystem().Load();
var store = (SkillBalanceFileStore)typeof(SkillBalanceSystem).GetField("store", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
int passed = 0;
void Check(bool condition, string message) {if (!condition) throw new Exception(message);}
void Near(double a, double b) => Check(Math.Abs(a-b) < 1e-5 * Math.Max(1, Math.Abs(b)), $"{a} != {b}");
void Test(string name, Action body) {body(); passed++; Console.WriteLine("PASS " + name);}
foreach (var fixture in fixtures.EnumerateArray())
{
    string json = fixture.GetProperty("json").GetString();
    store.Apply(json);
    var config = SkillBalanceSystem.Current;
    Test($"C# matches HTML skill nodes, caps, DPS and per-hit damage (phase {config.CharacterPhaseSize})", () =>
    {
        foreach (var row in fixture.GetProperty("expected").EnumerateArray())
        {
            string id = row.GetProperty("id").GetString();
            int level = row.GetProperty("skillLevel").GetInt32(), node = row.GetProperty("characterLevel").GetInt32();
            var entry = config.Skills[id];
            Check(config.GetSkillCharacterLevel(entry, level) == node, "Incorrect mapped character level");
            Check(config.GetSkillLevelCap(entry, node) == level, "Incorrect cap at upgrade gate");
            Check(config.GetSkillLevelCap(entry, node-1) == level-1, "Cap advanced before gate");
            Near(SkillBalanceSystem.GetBaseDps(level, id), row.GetProperty("dps").GetDouble());
            Near(config.GetSkillDamage(KLDpsHelper.GetLevelDps(node), entry), row.GetProperty("total").GetDouble());
            int hit = SkillBalanceSystem.GetBaseDamage(level, id), expectedHit = row.GetProperty("hit").GetInt32();
            Check(hit == expectedHit, $"Incorrect per-hit rounding: {id} Lv{level} node{node}, got {hit}, expected {expectedHit}; baseline={KLDpsHelper.GetLevelDps(node)} total={config.GetSkillDamage(KLDpsHelper.GetLevelDps(node), entry):R}");
        }
    });
}
store.Apply(fixtures[2].GetProperty("json").GetString());
var current = SkillBalanceSystem.Current;
var missile = current.Skills["MagicMissileSkill"];
var ice = current.Skills["IceConeSkill"];
var player = new Player();
var damageClass = new DamageClass();
Test("Delayed unlock maps Ice Lv1 to character 6, not character 1", () =>
{
    Check(current.GetSkillCharacterLevel(ice,1) == 6, "Wrong Ice Lv1 gate");
    Near(SkillBalanceSystem.GetBaseDps(1,ice.Id), KLDpsHelper.GetLevelDps(6)*ice.DpsRatio);
});
Test("No learned level yields no raw DPS or damage", () =>
{
    foreach (int level in new[]{0,-1,int.MinValue})
    {
        Check(current.GetSkillCharacterLevel(missile,level) == 0, "Nonpositive level has a node");
        Near(SkillBalanceSystem.GetBaseDps(level,missile.Id),0);
        Check(SkillBalanceSystem.GetBaseDamage(level,missile.Id) == 0, "Nonpositive level has damage");
        Check(!SkillBalanceSystem.TryGetPlayerBalance(player,missile.Id,level,damageClass,out _), "Nonpositive level has a cast result");
    }
});
Test("Player damage uses the learned skill level and never reads player progression", () =>
{
    Check(SkillBalanceSystem.TryGetPlayerBalance(player,missile.Id,2,damageClass,out var result), "Missing cast result");
    Check(result.SkillLevel == 2 && result.CharacterLevel == 3, "Wrong cast node");
    Near(result.BaseDps,SkillBalanceSystem.GetBaseDps(2,missile.Id));
    Check(result.DamagePerHit == SkillBalanceSystem.GetBaseDamage(2,missile.Id), "Raw and player paths disagree");
});
Test("Equipment damage is applied exactly once before hit splitting and rounding", () =>
{
    player.DamageMultiplier = 1.5f;
    Check(SkillBalanceSystem.TryGetPlayerBalance(player,ice.Id,20,damageClass,out var result,7), "Missing Ice result");
    Near(result.CastDamage,result.BaseCastDamage*1.5);
    Check(result.HitCount == 7 && result.DamagePerHit == SkillBalanceConfig.ToHitDamage(result.CastDamage,7), "Incorrect hit split");
    Check(SkillBalanceSystem.GetPlayerDamage(player,ice.Id,20,damageClass,7) == result.DamagePerHit, "Convenience API disagrees");
    player.DamageMultiplier = 1;
});
Test("Active skill pricing maps its own level, including occupancy compensation", () =>
{
    var entry = current.Skills["WaterBallSkill"];
    Check(SkillBalanceSystem.TryGetPlayerBalance(player,entry.Id,3,damageClass,out var result), "Missing active result");
    int node = current.GetSkillCharacterLevel(entry,3);
    Near(result.BaseCastDamage,current.GetSkillDamage(KLDpsHelper.GetLevelDps(node),entry));
    Check(result.CharacterLevel == node, "Active uses a basic attack gate");
});
Test("Changing player cap does not truncate an already learned damage level", () =>
{
    Check(current.GetSkillLevelCap(missile,1) == 1, "Unexpected early cap");
    Check(SkillBalanceSystem.TryGetPlayerBalance(player,missile.Id,5,damageClass,out var result), "Missing saved level result");
    Check(result.SkillLevel == 5 && result.CharacterLevel == 10, "Saved level was truncated");
});
Test("High saved levels saturate at the last representable node", () =>
{
    int last = current.GetSkillLevelNodes(ice).Last();
    Check(current.GetSkillCharacterLevel(ice,int.MaxValue) == last, "High skill level did not saturate");
    Check(current.GetSkillCharacterLevel(ice with {UnlockLevel=1000},2) == 1000, "Terminal unlock is incorrect");
});
Test("Missing IDs and invalid hit counts retain safe behavior", () =>
{
    Near(SkillBalanceSystem.GetBaseDps(1,"MissingSkill"),0);
    Check(SkillBalanceSystem.GetBaseDamage(1,"MissingSkill") == 0, "Missing ID has damage");
    Check(!SkillBalanceSystem.TryGetPlayerBalance(player,"MissingSkill",1,damageClass,out _), "Missing ID has result");
    bool threw = false;
    try {SkillBalanceSystem.GetPlayerDamage(player,missile.Id,1,damageClass,0);}
    catch (ArgumentOutOfRangeException) {threw=true;}
    Check(threw, "Invalid hit count was accepted");
});
Test("Hot configuration changes update both level caps and damage mapping without caching", () =>
{
    var oldConfig = SkillBalanceSystem.Current;
    double oldDps = SkillBalanceSystem.GetBaseDps(2,missile.Id);
    var payload = JsonNode.Parse(fixtures[2].GetProperty("json").GetString());
    var row = payload["configuration"]["basicAttacks"][0];
    row["unlockLevel"] = 2;
    row["levelsPerPhase"] = 1;
    row["ratio"] = .4;
    Check(store.Apply(payload.ToJsonString()), "Live configuration was not accepted");
    var next = SkillBalanceSystem.Current;
    var entry = next.Skills[missile.Id];
    Check(next.GetSkillCharacterLevel(entry,2) == 5, "Stale mapped node");
    Check(next.GetSkillLevelCap(entry,3) == 1, "Stale cap");
    Near(SkillBalanceSystem.GetBaseDps(2,entry.Id),KLDpsHelper.GetLevelDps(5)*.4);
    Check(Math.Abs(SkillBalanceSystem.GetBaseDps(2,entry.Id)-oldDps)>1, "Stale damage");
    Check(oldConfig.GetSkillCharacterLevel(missile,2) == 3, "Previous immutable snapshot changed");
    Check(!store.Apply("{}") && ReferenceEquals(next,SkillBalanceSystem.Current), "Bad JSON replaced valid snapshot");
});
Console.WriteLine($"{passed} C# runtime tests passed.");

namespace Microsoft.Xna.Framework {public class GameTime {}}
namespace Terraria
{
    public class Player
    {
        public float DamageMultiplier = 1;
        public StatModifier GetTotalDamage(DamageClass damageClass) => new(DamageMultiplier);
        public T GetModPlayer<T>() => throw new Exception("Damage must not query player progression");
    }
    public readonly record struct StatModifier(float Multiplier) {public float ApplyTo(float value) => value*Multiplier;}
    public static class Main
    {
        public static bool gameMenu=true, dedServ;
        public static void NewText(string text,int r,int g,int b) {}
    }
}
namespace Terraria.ModLoader
{
    public class DamageClass {}
    public class Mod
    {
        public static string Json;
        public string SourceFolder => null;
        public bool FileExists(string path) => true;
        public byte[] GetFileBytes(string path) => System.Text.Encoding.UTF8.GetBytes(Json);
        public Logger Logger {get;} = new();
    }
    public class Logger {public void Warn(string text) {} public void Info(string text) {}}
    public abstract class ModSystem
    {
        public Mod Mod {get;} = new();
        public virtual void Load() {} public virtual void Unload() {}
        public virtual void UpdateUI(Microsoft.Xna.Framework.GameTime gameTime) {}
        public virtual void PostUpdateEverything() {}
    }
}
`);
 for (const configuration of ['Debug','Release']) {
  console.log(`Checking ${configuration} C# runtime paths`);
  const result = spawnSync('dotnet',['run','--project',path.join(scratch,'Checks.csproj'),'-c',configuration,'--',path.join(scratch,'fixtures.json')],{stdio:'inherit'});
  if (result.error) throw result.error;
  assert.equal(result.status,0,`${configuration} runtime checks failed`);
 }
} finally {
 const resolved = fs.realpathSync(scratch);
 assert.equal(path.dirname(resolved),fs.realpathSync(os.tmpdir()));
 assert.ok(path.basename(resolved).startsWith('mage-balance-runtime-'));
 fs.rmSync(resolved,{recursive:true,force:true});
}
