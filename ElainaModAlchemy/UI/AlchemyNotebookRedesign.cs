using static 伊蕾娜.Localization.UIText;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace 伊蕾娜.ElainaModAlchemy.UI;

internal static partial class AlchemyNotebookPresentation
{
    internal const float TabWidth = 148.5f;
    internal static readonly Rectangle CraftBounds = new(790, 660, 270, 36);
    internal static readonly Rectangle BagBounds = new(110, 721, 382, 64);
    // Only the horizontal material bag has a visible scroll bar.
    internal static readonly Rectangle BagScrollBar = new(114, 780, 374, 7);
    internal static readonly Color Heading = new(196, 180, 210), Mint = new(178, 222, 200);
    private static Color Dots => AlchemyDrawing.DotColor;
    internal static Color CategoryGlow(string category) => category switch
    { "curio" => new(130,200,235), "food" => new(245,190,130), "material" => new(190,205,170), _ => new(206,160,245) };

    internal static void Chrome(AlchemyDrawing d, AlchemyNotebookState s)
    {
        d.NotebookPage(DesignWidth, DesignHeight);
        d.Dots(32,83,1068,83,Dots);
        d.Diamond(new(193,83),2.6f,Lavender,true);
        d.Ring(new(61,47),29,29,Lavender*.18f);
        d.Diamond(new(61,47),29,Lavender*.24f);
        d.Image(AssetRoot+"AlchemyCrest",38,22,46,46,Lavender);
        d.FittedText(LocalizeUI("AlchemyTitle"),101,27,530,28,Ink,serif:true,spacing:3);
        ProgressHeader(d,s);
        d.DotSeparator(775.5f,92,136);
        Label(d,LocalizeUI("AlchemyCollection"),791,101,12,Heading,serif:true);
        d.LatinText(AlchemyCatalog.Recipes.Count(s.IsUnlocked).ToString("00"),1016,99,22,Ink,1);
        d.LatinText("/ "+AlchemyCatalog.Recipes.Count,1045,111,11,Muted,1);
        for (int i=0;i<AlchemyCatalog.Recipes.Count;i++)
        {
            var r=AlchemyCatalog.Recipes[i]; bool known=s.IsUnlocked(r);
            d.Diamond(new(794+i*8.3f,126),2.5f,known ? Ink : s.Research(r).CanResearch ? Gold : Muted*.5f,known);
        }
        d.Dots(26,145.5f,731,145.5f,Dots); d.Trio(new(753.5f,145.5f)); d.Dots(776,145.5f,1074,145.5f,Dots);
        d.Dots(753.5f,156,753.5f,700,Dots);
        d.Dots(26,711,731,711,Dots); d.Trio(new(753.5f,711)); d.Dots(776,711,1074,711,Dots);
        d.Spark(new(39,743),12,Ink);
        d.FittedText(LocalizeUI("Materials"),50,737,56,13,Ink,serif:true);
        d.FittedText(LocalizeUI("AlchemyMaterialKinds",AlchemyCatalog.Materials.Count(m=>s.MaterialCount(m.Id)>0)),50,760,57,9,Muted);
        d.DotSeparator(515,731,776);
#if DEBUG
        d.Spark(new(539,738),6,Muted*.5f);
        Label(d,LocalizeUI("AlchemyDebug"),547,733,9,Muted*.6f);
#endif
    }

    private static void ProgressHeader(AlchemyDrawing d, AlchemyNotebookState s)
    {
        d.Diamond(new(823,40),25.5f,Gold*.22f);
        d.LatinText("LV",823,19,9,Gold*.7f,.5f,1.5f);
        d.LatinText(s.Level.ToString(),823,31,30,Gold,.5f);
        d.FittedText(LocalizeUI("AlchemyLevelTitle"),868,23,89,11,Heading,serif:true);
        d.FittedText(LocalizeUI("AlchemyCap",s.MaximumLevel),999,24,54,9,Muted,1);
        Label(d,LocalizeUI("AlchemyExpLabel"),868,44,9,Muted);
        bool full=s.Level>=s.MaximumLevel;
        d.LatinText(full ? "MAX" : $"{s.Experience} / {s.ExperienceToNextLevel}",999,42,10,full?Gold:Ink*.8f,1);
        d.RoundedBar(868,61,131,5,new Color(8,6,14)*.55f);
        float progress=full?1:s.Experience/(float)Math.Max(1,s.ExperienceToNextLevel);
        if(progress>0) d.RoundedBar(869,62,Math.Max(3,129*Math.Clamp(progress,0,1)),3,(full?Gold:Lavender)*.8f);
    }

    internal static void Category(AlchemyDrawing d, AlchemyNotebookState s, AlchemyCatalogCategory c, bool hover)
    {
        bool active=s.Category==c.Id;
        var list=AlchemyCatalog.Recipes.Where(r=>c.Id=="all"||r.Category==c.Id).ToArray();
        string count=list.Length.ToString("00");
        float size=14, lw=d.Measure(c.Name,size,1), cw=d.LatinWidth(count,12);
        if(lw>102-cw) { size*= (102-cw)/lw; lw=d.Measure(c.Name,size,1); }
        float x=(TabWidth-(17+lw+8+cw))/2;
        d.Diamond(new(x+4.5f,29),4.5f,Lavender*(active?1:.7f),active);
        d.Text(c.Name,x+17,19,size,active||hover?Ink:Muted,spacing:1);
        d.LatinText(count,x+17+lw+8,21.5f,12,active?Pearl:Muted*.8f);
        if(c.Id!="all" && list.Any(r=>!s.IsUnlocked(r)&&s.Research(r).CanResearch))
            d.Diamond(new(x+17+lw+8+cw+4,20),2.4f,Gold*.9f,true);
        if(c.Id!="all") d.Dots(0,20,0,41,Dots*.65f);
    }

    // The tab rule's dots under the active tab light up (bright middle, fading ends) with a slim
    // flash on the rule; index may be fractional while the marker slides between tabs.
    // Tabs are 148.5 wide (33 dot steps) and the lit run starts on the rule's dot grid (26 + n·4.5),
    // so at rest every lit dot covers a rule dot instead of interleaving with it.
    internal static void TabMarker(AlchemyDrawing d, float index)
    {
        float shift=index*TabWidth, center=26+TabWidth/2+shift;
        float start=26+MathF.Floor((TabWidth/2-63)/4.5f+.5f)*4.5f+shift;
        d.Glow(center-72,138.5f,144,14,new Color(192,154,220)*.16f);
        d.Glow(center-24,139.5f,48,12,new Color(207,157,250)*.32f);
        for(float x=start;x<=start+126.01f;x+=4.5f)
        {
            float peak=Math.Max(0,1-Math.Abs(x-center)/63);
            d.Dot(new(x,145.5f),.7f,Color.Lerp(new Color(226,206,240),new Color(240,228,252),peak)*peak);
        }
        d.Star(new(center,145.5f),new(34,12),new Color(244,234,252));
    }

    internal static void Hero(AlchemyDrawing d, AlchemyNotebookState s, float brewProgress)
    {
        var r=s.Current; bool known=s.IsUnlocked(r); string state=s.StateOf(r);
        d.Spark(new(794,167),8,Lavender);
        d.FittedText(known?r.Kind:AlchemyCatalog.Categories.First(c=>c.Id==r.Category).Name,804,160,155,11,Heading);
        d.FittedText(AlchemyNotebookState.StateLabel(state),1060,160,100,10,state==AlchemyNotebookState.Short?Red:StateColor(state),1);
        Vector2 center=new(824,220); Color tone=state==AlchemyNotebookState.Researchable?Gold:Lavender;
        d.Glow(center.X-36,center.Y-36,72,72,CategoryGlow(r.Category)*.3f);
        if(known) d.OutlinedItem(r.IconPath,center.X,center.Y);
        else d.ItemIcon(r.LockedIconPath,center.X,center.Y,tint:Color.White*(state==AlchemyNotebookState.Locked?.45f:1));
        d.Line(center+new Vector2(-23,-27),center+new Vector2(23,-27),tone*.34f);
        d.Line(center+new Vector2(-23,27),center+new Vector2(23,27),tone*.34f);
        d.Line(center+new Vector2(-27,-23),center+new Vector2(-27,23),tone*.34f);
        d.Line(center+new Vector2(27,-23),center+new Vector2(27,23),tone*.34f);
        d.Spark(center-new Vector2(27),8,Ink*.9f);
        foreach(var corner in new[]{new Vector2(27,-27),new Vector2(-27,27),new Vector2(27,27)}) d.Dot(center+corner,.9f,tone*.45f);
        d.FittedText(known?r.Name:MaskedName(r),874,194,186,25,known?Ink:Muted,serif:true,spacing:1);
        d.LatinText("No."+Number(r).ToString("00"),874,235,12,Muted);
        if(known)
        {
            // Measure the metadata; long English labels get a second line instead of colliding.
            string owned=LocalizeUI("AlchemyOwned",s.Owned(r));
            float ownWidth=d.Measure(owned,10), rarityWidth=d.Measure(r.RarityName,10);
            bool wide=rarityWidth+ownWidth+8>100;
            for(int i=0;i<4;i++) d.Diamond(new(923+i*9,242),3,Gold*.8f,i<=r.Rarity);
            d.FittedText(r.RarityName,960,235,100,10,Muted);
            d.FittedText(owned,1060,wide?253:235,186,10,Muted,1);
        }
        else Label(d,LocalizeUI("Unresearched"),925,235,10,Muted);
        d.Dots(790,272,1060,272,Dots);
    }

    internal static List<AlchemyNotebookRow> DetailRows(AlchemyDrawing measure, AlchemyNotebookState s)
    {
        var rows=new List<AlchemyNotebookRow>(); var r=s.Current;
        void Paragraph(string value,int size,Color color,int leading,int after=0)
        {
            var lines=measure.WrapParagraph(value??"",DetailContentWidth,size);
            rows.Add(new(lines.Length*leading+after,(d,_)=>{for(int i=0;i<lines.Length;i++) Label(d,lines[i],0,i*leading,size,color);}));
        }
        void RuleRow() => rows.Add(new(25,(d,_)=>d.Dots(0,12,DetailContentWidth,12,Dots)));
        void Section(string title,string tail=null) => rows.Add(new(26,(d,_)=>
        {
            d.Spark(new(4,8),8,Lavender); Label(d,title,14,0,11,Heading);
            if(tail!=null)d.FittedText(tail,DetailContentWidth,1,130,10,Muted,1);
        }));
        if(!s.IsUnlocked(r))
        {
            var research=s.Research(r);
            Section(LocalizeUI("AlchemyClue"));
            Paragraph(LocalizeUI("AlchemyLevelRequirement",s.Level,research.RequiredLevel),12,s.Level>=research.RequiredLevel?Gold:Red,23,8);
            // Retain real recipe requirements; the HTML only mocks these conditions.
            if(research.RequiresRecipe)
            {
                Paragraph(research.RecipeConfigured?research.RecipeName:LocalizeUI("RecipeUnregistered"),12,Ink,22,4);
                Paragraph(LocalizeUI("InventoryOwned",research.RecipeOwned,research.RecipeRequired),11,
                    research.RecipeConfigured&&research.RecipeOwned>=research.RecipeRequired?Gold:Red,21,8);
                Paragraph(research.ConsumeRecipe?LocalizeUI("ConsumeRecipe"):LocalizeUI("KeepRecipe"),11,Muted,21);
            }
            return rows;
        }
        Section(LocalizeUI("Effect"));
        Paragraph(r.Description,13,Ink*.86f,23,8);
        foreach(var effect in r.Effects)
        {
            // Keep values opposite labels when they fit; long translations wrap below.
            float labelWidth=measure.Measure(effect.Label,12);
            float available=DetailContentWidth-labelWidth-12;
            bool stacked=available<90 || measure.Measure(effect.Value,12)>available*2;
            var lines=measure.WrapParagraph(effect.Value,stacked?DetailContentWidth:available,12);
            float height=Math.Max(28,lines.Length*19+8+(stacked?22:0));
            rows.Add(new(height,(d,_)=>
            {
                d.Line(new(0,0),new(DetailContentWidth,0),Ink*.06f);
                Label(d,effect.Label,0,5,12,Heading);
                for(int i=0;i<lines.Length;i++) Label(d,lines[i],DetailContentWidth,5+(stacked?22:0)+i*19,12,
                    effect.Tone=="bad"?Red:effect.Tone=="live"?Mint:Ink,1);
            }));
        }
        RuleRow();
        if(r.Craftable)
        {
            Section(LocalizeUI("RequiredMaterials"),LocalizeUI("AlchemyBatchTotal",s.Quantity));
            foreach(var ingredient in r.Ingredients)
            {
                var mats=ingredient.Choices.Select(AlchemyCatalog.GetMaterial).ToArray();
                string name=string.Join(" / ",mats.Select(m=>m.Name));
                string entry=mats.Length==1?mats[0].Entry:null;
                var names=measure.WrapParagraph(name+(entry!=null?" ›":""),155,13);
                float height=Math.Max(34,names.Length*19+8);
                rows.Add(new(height,(d,pointer)=>
                {
                    bool hover=pointer!=null;
                    int have=s.Available(ingredient),need=ingredient.Count*s.Quantity;
                    d.Line(new(0,height),new(DetailContentWidth,height),Ink*.06f);
                    d.Diamond(new(13,height/2),14,Lavender*.2f);
                    for(int i=0;i<mats.Length;i++)d.MaterialIcon(mats[i].IconPath,i*9,height/2-13+i*3,mats.Length==1?26:20,mats.Length==1?26:20);
                    for(int i=0;i<names.Length;i++) Label(d,names[i],36,5+i*19,13,hover&&entry!=null?Lavender:have<need?Muted:Ink*.9f);
                    string tail=" / "+need;
                    d.LatinText(have.ToString(),DetailContentWidth-d.LatinWidth(tail,14),height/2-11,17,have<need?Red:Gold,1);
                    d.LatinText(tail,DetailContentWidth,height/2-8,14,Muted,1);
                },entry,BlocksDragging:true));
            }
        }
        else { Section(LocalizeUI("Acquisition")); Paragraph(r.Acquisition,12,Ink*.8f,22); }
        string material=r.OutputMaterial??r.Material;
        if(material!=null)
        {
            var uses=AlchemyCatalog.Recipes.Where(x=>x.Ingredients.Any(i=>i.Choices.Contains(material))).ToArray();
            if(uses.Length>0)
            {
                RuleRow(); Section(LocalizeUI("AlchemyUsedIn"),LocalizeUI("AlchemyCreations",uses.Length));
                // Wrapped chips (HTML .chips): 22 tall pills 6 apart, flowing left to right;
                // researched entries lead with their icon, locked ones stay masked but still jump.
                const float chipHeight=22,chipGap=6,chipText=11,chipSpacing=1.2f;
                var chips=new List<(float X,float Y,float W,AlchemyCatalogRecipe Use,string Label,bool Known)>();
                float cx=0,cy=0;
                foreach(var use in uses)
                {
                    bool known=s.IsUnlocked(use);
                    string label=known?use.Name:MaskedName(use);
                    float w=Math.Min(DetailContentWidth,20+(known?22:0)+measure.Measure(label,chipText,chipSpacing));
                    if(cx>0&&cx+w>DetailContentWidth){cx=0;cy+=chipHeight+chipGap;}
                    chips.Add((cx,cy,w,use,label,known));
                    cx+=w+chipGap;
                }
                bool Inside((float X,float Y,float W,AlchemyCatalogRecipe,string,bool) c,Vector2 p)
                    => p.X>=c.X&&p.X<c.X+c.W&&p.Y>=c.Y&&p.Y<c.Y+chipHeight;
                rows.Add(new(cy+chipHeight+4,(d,pointer)=>
                {
                    foreach(var c in chips)
                    {
                        bool hover=pointer is {} p&&Inside(c,p);
                        d.RoundRect(c.X,c.Y,c.W,chipHeight,chipHeight/2,Ink*(hover?.05f:0),Ink*(hover?.5f:.2f),1);
                        float tx=c.X+10;
                        if(c.Known){d.MaterialIcon(c.Use.IconPath,tx,c.Y+3,16,16);tx+=22;}
                        d.FittedText(c.Label,tx,c.Y+4.5f,c.X+c.W-10-tx,chipText,
                            c.Known?hover?Ink:new Color(214,200,224):new Color(108,101,121),spacing:chipSpacing);
                    }
                },EntryAt:p=>chips.FirstOrDefault(c=>Inside(c,p)).Use?.Id));
            }
        }
        rows.Add(new(12,(_,_)=>{}));
        Section(LocalizeUI("AlchemyResearchRecord"));
        // Use current research metadata, never an obsolete catalog level hint.
        Paragraph(!r.Craftable ? r.Stage??r.Acquisition : LocalizeUI("ResearchedLevel",s.Research(r).RequiredLevel),10,Muted,18,8);
        return rows;
    }

    internal static void Footer(AlchemyDrawing d,AlchemyNotebookState s,bool brewing)
    {
        d.Dots(790,605,1060,605,Dots);
        bool craft=s.IsUnlocked(s.Current)&&s.Current.Craftable;
        if(craft)
        {
            d.FittedText(LocalizeUI("AlchemyBatchCount"),790,622,31,11,Heading);
            d.RoundRect(826,615,136,27,13.5f,new Color(26,22,35)*.45f,Ink*.18f,1);
            d.LatinText(s.Quantity.ToString(),868,621,15,Ink,.5f);
            d.Dots(913,622,913,636,Dots);
            d.FittedText(LocalizeUI("Output",s.Current.Yield*s.Quantity),1060,622,90,12,Mint,1);
        }
        string hint=Hint(s,brewing);
        d.FittedText(hint,1060,craft?647:632,270,10,craft&&!s.CanCraft()?Red:Muted,1);
    }

    internal static string CraftLabel(AlchemyNotebookState s,bool brewing)
        => brewing?LocalizeUI("Brewing"):s.IsBusy?LocalizeUI("Processing"):!s.IsUnlocked(s.Current)
            ?s.CanResearch()?LocalizeUI("Research"):LocalizeUI("LegendLocked")
            :!s.Current.Craftable?LocalizeUI("AlchemyExternalSource")
            :LocalizeUI("AlchemyCraftYield",s.Current.Yield*s.Quantity);

    internal static void CraftButton(AlchemyDrawing d,AlchemyNotebookState s,bool hover,float progress)
    {
        bool enabled=!s.IsBusy&&progress<=0&&(s.IsUnlocked(s.Current)?s.CanCraft():s.CanResearch());
        float alpha=enabled?1:.4f;
        d.RoundRect(0,0,270,36,18,new Color(185,155,87)*((hover&&enabled?.2f:.1f)*alpha),Gold*((hover&&enabled?.95f:.6f)*alpha),1);
        d.Spark(new(18.5f,18),9,Gold*(.55f*alpha)); d.Spark(new(251.5f,18),9,Gold*(.55f*alpha));
        d.FittedText(CraftLabel(s,progress>0),135,9,224,13,Gold*(progress>0?1:alpha),.5f,spacing:1);
        if(progress>0)d.RoundedBar(18,32,234*progress,2,Lavender);
    }

    internal static string Hint(AlchemyNotebookState s,bool brewing)
    {
        if(brewing)return LocalizeUI("BrewingHint");
        var r=s.Current;
        if(!s.IsUnlocked(r)) return s.CanResearch()&&!s.Research(r).RequiresRecipe?LocalizeUI("AlchemyResearchFree"):s.Research(r).Hint;
        if(!r.Craftable)return LocalizeUI("AlchemyCannotBrew");
        var missing=r.Ingredients.Where(i=>s.Available(i)<i.Count*s.Quantity)
            .Select(i=>LocalizeUI("AlchemyMissingMaterial",string.Join(" / ",i.Choices.Select(id=>AlchemyCatalog.GetMaterial(id).Name)),i.Count*s.Quantity-s.Available(i))).ToArray();
        return missing.Length>0?LocalizeUI("AlchemyMissingList",string.Join(LocalizeUI("AlchemyListSeparator"),missing))
            :LocalizeUI("AlchemyCraftCapacity",s.MaxBatches(r));
    }

    internal static AlchemyCatalogMaterial[] BagMaterials(AlchemyNotebookState s)
    {
        var need=s.IsUnlocked(s.Current)?s.Current.Ingredients.SelectMany(i=>i.Choices).Distinct().ToArray():[];
        return need.Select(AlchemyCatalog.GetMaterial).Concat(AlchemyCatalog.Materials.Where(m=>s.MaterialCount(m.Id)>0&&!need.Contains(m.Id))).ToArray();
    }

    internal static void MaterialSlot(AlchemyDrawing d,AlchemyNotebookState s,AlchemyCatalogMaterial m,bool hover)
    {
        var ingredient=s.IsUnlocked(s.Current)?s.Current.Ingredients.FirstOrDefault(i=>i.Choices.Contains(m.Id)):null;
        bool required=ingredient!=null, enough=!required||s.Available(ingredient)>=ingredient.Count*s.Quantity;
        Color tone=required?enough?Gold:Red:Lavender;
        if(required)d.Glow(-8,1,60,60,Gold*.2f);
        d.Box(1,9,42,42,required?new Color(185,155,87)*.1f:new Color(33,27,44));
        for(int k=1;k<=3;k++)d.Frame(1+k,9+k,42-2*k,42-2*k,tone*.07f);
        d.Frame(1,9,42,42,tone*(required?.9f:hover?1:.34f));
        d.MaterialIcon(m.IconPath,7,14,30,30);
        int owned=s.MaterialCount(m.Id);
        string count=required?$"{owned}/{ingredient.Count*s.Quantity}":owned.ToString();
        float w=d.LatinWidth(count,9);
        d.Box(41-w-3,46,w+6,10,new Color(32,30,47));
        d.LatinText(count,41,45,9,required?tone:Muted,1);
        if(required)d.Spark(new(7,15),6,tone);
    }
}
