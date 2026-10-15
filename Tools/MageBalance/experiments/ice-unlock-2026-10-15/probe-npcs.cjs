'use strict';
// Read vanilla NPC defaults and HitModifiers directly from the locally installed tModLoader.
const fs=require('node:fs'),path=require('node:path'),os=require('node:os'),crypto=require('node:crypto');
const {spawnSync}=require('node:child_process');
const root=path.resolve(__dirname,'../../../..');
const targets=fs.readFileSync(path.resolve(root,'../tModLoader.targets'),'utf8');
const tmlTargets=process.argv[2]||targets.match(/Import Project="([^"]+)"/)[1];
const dll=path.join(path.dirname(tmlTargets),'tModLoader.dll');
const scratch=fs.mkdtempSync(path.join(os.tmpdir(),'elaina-npc-probe-'));
const output=path.join(scratch,'npcs.json');
const xml=s=>s.replaceAll('&','&amp;').replaceAll('"','&quot;');
fs.copyFileSync(path.join(__dirname,'NpcProbe.cs.txt'),path.join(scratch,'Program.cs'));
fs.writeFileSync(path.join(scratch,'Probe.csproj'),`<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><BuildMod>false</BuildMod><OutputTmlReferences>true</OutputTmlReferences><RunAnalyzers>false</RunAnalyzers></PropertyGroup><Import Project="${xml(tmlTargets)}" /></Project>`);
const r=spawnSync('dotnet',['run','--project',path.join(scratch,'Probe.csproj'),'-v:q','--',output],{encoding:'utf8'});
if(r.status!==0)throw new Error(r.stdout+'\n'+r.stderr);
const data=JSON.parse(fs.readFileSync(output,'utf8'));
data.source={date:'2026-10-15',dll,sha256:crypto.createHash('sha256').update(fs.readFileSync(dll)).digest('hex'),
  method:'NPC.SetDefaults; single player; no special seed; NPC.GetIncomingStrikeModifiers/GetDamage; no random damage variation; vanilla NPCs without loaded mods'};
fs.writeFileSync(path.join(__dirname,'boss-stats.json'),JSON.stringify(data,null,2)+'\n');
console.log(JSON.stringify({source:data.source,master:data.rows.filter(r=>r.mode===2)},null,2));
