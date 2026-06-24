using System.Reflection;
using KL.Configs;
using KL.Utils;
using Terraria.ModLoader;
using Terraria.Localization;
using Terraria.ModLoader.IO;
using Terraria.ModLoader.Config;

namespace 伊蕾娜.Config
{
    public class KeyBind : ModSystem
    {
        public static ModKeybind BroomAcceleration { get; private set; }
        public static ModKeybind OpenSkillPanel { get; private set; }
        
        public static ModKeybind SwitchNextSkill { get; private set; }
        
        public static ModKeybind SwitchPreviousSkill { get; private set; }
        
        public override void Load()
        {
            BroomAcceleration = KeybindLoader.RegisterKeybind(Mod, "BroomAcceleration", "Space");
            OpenSkillPanel = KeybindLoader.RegisterKeybind(Mod, "OpenSkillPanel", "V");
            SwitchNextSkill = KeybindLoader.RegisterKeybind(Mod, "SwitchNextSkill", "E");
            SwitchPreviousSkill = KeybindLoader.RegisterKeybind(Mod, "SwitchLastSkill", "Q");
        }

        public override void Unload()
        {
            BroomAcceleration = null;
            OpenSkillPanel = null;
            SwitchNextSkill = null;
            SwitchPreviousSkill = null;
        }

        public static void InitAllKeybinds()
        {
            BroomAcceleration.BindKey(BroomAcceleration.GetKeyDefaultName());
            OpenSkillPanel.BindKey(OpenSkillPanel.GetKeyDefaultName());
            SwitchNextSkill.BindKey(SwitchNextSkill.GetKeyDefaultName());
            SwitchPreviousSkill.BindKey(SwitchPreviousSkill.GetKeyDefaultName());
        }
        
        class KeyInitSystem : ClientSaveLoadSystem
        {
            public static List<string> KeyList = new List<string>();
            
            public override void SaveClientFlags(TagCompound tag)
            {
                if (KeyList is { Count: > 0 }) tag["KeyList"] = KeyList;
                else tag["KeyList"] = null;
                
                base.SaveClientFlags(tag);
            }

            public override void LoadClientFlags(TagCompound tag)
            {
                tag.TryGet("KeyList", out KeyList);
                base.LoadClientFlags(tag);
            }
        }

        class KeyModPlayer : ModPlayer
        {
            public override void OnEnterWorld()
            {
                KeyInitSystem.KeyList ??= new List<string>();
                var props = typeof(KeyBind).GetProperties(BindingFlags.Public | BindingFlags.Static);

                // 遍历所有keybind，如果没有绑定过则绑定到默认值
                foreach (var prop in props)
                {
                    if (prop.PropertyType != typeof(ModKeybind)) continue;
                    ModKeybind keybind = (ModKeybind)prop.GetValue(null);
                    string keybindFullName = keybind.GetKeybindFullName();
                    if (keybind == null || KeyInitSystem.KeyList.Contains(keybindFullName)) continue;
                    
                    keybind.BindKey(keybind.GetKeyDefaultName());
                    KeyInitSystem.KeyList.Add(keybindFullName);
                    //PrintText($"未曾绑定过按键:{keybind.DisplayName}, 绑定到默认值");
                }
                //KeyInitSystem.KeyList.Clear();
                base.OnEnterWorld();
            }
        }
    }
    
}
