using System.Reflection;
using KL.Configs;
using KL.Utils;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace 伊蕾娜.Config
{
    public class KeyBind : ModSystem
    {
        public static ModKeybind BroomAcceleration { get; private set; }
        public static ModKeybind OpenSkillPanel { get; private set; }
#if DEBUG
        public static ModKeybind OpenLoadoutPanel { get; private set; }
#endif

        public static ModKeybind DollMark { get; private set; }

        public static ModKeybind OpenSkillWheel { get; private set; }

        /// <summary>按住时技能栏跟随鼠标，松开后保存位置（模组全局，所有角色共用）。技能栏本身不响应鼠标。</summary>
        public static ModKeybind MoveSkillBar { get; private set; }

        public override void Load()
        {
            BroomAcceleration = KeybindLoader.RegisterKeybind(Mod, "BroomAcceleration", "Space");
            OpenSkillPanel = KeybindLoader.RegisterKeybind(Mod, "OpenSkillPanel", "V");
#if DEBUG
            OpenLoadoutPanel = KeybindLoader.RegisterKeybind(Mod, "OpenLoadoutPanel", "L");
#endif
            OpenSkillWheel = KeybindLoader.RegisterKeybind(Mod, "OpenSkillWheel", "Mouse2");
            DollMark = KeybindLoader.RegisterKeybind(Mod, "DollMark", "F");
            MoveSkillBar = KeybindLoader.RegisterKeybind(Mod, "MoveSkillBar", "K");
        }

        public override void Unload()
        {
            BroomAcceleration = null;
            OpenSkillPanel = null;
#if DEBUG
            OpenLoadoutPanel = null;
#endif
            OpenSkillWheel = null;
            MoveSkillBar = null;
            DollMark = null;
        }

        public static void InitAllKeybinds()
        {
            BroomAcceleration.BindKey(BroomAcceleration.GetKeyDefaultName());
            OpenSkillPanel.BindKey(OpenSkillPanel.GetKeyDefaultName());
#if DEBUG
            OpenLoadoutPanel.BindKey(OpenLoadoutPanel.GetKeyDefaultName());
#endif
            OpenSkillWheel.BindKey(OpenSkillWheel.GetKeyDefaultName());
            DollMark.BindKey(DollMark.GetKeyDefaultName());
            MoveSkillBar.BindKey(MoveSkillBar.GetKeyDefaultName());
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
                    if (keybind == null) continue;
                    string keybindFullName = keybind.GetKeybindFullName();
                    if (KeyInitSystem.KeyList.Contains(keybindFullName)) continue;
                    
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
