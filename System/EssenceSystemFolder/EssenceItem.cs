using System;
using System.IO;
using System.Linq;
using KL.Extensions;
using KL.Utils;
using Terraria.GameContent;
using Terraria.ModLoader.IO;
using Terraria.Utilities;
using 伊蕾娜.System.EssenceSystemFolder;
using 伊蕾娜.System.EssenceSystemFolder.EssenceAffixFolder;

namespace 伊蕾娜.System.EssenceSystemFolder;

public class EssenceItem : ModItem
{
    private float drawScale = 0.5f;

    //————需要存档和联机同步的重要属性————//
    public Color ColorA = Color.White;
    public Color ColorB = Color.White;
    public Color ColorC = Color.White;
    
    public float BossState = 0;
    public string BossName = "";

    // 词条定义（运行时引用）：
    // - 不要直接存档/同步这个对象（运行时顺序会变）
    // - 存档/同步应使用稳定的 `AffixKey`
    public EssenceAffixDef? Affix;

    // 词条 Key（稳定标识）：用于存档/联机同步
    public string AffixKey = "";

    // 词条 roll 结果（0..1）：用于决定数值强度，必须存档/联机同步
    public float AffixRoll01 = 0f;

    // 处于封印状态：不显示、也不生效词条
    public bool Sealed = true;
    
    //————其他属性————//
    
    static Texture2D noiseTex;
    static Texture2D crystalTex;
    static Effect effect;

    private Texture2D thisTex => TextureAssets.Item[Type].Value;
    
    private float time = 0;

    #region load and save data
    public override void Load()
    {
        
        noiseTex ??=ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/T_Noise_23", AssetRequestMode.ImmediateLoad).Value;
        crystalTex ??=ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/CrystalNoise_Nor", AssetRequestMode.ImmediateLoad).Value;
        effect ??=ModContent.Request<Effect>("伊蕾娜/Effects/Content/EssenceEffect", AssetRequestMode.ImmediateLoad).Value;
        
        base.Load();
    }

    public override void SaveData(TagCompound tag)
    {
        tag["colorA"] = ColorA;
        tag["colorB"] = ColorB;
        tag["colorC"] = ColorC;
        tag["bossName"] = BossName;
        tag["bossState"] = BossState;

        // 词条相关存档（必须是稳定数据）
        tag["sealed"] = Sealed;
        tag["affixKey"] = AffixKey;
        tag["affixRoll01"] = AffixRoll01;
        base.SaveData(tag);
    }

    public override void LoadData(TagCompound tag)
    {
        tag.TryGet("colorA", out ColorA);
        tag.TryGet("colorB", out ColorB);
        tag.TryGet("colorC", out ColorC);
        tag.TryGet("bossName", out BossName);
        tag.TryGet("bossState", out BossState);

        // 词条相关读档
        tag.TryGet("sealed", out Sealed);
        tag.TryGet("affixKey", out AffixKey);
        tag.TryGet("affixRoll01", out AffixRoll01);

        ResolveAffixFromKey();
        Item.rare = GetRarity();
        base.LoadData(tag);
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.WriteRGB(ColorA);
        writer.WriteRGB(ColorB);
        writer.WriteRGB(ColorC);
        writer.Write(BossName);
        writer.Write(BossState);

        // 词条相关联机同步（顺序必须与 NetReceive 完全一致）
        writer.Write(Sealed);
        writer.Write(AffixKey ?? string.Empty);
        writer.Write(AffixRoll01);
        base.NetSend(writer);
    }

    public override void NetReceive(BinaryReader reader)
    {
        ColorA = reader.ReadRGB();
        ColorB = reader.ReadRGB();
        ColorC = reader.ReadRGB();
        BossName = reader.ReadString();
        BossState = reader.ReadSingle();

        // 词条相关联机同步
        Sealed = reader.ReadBoolean();
        AffixKey = reader.ReadString();
        AffixRoll01 = reader.ReadSingle();

        ResolveAffixFromKey();
        Item.rare = GetRarity();
        base.NetReceive(reader);
    }
    #endregion
    
    public void Initialize(Color[] colors, string bossName,float bossState)
    {
        BossName = bossName;
        BossState = bossState;
        
        if (colors.Length <= 2) return;
        ColorA = colors[0];
        ColorB = colors[1];
        ColorC = colors[2];

        Item.rare = GetRarity();
    }
    
    float GetBossProgress01()
    {
        // 获取 boss 阶段进度（0..1）
        float max = KLGameStateManager.GetWorldMaxBossValue();
        if (max <= 0)
            return 0f;

        return MathF.Min(1f, BossState / max);
    }

    void ResolveAffixFromKey()
    {
        // 根据稳定 Key 反查运行时的定义对象。
        // 这一步必须在：LoadData/NetReceive 之后做。
        if (!string.IsNullOrWhiteSpace(AffixKey) && EssenceAffixRegistry.TryGet(AffixKey, out var def))
            Affix = def;
        else
            Affix = null;
    }

    int GetRarity()
    {
        float stateProgress = GetBossProgress01();
        int maxRarity = EssenceRaritySystem.ItemMaxRarity ?? 0;
        return (int)(stateProgress * maxRarity);
    }
    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 30;
        
        Item.accessory = true;
        Item.rare = GetRarity();
        base.SetDefaults();
    }

    #region 能否装备判定与右键判定,以及roll词条的方法

    public void TryResonateAffix(string bossName,float bossState)
    {
        // 抽取一个满足条件的词条，并把“稳定 Key + roll 结果”存下来。
        // 注意：
        // - 不要用 runtimeId 做存档/同步（注册顺序变化会导致错位）
        // - 不要用固定种子 new UnifiedRandom(0)（会导致每次都一样）
        var rollContext = new EssenceAffixRollContext(bossName, bossState);
        if (EssenceAffixRegistry.TryRollRandomAffix(Main.rand, rollContext, out var resultAffix))
        {
            Affix = resultAffix;
            
            AffixKey = resultAffix.Key;
            AffixRoll01 = resultAffix.Roll01(Main.rand, rollContext);
            PrintText($"抽到词条：{resultAffix.Key}，效果为{
                resultAffix.Effect.GetDescription(GetEffectContext())},词条强度为{AffixRoll01}");
        }
        else
        {
            // 如果当前没有任何候选词条（理论上不该发生），保持为空。
            Affix = null;
            AffixKey = string.Empty;
            AffixRoll01 = 0f;
            PrintText("什么都没抽到");
        }
    }
    
    public override bool CanAccessoryBeEquippedWith(Item equippedItem, Item incomingItem, Player player)
    {
        return base.CanAccessoryBeEquippedWith(equippedItem, incomingItem, player);
    }

    public override bool CanEquipAccessory(Player player, int slot, bool modded)
    {
        //检查player身上的essencesitem是否超过2个
        if (player.GetModPlayer<EssencesModPlayer>().EssencesItemCount >= 2)
        {
            return false;
        }

        return base.CanEquipAccessory(player, slot, modded);
    }

    public override bool CanRightClick()
    {
        return Sealed;
    }

    public override void RightClick(Player player)
    {
        TryResonateAffix(BossName,18);
        base.RightClick(player);
    }

    public override bool ConsumeItem(Player player)
    {
        return false;
    }

    #endregion

    public EssenceAffixEffectContext GetEffectContext()
    {
        return new EssenceAffixEffectContext(
            Affix,
            AffixRoll01,
            BossName,
            18,
            GetBossProgress01());
    }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<EssencesModPlayer>().EssencesItemCount++;

        // 只有解封后才应用词条效果
        if (/*!Sealed && */Affix != null)
        {
            var ctx = GetEffectContext();

            // 词条效果
            Affix.Effect.UpdateAccessory(player, ctx);
        }
        base.UpdateAccessory(player, hideVisual);
    }

    /*public override string Name { get; }

    public override LocalizedText DisplayName { get; }*/
    
    public string Colorize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text ?? string.Empty;

        if (text.Contains("\\n"))
            text = text.Replace("\\n", "\n");

        text = text.Replace("\r\n", "\n");

        string ColorizeLines(string colorHex)
        {
            var lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                // 空行就保持空行，不额外塞颜色标记
                if (lines[i].Length == 0)
                    continue;

                lines[i] = $"[c/{colorHex}:{lines[i]}]";
            }
            return string.Join("\n", lines);
        }

        switch (Affix?.Rarity)
        {
            case EssenceAffixRarity.White:
                return ColorizeLines("FFFFFF");
            case EssenceAffixRarity.Blue:
                return ColorizeLines("bd6ff");
            case EssenceAffixRarity.Purple:
                return ColorizeLines("BB46FF");
            case EssenceAffixRarity.Gold:
                return ColorizeLines("FDEB09");
            case EssenceAffixRarity.Prismatic:
                return ColorizeLines("FD0909");
            default:
                return text;
        }
    }

    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        tooltips.Add(new TooltipLine(Mod, "概率展示",EssenceAffixRegistry.DebugDescribeRarityChances(BossState)));

        if (Affix is { Effect: not null })
        {
            tooltips.Add(new TooltipLine(Mod, "词条", $"{Colorize(Affix.Effect.GetDescription(GetEffectContext()))}, 词条强度为{AffixRoll01}"));
        }
        //PrintText($"Item state {BossState} Item rarity {Item.rare}");
        base.ModifyTooltips(tooltips);
    }
    //public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(0); // 重写Tooltip并传入数值


    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor,
        Vector2 origin, float scale)
    {
        EndBeginDrawUI(2, 1);
        DrawItem(position, scale*0.01f);
        //EndBeginDrawUI(1, 1);

        /*SphereEffect(new Vector3(0,0,0),new Vector3(-time*5,0,0),sphereColor:ColorA.ToVector4(),sphereScale:new Vector2(1,0.2f),clipImageY:true);
        DrawInScreen(trail, position, scale: new Vector2(scale)*0.25f);*/
        EndBeginDrawUI();
        DrawInScreen(thisTex, position, scale: new Vector2(scale*1f));
        //DrawInScreen(tex1, position, scale: new Vector2(scale));

        return false;
    }

    void DrawItem(Vector2 position, float scale)
    {
        Item.SetNameOverride($"{Language.GetTextValue($"Mods.伊蕾娜.EssencesItem")} ({BossName})");

        if(thisTex==null)Main.instance.LoadItem(Type);
        
        time += 0.01f;

        effect.SetValue("ColorA", ColorA.ToVector4());
        effect.SetValue("ColorB", ColorB.ToVector4());
        effect.SetValue("ColorC", ColorC.ToVector4());

        effect.SetValue("NoiseTiling", new Vector2(1.5f));
        effect.SetValue("CrystalTiling", new Vector2(1.0f, 1.0f));

        effect.SetValue("AlphaStrength", 5.0f);
        effect.SetValue("EdgeSoftness", 0.0f);
        
        effect.SetValue("iTime", time);
        effect.SetValue("FlickerSpeed", 5.0f);
        effect.SetValue("FlickerAmount", 0.5f);

        effect.SetValue("NoiseFlowSpeed", new Vector2(0.1f));
        
        effect.SetValue("OutlineColor", new Vector4(0,0,0,1));
        effect.SetValue("OutlineStrength", 0f);
        effect.SetValue("OutlineThreshold", 0.2f);
        
        effect.SetValue("RimColor", new Vector4(1));
        effect.SetValue("RimStrength", 1.35f);
        effect.SetValue("RimPower", 2.0f);

        effect.SetValue("FinalScale",scale);

        // iChannel1 : register(s1)
        effect.SetTexture(1,crystalTex);
        effect.Apply();
        drawScale = 0.03f;
        // iChannel0 (s0) 默认就是 SpriteBatch 当前 Draw 的纹理
        
        DrawInScreen(noiseTex, position, scale: new Vector2(2));
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale,
        int whoAmI)
    {

        //Item.rare = 19;
        EndBeginDraw(2, 1);
        DrawItem(Item.position+new Vector2(Item.width/2f,Item.height/2f)-Main.screenPosition, scale*0.01f);
        EndBeginDraw();

        DrawInWorld(thisTex, Item.position+new Vector2(Item.width/2f,Item.height/2f), scale: new Vector2(scale));
        //DrawInWorld(tex1, Item.position+new Vector2(Item.width/2f,Item.height/2f), scale: new Vector2(scale));
        return false;
    }

    class EssencesModPlayer : ModPlayer
    {
        public int EssencesItemCount = 0;
        public override void ResetEffects()
        {
            EssencesItemCount = 0;
            base.ResetEffects();
        }
    }
}