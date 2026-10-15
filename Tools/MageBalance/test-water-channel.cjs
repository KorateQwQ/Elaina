'use strict';
// Execute the channel action, hold state, shooting helper/node and wand Shoot hook in a headless host.
const fs=require('node:fs'),path=require('node:path'),os=require('node:os'),assert=require('node:assert/strict');
const {spawnSync}=require('node:child_process');
const root=path.resolve(__dirname,'../..');
const scratch=fs.mkdtempSync(path.join(process.argv[2]||os.tmpdir(),'elaina-water-tests-'));
function block(source,marker,from=0) {
 const start=source.indexOf(marker,from),brace=source.indexOf('{',start);
 assert(start>=0,marker);let end=brace+1,depth=1;
 for(;depth;end++){if(source[end]==='{')depth++;if(source[end]==='}')depth--;}
 return {text:source.slice(start,end),end};
}
const action=fs.readFileSync(path.resolve(root,'../KL/ActionsSystem/AnimAction.cs'),'utf8');
const firstShoot=block(action,'public static void ShootFromAction(');
const secondShoot=block(action,'public static void ShootFromAction(',firstShoot.end);
const sourceConstant=action.match(/public const string ShotSourceContext = [^;]+;/)?.[0]||'';
const shootNode=block(fs.readFileSync(path.resolve(root,'../KL/ActionsSystem/ActionNode.cs'),'utf8'),'public class ShootActionNode').text;
const wandShoot=block(fs.readFileSync(path.join(root,'Items/ElainaWand.cs'),'utf8'),'public override bool Shoot(').text;
fs.writeFileSync(path.join(scratch,'Shooting.cs'),`
namespace KL.ActionsSystem {
 public abstract partial class AnimAction {${sourceConstant}\n${firstShoot.text}\n${secondShoot.text}}
 ${shootNode}
}
namespace 伊蕾娜.Items {public class ElainaWand:WandBase {${wandShoot}}}
`);
const water=fs.readFileSync(path.join(root,'ElainaModSkills/Skills/Water/WaterLaser.cs'),'utf8');
const start=water.indexOf('public override void ModifyHitNPC('),brace=water.indexOf('{',start);
assert(start>=0);let end=brace+1,depth=1;
for(;depth;end++){if(water[end]==='{')depth++;if(water[end]==='}')depth--;}
const method=water.slice(start,end);
const constants=['SplashDamageMultiplier','OutsideFreezeCooldownDamageMultiplier'].map(name=>water.match(new RegExp('public const float '+name+' = [^;]+;'))[0]).join('\n');
const xml=s=>s.replaceAll('&','&amp;').replaceAll('"','&quot;');
const linked=[path.join(root,'ElainaActions/Action_WaterLaser.cs'),path.resolve(root,'../KL/ActionsSystem/ActionModPlayer.cs')];
fs.writeFileSync(path.join(scratch,'Tests.csproj'),`<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup>${linked.map(p=>`<Compile Include="${xml(p)}" Link="${path.basename(p)}"/>`).join('')}</ItemGroup></Project>`);
fs.writeFileSync(path.join(scratch,'Stubs.cs'),String.raw`
global using Terraria;
global using Vector2=System.Numerics.Vector2;
global using Terraria.ModLoader;
global using Terraria.DataStructures;
global using Terraria.ID;
global using KL.Utils;
global using KL.ActionsSystem;
namespace KL.Utils.Net {}
namespace KL.Utils {
 public class KLNetModule { public enum NetSendType { ClientToAll } }
 public static class GamePlayStatic {
  public static int Shots;
  public static Projectile NewProjectile(EntitySource_ItemUse_WithAmmo source,Vector2 position,Vector2 velocity,
   int type,int damage,float knockback,int owner,Action<Projectile> configureProjectile=null) {
   Shots++; var p=new Projectile();var beam=new 伊蕾娜.ElainaModSkills.Skills.Water.WaterLaser();
   p.ModProjectile=beam;beam.Projectile=p;configureProjectile?.Invoke(p);return p;
  }
 }
}
namespace KL.ActionsSystem.TemplateActions {}
public static class MathHelper { public const float Pi=MathF.PI; public static float WrapAngle(float x)=>x; }
public static class VectorExtensions {
 public static Vector2 ToRotationVector2(this float x)=>new(MathF.Cos(x),MathF.Sin(x));
}
namespace Terraria {
 public static class Main { public static int myPlayer=0; public static bool mouseLeft=true; public static Player LocalPlayer; }
 public class Item { public bool IsAir,ChangePlayerDirectionOnShoot; public int useTime,useAnimation; }
 public class Player {
  public enum CompositeArmStretchAmount { Full }
  public int whoAmI,direction=1,itemTime,itemAnimation; public float gravDir=1;
  public bool active=true,dead,CCed,noItems,mouseInterface; public Item HeldItem=new();
  private readonly Dictionary<Type,object> mods=new();
  public T GetModPlayer<T>() where T:new() {
   if(!mods.TryGetValue(typeof(T),out var value)){value=new T();mods[typeof(T)]=value;if(value is KLModPlayer p)p.Player=this;}
   return (T)value;
  }
  public void SetCompositeArmFront(bool b,CompositeArmStretchAmount s,float r){}
  public void ChangeDir(int d){direction=d;}
 }
 public class Projectile { public bool active=true; public object ModProjectile; }
 public class NPC { public int whoAmI,Cooldown; public NPC Anchor; public int GetFreezeCooldown()=>Anchor?.Cooldown??Cooldown;
  public struct HitModifiers { public float SourceDamage; }
 }
 public class KLModPlayer {
  public Player Player; public virtual void PostUpdate(){} public virtual void FrameEffects(){}
  public bool IsLeftClick()=>false;
  public void RPC(string name,object[] args,KL.Utils.KLNetModule.NetSendType type){}
 }
}
namespace Terraria.ID {public static class ProjectileID {public const int None=0;}}
namespace Terraria.DataStructures {
 public class EntitySource_ItemUse_WithAmmo(Player player,Item item,int ammo,string context=null) {public string Context=>context;}
}
namespace Terraria.ModLoader {
 public static class ModContent {public static int ProjectileType<T>()=>1;}
 public class WandBase {public virtual bool Shoot(Player player,EntitySource_ItemUse_WithAmmo source,Vector2 position,Vector2 velocity,int type,int damage,float knockback)=>true;}
 public static class CombinedHooks {
  public static bool CanShootAllowed=true,ShootAllowed=true;
  public static bool CanShoot(Player p,Item i)=>CanShootAllowed;
  public static void ModifyShootStats(Player p,Item i,ref Vector2 position,ref Vector2 velocity,ref int type,ref int damage,ref float knockback){}
  public static bool Shoot(Player p,Item i,EntitySource_ItemUse_WithAmmo s,Vector2 position,Vector2 velocity,int type,int damage,float knockback)
   =>ShootAllowed&&new 伊蕾娜.Items.ElainaWand().Shoot(p,s,position,velocity,type,damage,knockback);
 }
}
namespace KL.ActionsSystem {
 public abstract partial class AnimAction {
  public int TotalFrame{get;private set;} public int TypeId=>1; public float StartRotation{get;private set;}
  public virtual bool UseItemTime=>true; readonly List<ShootActionNode> nodes=new();
  protected AnimAction(int total){TotalFrame=total;} public void SetTotalFrame(int n){TotalFrame=n;}
  public void SetStartRotation(float r){StartRotation=r;} public void AddNode(ShootActionNode n){nodes.Add(n);}
  public bool IsFinished(int n)=>n>=TotalFrame; public float GetProgress(int n)=>n/(float)TotalFrame;
  public virtual void OnStart(ActionModPlayer p){} public virtual void Update(ActionModPlayer p,int f,float t){foreach(var n in nodes)if(n.StartFrame==f)n.Update(p,f,1);}
  public virtual bool IsAutomaticHoldPoint(ActionModPlayer p,int f)=>false;
  public virtual bool ShouldContinueHolding(ActionModPlayer p,int f)=>true;
  public virtual void OnHoldEnter(ActionModPlayer p,int f){} public virtual void OnHoldUpdate(ActionModPlayer p,int f,int t){}
  public virtual void OnHoldExit(ActionModPlayer p,int f){} public virtual void OnInterrupt(ActionModPlayer p){}
  public virtual void OnFinish(ActionModPlayer p){} public void ApplyFrameEffects(ActionModPlayer p,int f,float t){}
 }
 public static class AnimActionRegistry { public static bool TryCreate(int id,out AnimAction a){a=new 伊蕾娜.ElainaActions.Action_WaterLaser();return true;} }
 public class ActionNode {
  public int StartFrame;public ActionNode(int start,int end){StartFrame=start;}
  public virtual void Update(ActionModPlayer p,int f,float progress){}
 }
}
namespace 伊蕾娜.ElainaAttribute {public class ElainaAttributeModPlayer {public bool UniqueMagicEnabled=true;}}
namespace 伊蕾娜.ElainaModSkills {public static class ElainaSkillModPlayer {public static int CurrentSkillIndex;}}
namespace 伊蕾娜.ElainaActions {
 public abstract class ElainaAction:KL.ActionsSystem.AnimAction {
  protected Player owner; protected Vector2 WandCenter=>Vector2.Zero;
  protected ElainaAction(int n):base(n){} public override void OnStart(KL.ActionsSystem.ActionModPlayer p){owner=p.Player;}
  protected void SyncItemRotationToArmRotation(){}
 }
}
namespace 伊蕾娜.ElainaModSkills.Skills.Water {
 public class HitBase {public virtual void ModifyHitNPC(NPC target,ref NPC.HitModifiers modifiers){} }
 public class WaterLaser:HitBase {
  public Projectile Projectile=new(); public int primaryTarget=1;
  public void BindAction(int t,float mana,int interval){} public void StopChannel(){Projectile.active=false;}
`+constants+'\n'+method+String.raw`
 }
}
`);
fs.writeFileSync(path.join(scratch,'Program.cs'),String.raw`
using KL.ActionsSystem;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.Skills.Water;
using 伊蕾娜.ElainaAttribute;
static void Check(bool value,string name){if(!value)throw new Exception(name);}
static (Player,ActionModPlayer,Action_WaterLaser) Start(){
 Main.mouseLeft=true;ElainaSkillModPlayer.CurrentSkillIndex=0;GamePlayStatic.Shots=0;
 CombinedHooks.CanShootAllowed=CombinedHooks.ShootAllowed=true;
 var p=new Player();var ap=p.GetModPlayer<ActionModPlayer>();var a=new Action_WaterLaser(90,1.5f,12);
 Main.LocalPlayer=p;
 Check(ap.StartAction(a),"start");return(p,ap,a);
}
{
 var(p,ap,a)=Start();ap.PostUpdate();
 Check(GamePlayStatic.Shots==1&&ap.IsActionHolding&&a.IsChanneling,"first update creates beam through wand Shoot before holding");
 for(int n=0;n<120;n++)ap.PostUpdate();
 Check(GamePlayStatic.Shots==1&&ap.IsActionHolding,"sustained hold never reshoots");
 Main.mouseLeft=false;ap.PostUpdate();Check(!ap.IsActionHolding&&!a.IsChanneling,"release ends channel");
 for(int n=0;n<22;n++)ap.PostUpdate();Check(!ap.HasAction&&GamePlayStatic.Shots==1,"recovery completes without late shot");
}
{
 var(p,ap,a)=Start();Main.mouseLeft=false;ap.PostUpdate();
 for(int n=0;n<22;n++)ap.PostUpdate();Check(GamePlayStatic.Shots==0,"cancel before first frame never fires");
}
foreach(var reason in new[]{"slot","item","mana","death","disable"}) {
 var(p,ap,a)=Start();ap.PostUpdate();
 switch(reason){case "slot":ElainaSkillModPlayer.CurrentSkillIndex=1;break;case "item":p.HeldItem=new();break;
 case "mana":a.FinishChannel(ap);break;case "death":p.dead=true;break;
 case "disable":p.GetModPlayer<ElainaAttributeModPlayer>().UniqueMagicEnabled=false;break;}
 ap.PostUpdate();Check(!ap.IsActionHolding&&!a.IsChanneling,"cancel: "+reason);
}
{
 var(p,ap,old)=Start();ap.PostUpdate();var next=new Action_WaterLaser();ap.StartAction(next);ap.PostUpdate();
 old.FinishChannel(ap);Check(ap.IsActionHolding&&next.IsChanneling,"old token cannot end replacement");
}
foreach(var splash in new[]{false,true})foreach(var cooldown in new[]{0,1,30,3600}) {
 var beam=new WaterLaser();var target=new NPC{whoAmI=splash?2:1,Cooldown=cooldown};
 var h=new NPC.HitModifiers{SourceDamage=1};beam.ModifyHitNPC(target,ref h);
 float expected=(splash?.5f:1f)*(cooldown>0?1f:0.7f/0.9f);
 Check(Math.Abs(h.SourceDamage-expected)<1e-6f,"per-target cooldown/splash modifier");
 Check(target.Cooldown==cooldown,"damage query never reduces cooldown");
}
foreach(var veto in new[]{"CanShoot","Shoot"}) {
 var(p,ap,a)=Start();if(veto=="CanShoot")CombinedHooks.CanShootAllowed=false;else CombinedHooks.ShootAllowed=false;
 ap.PostUpdate();Check(GamePlayStatic.Shots==0&&!ap.IsActionHolding,"third-party hook veto respected: "+veto);
}
{
 var(p,ap,a)=Start();var wand=new 伊蕾娜.Items.ElainaWand();
 var automatic=new EntitySource_ItemUse_WithAmmo(p,p.HeldItem,0);
 Check(!wand.Shoot(p,automatic,Vector2.Zero,Vector2.Zero,1,90,0),"automatic wand shot blocked at frame zero");
 ap.PostUpdate();Main.mouseLeft=false;ap.PostUpdate();
 Check(ap.CurrentActionFrame>0,"recovery reached");
 Check(!wand.Shoot(p,automatic,Vector2.Zero,Vector2.Zero,1,90,0),"automatic wand shot blocked during recovery too");
 // Delayed action shots used by other skills still pass the same source-aware hook.
 AnimAction.ShootFromAction(p,1,Vector2.Zero,Vector2.Zero,90,0);
 Check(GamePlayStatic.Shots==2,"nonzero-frame action shots remain supported");
}
Console.WriteLine("PASS real action shot helper/node + wand Shoot hook: immediate channel, no automatic duplicate shots, delayed action shots, hook vetoes, sustained hold, release/recovery, early cancellation, slot/item/mana/death/disable cancellation, stale action token, per-target cooldown and splash damage");
`);
const run=spawnSync('dotnet',['run','--project',path.join(scratch,'Tests.csproj'),'-v:q'],{encoding:'utf8'});
process.stdout.write(run.stdout);process.stderr.write(run.stderr);
if(run.status!==0)process.exit(run.status||1);
