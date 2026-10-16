using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.System;

namespace 伊蕾娜.Config
{
    public enum SkillSelectionMode
    {
        Slide,
        Wheel
    }

    [BackgroundColor(164, 153, 190)]
    public class BasicConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(true)]

        public bool Water;
        //public float IgnoreWithLabelGetter =>  Public;

        [DefaultValue(true)]

        public bool MissileCamera;

        [DefaultValue(0.33f)]
        [Range(0.2f, 2)]

        public float LongPress = 0.2f;

        //显示具体CD
        [DefaultValue(false)]
        public bool ShowCD;

        [DefaultValue(SkillSelectionMode.Slide)]
        public SkillSelectionMode SkillSelection;
        
        
        [Header("Damage")]

        [DefaultValue(1f)]
        [Range(0.3f, 3f)]
        [Increment(0.1f)]
        //[Slider]
        [DrawTicks]

        public float MaxDamage = 1f;
        

        public override void OnChanged()
        {
            ElainaModplayer.water = Water;
            ElainaModplayer.MissileCamera = MissileCamera;

            ElainaModplayer.LongPress = (int)(LongPress * 60);
            EXPmodplayer.DamageScale = MaxDamage;
            ElainaSkillManager.ShowCD = ShowCD;
            ElainaSkillManager.SelectionMode = SkillSelection;
            base.OnChanged();
        }
    }
}
