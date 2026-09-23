const fs=require('fs'),path=require('path');
const option=process.argv.indexOf('--sharp-module');
const sharp=require(option<0?'sharp':path.resolve(process.argv[option+1]));
const here=__dirname,root=path.resolve(here,'../..'),out=path.join(root,'ElainaModAlchemy/item/ExampleAssets');
const plan=JSON.parse(fs.readFileSync(path.join(here,'plan.json'),'utf8'));
(async()=>{
 if(process.argv.includes('--pencils-only')){
  const targets=JSON.parse(fs.readFileSync(path.join(here,'generated','pencil-refresh.json'),'utf8'));
  for(const target of targets){
   await sharp(path.join(here,'generated',target.source),{density:288}).resize(target.size,target.size).png().toFile(path.join(out,target.output));
  }
  console.log('Replaced '+targets.length+' pencil PNGs; original SVG and pixel PNG assets were not written.');
  return;
 }
 for(const item of plan.items){
  await sharp(path.join(here,'generated',item.art+'_Pencil.svg'),{density:288}).resize(100,100).png().toFile(path.join(out,item.art+'_Pencil.png'));
  await sharp(path.join(out,item.art+'.svg'),{density:288}).resize(100,100).png().toFile(path.join(here,'generated',item.art+'.png'));
 }
 for(const material of plan.materials.filter(m=>!m.art)){
  await sharp(path.join(here,'generated','material-'+material.id+'.svg'),{density:288}).resize(64,64).png().toFile(path.join(out,'Materials',material.id+'_Pencil.png'));
 }
 console.log('Rendered transparent pencils for 23 mod items and 28 vanilla material glyphs.');
})().catch(error=>{console.error(error);process.exitCode=1});
