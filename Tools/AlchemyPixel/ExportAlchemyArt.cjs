// Extract the two reviewed notebook art sets. Run with an installed sharp module.
// node Tools/AlchemyPixel/ExportAlchemyArt.cjs --sharp-module <path-to-sharp>
const fs=require('fs'),path=require('path'),vm=require('vm');
const option=process.argv.indexOf('--sharp-module');
const sharp=require(option<0?'sharp':path.resolve(process.argv[option+1]));
const root=path.resolve(__dirname,'../..');
const example=path.join(root,'ElainaModSkills/ElainaSkillUI/NewUIExample');
const output=path.join(root,'ElainaModAlchemy/item/ExampleAssets');
const sourcePreview=path.join(__dirname,'preview/source');
const names={
 mana:'ManaElixir',painkiller:'Painkiller',bloodlust:'BloodthirstPotion',starpower:'StarPowerPotion',
 focus:'ConcentrationPotion',resonance:'ResonancePotion',featherlight:'FeatherlightPotion',isolation:'IsolationPotion',
 extractor:'ShimmerExtractor',mimic:'AshenFacsimileDust',starspring:'StarSpringDew',bread:'HoneyStarBread',
 flameflask:'FlameFlask',ward:'WardPotion',starink:'StarInk',homeward:'HomewardBottle',
 bottledsky:'BottledSky',hairdye:'AshenHairDye',mimiclure:'MimicLure',wishslip:'WishSlip',
 incense:'MoonDewIncense',pudding:'GelPudding'
};
function context(filename){
 const html=fs.readFileSync(path.join(example,filename),'utf8');
 const script=[...html.matchAll(/<script(?:\s[^>]*)?>([\s\S]*?)<\/script>/g)].map(m=>m[1]).find(s=>s.includes('const ART='));
 if(!script)throw Error('Alchemy artwork not found in '+filename);
 const start=script.indexOf('const $a='),end=script.indexOf('function offsetWithin');
 if(start<0||end<start)throw Error('Artwork extraction boundary changed');
 // Reviewed pure declarations only: no page startup, storage, DOM, or listeners.
 const ctx=vm.createContext({});
 vm.runInContext(script.slice(start,end),ctx,{timeout:2000});
 return ctx;
}
const svg=uri=>decodeURIComponent(uri.slice(uri.indexOf(',')+1));
(async()=>{
 const normal=context('elaina-battle-eight-skills-alchemy.html');
 const study=context('elaina-battle-eight-skills-alchemy-all-pencil.html');
 const data=vm.runInContext('({art:ART,recipes:RECIPES.map(({id,name,art,cat})=>({id,name,art,cat})),categories:CATEGORIES})',normal);
 const pencil=vm.runInContext('PENCIL_ART',study),studyOriginal=vm.runInContext('ART',study);
 if(data.recipes.length!==Object.keys(names).length)throw Error('Recipe list changed; update asset names explicitly');
 fs.mkdirSync(output,{recursive:true});fs.mkdirSync(sourcePreview,{recursive:true});
 const manifest={source:'ElainaModSkills/ElainaSkillUI/NewUIExample/elaina-battle-eight-skills-alchemy.html',
  pencilSource:'ElainaModSkills/ElainaSkillUI/NewUIExample/elaina-battle-eight-skills-alchemy-all-pencil.html',
  output:'ElainaModAlchemy/item/ExampleAssets',pencilSize:[100,100],categories:data.categories,items:[]};
 for(const r of data.recipes){
  const stem=names[r.id];if(!stem||!pencil[r.art])throw Error('Missing art: '+r.id);
  if(studyOriginal[r.art]!==data.art[r.art])throw Error('Original art differs between notebooks: '+r.id);
  const originalSvg=svg(data.art[r.art]);
  fs.writeFileSync(path.join(output,stem+'.svg'),originalSvg,'utf8');
  await sharp(Buffer.from(svg(pencil[r.art])),{density:288}).resize(100,100).png().toFile(path.join(output,stem+'_Pencil.png'));
  await sharp(Buffer.from(originalSvg),{density:288}).resize(100,100).png().toFile(path.join(sourcePreview,stem+'.png'));
  manifest.items.push({...r,stem,original:stem+'.svg',pencil:stem+'_Pencil.png',pixel:stem+'_Pixel.png'});
 }
 fs.writeFileSync(path.join(__dirname,'manifest.json'),JSON.stringify(manifest,null,2)+'\n');
 console.log('Exported '+manifest.items.length+' original SVGs and '+manifest.items.length+' transparent pencil PNGs.');
})().catch(error=>{console.error(error);process.exitCode=1});
