// Isolated stage-unlock checks: real config parser and exact condition snippets; no game process.
'use strict';
const fs=require('node:fs'),os=require('node:os'),path=require('node:path');
const assert=require('node:assert/strict');
const {spawnSync}=require('node:child_process');
const root=path.resolve(__dirname,'../..');
const skills=[['Ice/IceConeSkill',25],['Wind/WindSlashSkill',20],['Water/WaterLaserSKill',30],['Fire/FireLaserSkill',40],['Heal/HealSkill',25]];
const base=fs.readFileSync(path.join(root,'ElainaModSkills/ElainaSkill.cs'),'utf8');
const helper=base.match(/    protected static SkillUnlockCondition RequireConfiguredCharacterLevelCap[\s\S]*?(?=    \/\/\/ <summary>)/)?.[0];
assert.ok(helper,'Configured cap helper is missing');
const declarations=skills.map(([file,fallback])=>{
 const source=fs.readFileSync(path.join(root,`ElainaModSkills/Skills/${file}.cs`),'utf8');
 const condition=source.match(/public override SkillUnlockCondition UnlockCondition \{ get; set; \} =\s*new CompositeSkillUnlockCondition\((?:DefaultUnlockCost|SkillUnlockCondition\.BySkillPoint\(\d+\)), RequireConfiguredCharacterLevelCap\(nameof\((\w+)\), (\d+)\)[\s\S]*?;/);
 assert.ok(condition,`${file} must combine default cost with configured stage cap`);
 const name=file.split('/').at(-1);
 assert.equal(condition[1],name,'Each condition must use its own JSON ID');
 assert.equal(Number(condition[2]),fallback,'Fallback stage changed');
 return `public sealed class ${name} : UnlockBase { ${condition[0]} }`;
}).join('\n');
const scratch=fs.mkdtempSync(path.join(os.tmpdir(),'mage-balance-unlocks-'));
const xml=s=>s.replaceAll('&','&amp;').replaceAll('"','&quot;').replaceAll('<','&lt;');
try{
 fs.writeFileSync(path.join(scratch,'Checks.csproj'),`<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup><Compile Include="${xml(path.join(root,'ElainaModSkills/SkillBalanceConfig.cs'))}" Link="SkillBalanceConfig.cs" /><Compile Include="${xml(path.join(root,'ElainaModSkills/SkillStageGate.cs'))}" Link="SkillStageGate.cs" /></ItemGroup></Project>`);
 fs.writeFileSync(path.join(scratch,'Program.cs'),String.raw`
using System.Text.Json.Nodes;
using KL.SkillSystem;
using Terraria.ID;
using Terraria.Localization;
using 伊蕾娜.ElainaModSkills;
var json=File.ReadAllText(args[0]);
void Apply(string text){if(!SkillBalanceConfig.TryParse(text,out var config,out var error))throw new Exception(error);SkillBalanceSystem.Current=config;}
int passed=0;
void Check(bool condition,string message){if(!condition)throw new Exception(message);passed++;Console.WriteLine("PASS "+message);}
Apply(json);
foreach(var (skill,fallback) in new (UnlockBase,int)[]{(new IceConeSkill(),25),(new WindSlashSkill(),20),(new WaterLaserSKill(),30),(new FireLaserSkill(),40),(new HealSkill(),25)}){
 Apply(json);
 string id=skill.GetType().Name;
 int cap=SkillBalanceSystem.Current.Skills[id].UnlockStageCap;
 var player=new KLSkillModPlayer();
 var condition=skill.UnlockCondition;
 player.Player.State.Cap=cap-1;player.Player.State.Level=1000;
 Check(!condition.CanUnlock(player,skill),id+": stage cap below threshold blocks unlock");
 player.Player.State.Cap=cap;player.Player.State.Level=1;
 Check(condition.CanUnlock(player,skill),id+": stage cap at threshold unlocks even at character level 1");
 Check(condition.GetDescription(player,skill).Contains("Lv."+cap),id+": description uses configured cap");
 player.HasCost=false;
 Check(!condition.CanUnlock(player,skill),id+": stage readiness does not remove research point cost");
 player.HasCost=true;
 if(id=="IceConeSkill"){
  player.HasFrostCore=false;
  Check(!condition.CanUnlock(player,skill),"Ice Cone requires a Frost Core even with stage and default cost ready");
  player.HasFrostCore=true;
 }
 Check(!condition.CanUnlock(null,skill),id+": null player is safe");
 var payload=JsonNode.Parse(json);
 var row=payload["configuration"][id=="HealSkill"?"skills":"basicAttacks"].AsArray().First(x=>x["id"].GetValue<string>()==id);
 row["unlockStageCap"]=cap+5;Apply(payload.ToJsonString());
 Check(!condition.CanUnlock(player,skill)&&condition.GetDescription(player,skill).Contains("Lv."+(cap+5)),id+": existing rule and description read updated JSON");
 player.Player.State.Cap=cap+5;
 Check(condition.CanUnlock(player,skill),id+": updated threshold unlocks correctly");
 if(id=="HealSkill"){
  row["unlockLevel"]=31;Apply(payload.ToJsonString());
  Check(condition.CanUnlock(player,skill)&&!condition.GetDescription(player,skill).Contains("Lv.31"),
   "Healing growth origin does not impose an actual-level learning requirement");
 }
 SkillBalanceSystem.Current=SkillBalanceConfig.Fallback;
 player.Player.State.Cap=fallback;
 Check(condition.CanUnlock(player,skill)&&condition.GetDescription(player,skill).Contains("Lv."+fallback),id+": missing JSON entry uses fallback stage");
}
Console.WriteLine($"{passed} stage-unlock checks passed.");
public abstract class UnlockBase : ModSkill {
 public virtual SkillUnlockCondition UnlockCondition {get;set;}
 private static int CharacterLevelCap(KLSkillModPlayer p)=>p?.Player.State.GetMaxLevel()??0;
 protected static SkillUnlockCondition DefaultUnlockCost=>SkillUnlockCondition.Custom((p,_)=>p?.HasCost==true,description:(_,_)=>"cost");
`+helper+String.raw`
}
`+declarations+String.raw`
namespace 伊蕾娜.ElainaModSkills {
 public static class SkillBalanceSystem {
  public static SkillBalanceConfig Current;
  public static bool TryGetSkill(string id,out SkillBalanceEntry entry)=>Current.Skills.TryGetValue(id,out entry);
 }
 public sealed class ElainaStatePlayer {
  public int Cap,Level;
  public int GetMaxLevel()=>Cap;
  public int GetLevel()=>throw new Exception("Stage unlock must not query actual character level");
 }
}
namespace KL.SkillSystem {
 public class ModSkill {}
 public sealed class Player {
  public ElainaStatePlayer State=new();
  public T GetModPlayer<T>()=>(T)(object)State;
 }
 public sealed class KLSkillModPlayer {public Player Player=new();public bool HasCost=true,HasFrostCore=true;}
 public readonly record struct SkillUnlockItem(int ItemType,int Stack);
 public class SkillUnlockCondition {
  private Func<KLSkillModPlayer,ModSkill,bool> predicate;
  private Func<KLSkillModPlayer,ModSkill,string> describe;
  public static SkillUnlockCondition Custom(Func<KLSkillModPlayer,ModSkill,bool> canUnlock,Func<KLSkillModPlayer,ModSkill,string> description)=>new(){predicate=canUnlock,describe=description};
  public static SkillUnlockCondition BySkillPoint(int amount)=>Custom((p,_)=>p?.HasCost==true,(_,_)=>"cost "+amount);
  public static SkillUnlockCondition ByItems(params SkillUnlockItem[] items){
   if(items.Length!=1||items[0].ItemType!=ItemID.FrostCore||items[0].Stack!=1)throw new Exception("Expected exactly one Frost Core");
   return Custom((p,_)=>p?.HasFrostCore==true,(_,_)=>"Frost Core x1");
  }
  public virtual bool CanUnlock(KLSkillModPlayer player,ModSkill skill)=>predicate(player,skill);
  public virtual string GetDescription(KLSkillModPlayer player,ModSkill skill)=>describe(player,skill);
 }
 public sealed class CompositeSkillUnlockCondition : SkillUnlockCondition {
  private readonly SkillUnlockCondition[] conditions;
  public IReadOnlyList<SkillUnlockCondition> Conditions=>conditions;
  public CompositeSkillUnlockCondition(params SkillUnlockCondition[] rules){conditions=rules;}
  public override bool CanUnlock(KLSkillModPlayer player,ModSkill skill)=>conditions.All(c=>c.CanUnlock(player,skill));
  public override string GetDescription(KLSkillModPlayer player,ModSkill skill)=>string.Join(" / ",conditions.Select(c=>c.GetDescription(player,skill)));
 }
}
namespace Terraria.Localization {
 public static class Language {
  public static string GetTextValue(string key,params object[] args){
   if(key!="Mods.伊蕾娜.SkillInfo.UnlockConditions.CharacterLevelCap"&&key!="Mods.伊蕾娜.SkillInfo.UnlockConditions.CharacterLevel")throw new Exception("Wrong localization key");
   return string.Format("角色等级上限达到 Lv.{0}",args);
  }
 }
}
namespace Terraria.ID {public static class ItemID {public const int FrostCore=2161;}}
`);
 const result=spawnSync('dotnet',['run','--project',path.join(scratch,'Checks.csproj'),'--',path.join(root,'ElainaModSkills/Data/SkillBalance.json')],{stdio:'inherit'});
 if(result.error)throw result.error;
 assert.equal(result.status,0,'Stage-unlock checks failed');
}finally{
 const resolved=fs.realpathSync(scratch);
 assert.equal(path.dirname(resolved),fs.realpathSync(os.tmpdir()));
 assert.ok(path.basename(resolved).startsWith('mage-balance-unlocks-'));
 fs.rmSync(resolved,{recursive:true,force:true});
}
