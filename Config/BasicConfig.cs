using System.ComponentModel;
using Terraria.ModLoader.Config;
using 伊蕾娜.ElainaModSkills;

namespace 伊蕾娜.Config
{
    [BackgroundColor(164, 153, 190)]
    public class BasicConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(true)]
        public bool ShowCD = true;

        [DefaultValue(0.75f)]
        [Range(0.3f, 1.5f)]
        [Increment(0.05f)]
        [Slider]
        public float SkillWheelScale = 0.75f;

        public override void OnChanged()
        {
            ElainaSkillManager.ShowCD = ShowCD;
            ElainaSkillManager.WheelScale = SkillWheelScale;
            base.OnChanged();
        }
    }
}
