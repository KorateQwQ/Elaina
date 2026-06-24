using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.System;

namespace 伊蕾娜.Config
{
    [BackgroundColor(164, 153, 190)]
    public class BasicConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(true)]
        [Label("水是否生成/Does water magic generate water")]
        public bool Water;
        //public float IgnoreWithLabelGetter =>  Public;

        [DefaultValue(true)]
        [Label("魔力飞弹特写镜头/close-up when shooting MagicMissile")]
        public bool MissileCamera;

        [DefaultValue(0.33f)]
        [Range(0.2f, 2)]
        [Label("长按最小判定秒数")]
        public float LongPress = 0.2f;

        //显示具体CD
        [DefaultValue(false)]
        public bool ShowCD;
        
        
        [Header("Damage")]

        
        [DefaultValue(1f)]
        [Range(0.3f, 3f)]
        [Increment(0.1f)]
        //[Slider]
        [DrawTicks]
        [Label("伤害比例")]
        public float MaxDamage = 1f;
        

        public override void OnChanged()
        {
            ElainaModplayer.water = Water;
            ElainaModplayer.MissileCamera = MissileCamera;

            ElainaModplayer.LongPress = (int)(LongPress * 60);
            EXPmodplayer.DamageScale = MaxDamage;
            ElainaSkillManager.ShowCD = ShowCD;
            base.OnChanged();
        }
    }
}
