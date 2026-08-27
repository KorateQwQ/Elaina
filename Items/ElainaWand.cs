using System;
using System.Collections.Generic;
using System.IO;
using KL.ActionsSystem;
using KL.DamageSystem.ElementalDamageClass;
using KL.Drawing;
using KL.Dusts;
using KL.SkillSystem;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using 伊蕾娜.Config;
using 伊蕾娜.ElainaActions;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.ElainaModSkills;
using 伊蕾娜.ElainaModSkills.ElainaDamageClass;
using 伊蕾娜.ElainaModSkills.Skills;
using 伊蕾娜.ElainaModSkills.Skills.Fire;
using 伊蕾娜.ElainaModSkills.Skills.Lightning;
using 伊蕾娜.ElainaModSkills.Skills.MagicMissile;
using 伊蕾娜.ElainaModSkills.Skills.Water;
using 伊蕾娜.Projectiles.Flame;
using 伊蕾娜.Projectiles.MagicBarrier;
using 伊蕾娜.Projectiles.MagicMissile;
using 伊蕾娜.ReProjs;
using 伊蕾娜.ReProjs.Water;

namespace 伊蕾娜.Items
{
    public class ElainaWand : ModItem
    {
        private static SoundStyle 飞弹, 生成;
        private static int iceCount = 0;
        private static int windCount = 0;
        
        //TODO: 把技能不同的基础伤害写出来，控制武器显示当前武器的百分比数值
        public override string Texture => base.Texture.Replace(GetType().Name, "魔杖");
        public override void Load()
        {
            飞弹 = (new SoundStyle($"伊蕾娜/Projectiles/MagicMissile/飞弹", 1, SoundType.Sound)
            {
                Volume = 0.1f,
                MaxInstances = 5,
                Pitch = 0,
                PitchVariance = 0.3f,
                SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
                PlayOnlyIfFocused = true,
            });
            生成 = (new SoundStyle($"伊蕾娜/Projectiles/MagicMissile/生成", 1, SoundType.Sound)
            {
                Volume = 0.1f,
                MaxInstances = 5,
                Pitch = 0,
                PitchVariance = 0.3f,
                SoundLimitBehavior = SoundLimitBehavior.ReplaceOldest,
                PlayOnlyIfFocused = true,
            });
        }
        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }
        public override void SetDefaults()
        {
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.DamageType = ModContent.GetInstance<ElainaBasicDamage>();
            
            Item.damage = 100;
            Item.width = 15;
            Item.height = 15;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<MagicMissile>();
            Item.staff[Item.type] = true;
            Item.shootSpeed = 15;
            Item.channel = true;
            Item.noUseGraphic = true;
            Item.useTurn = false;

            Item.rare = ModContent.RarityType<WandRarity>();
        }
        public override bool AllowPrefix(int pre) => false;
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player p = Main.LocalPlayer;
            bool elaina = p.EMP().Elaina;
            float 渐变 = 60 - (float)Main.GameUpdateCount % 120;//60到 -60
            Color c = new(255, 48, 215, 255);
            Color c1 = new(255, 100, 239, 255);
            Color c2 = Color.Lerp(c, c1, (float)Math.Abs(渐变) / 60f);
            foreach (TooltipLine line in tooltips)
            {
                if (line.Mod == "Terraria")
                {
                    switch (line.Name)
                    {
                        case "ItemName":
                            if (elaina)
                            {
                                line.OverrideColor = c2;
                            }
                            else line.Text = GTV("Wand.CantUse");
                            break;
                        case { } name when name.StartsWith("Tooltip"):
                            if(!elaina)line.Text = string.Empty;   
                            break;
                        default:
                            line.OverrideColor = c2;
                            if (elaina)
                            {
                                if(line.Name!="Damage") line.Text = string.Empty;
                            }
                            else
                            {
                                line.Text = string.Empty;
                            }
                            break;
                    }
                }
            }
            if (elaina)
            {
                tooltips.Add(new(Mod, "Proficiency", GTV("Wand.Proficiency",
                    p.EXP().GetLv().ToString()))
                { OverrideColor = c2 });
                tooltips.Add(new(Mod, "Crit", GTV("Wand.Crit",
                    p.GetTotalCritChance(Item.DamageType).ToString()))
                { OverrideColor = c2 });
                tooltips.Add(new(Mod, "Damage", GTV("Wand.Damage",
                    ((p.GetTotalDamage(Item.DamageType).Additive - 1) * 100).ToString(".##")))
                { OverrideColor = c2 });
            }
            else
            {
                tooltips.Add(new(Mod, "Warn", GTV("Wand.Warn")) { OverrideColor = Color.Red });
            }
            
            /*
            EndBeginDrawUI(adjustToScreen:false);
            DrawInScreen(Main.screenTarget,Main.screenTarget.Size()/2f+new Vector2(500));
            EndBeginDrawUI();*/
        }
        public override void UpdateInventory(Player player)
        {
            if (player.HeldItem == Item)
            {
                if (Main.myPlayer == player.whoAmI)
                {
                    /*if (!Main.mouseLeft && !Main.mouseRight)
                    {
                        player.reuseDelay = 0;
                        player.itemTime = 0;
                        player.itemAnimation = 0;
                    }*/

                    if (Main.GameUpdateCount % 60 == 0)
                    {
                        Vector2 toward = new Vector2(1, 0);
                        float rotation = MathHelper.Lerp(0.2f, -3.34f,
                            player.ownedProjectileCounts[ModContent.ProjectileType<MagicMissleSpawner>()] / 8f);
                        toward = toward.RotatedBy(rotation);
                        
                        if (player.ownedProjectileCounts[ModContent.ProjectileType<MagicMissleSpawner>()] < 8)
                        {
                            //int id = Projectile.NewProjectile(Item.GetSource_FromAI(), 
                                //player.MountedCenter+toward*Main.rand.NextFloat(150,250), Vector2.Zero, ModContent.ProjectileType<MagicMissleSpawner>(), 0, 0);
                        }
                    }

                }
            }
        }
        public override void HoldItem(Player player)
        {

            if (player.whoAmI == Main.myPlayer && !player.mouseInterface)
            {
                //SKillTable.TryVisible();
                //Projectile.NewProjectileDirect(Entity.GetSource_FromThis(), player.position, Vector2.Zero, ModContent.ProjectileType<护盾>(), 0, 0, player.whoAmI);
            }
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(0, 0);
        }

        public override Vector2? HoldoutOrigin()
        {            
            return new Vector2(0, 0);
        }

        public override void ModifyItemScale(Player player, ref float scale)
        {
            scale = 0.5f;
            base.ModifyItemScale(player, ref scale);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddRecipeGroup(RecipeGroupID.Wood);
            recipe.AddCondition(ElainaSystem.IsElaina);
            recipe.Register();
        }
        public override bool CanUseItem(Player player)
        {
            if (!player.GetModPlayer<ElainaModplayer>().Elaina||Main.myPlayer!=player.whoAmI) return false;
            
            ElainaSkillModPlayer skillModPlayer = player.GetModPlayer<ElainaSkillModPlayer>();
            return skillModPlayer.CanUseSkill();
        }

        public override bool? UseItem(Player player)
        {
            ElainaSkillModPlayer skillModPlayer = player.GetModPlayer<ElainaSkillModPlayer>();
            Item.noUseGraphic = true;
            if (Main.myPlayer==player.whoAmI&&skillModPlayer.CanUseSkill())
            {
                skillModPlayer.UseSkill();
            }
            return base.UseItem(player);
        }
        
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            base.ModifyWeaponDamage(player, ref damage);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if(player.GetModPlayer<ActionModPlayer>().CurrentActionFrame==0)return false;
            Projectile projectile = Projectile.NewProjectileDirect(source,position , velocity, type, damage, knockback);
            return false;
            Vector2 wandPos =  player.MountedCenter+ new Vector2(1*player.direction, 0).RotatedBy(player.itemRotation)*35f;
            Vector2 toward = velocity.SafeNormalize(velocity);
            
            /*int glitter = ModContent.ProjectileType<WandGlitter>();
            //if (player.ownedProjectileCounts[glitter] < 1)
                Projectile.NewProjectile(source, position, velocity, glitter, damage, knockback);*/
            
            ElainaSkillModPlayer skillModPlayer = player.GetModPlayer<ElainaSkillModPlayer>();
            //skillModPlayer.UseSkill(0,source);


            //适配其他mod的增伤效果
            float damageScale = ((float)damage / 100);
            float skillBasicDamage = 1000;

            Vector2 wandCenter = wandPos + toward * 10;

            Vector2 validPosition = Main.MouseWorld+Main.rand.NextVector2Circular(100,100);
            FireBurstSkillHelper.GetValidPositionForFireBurst(ref validPosition,1000);

            int ProjType = ElainaSkillModPlayer.GetCurrentSkillType();
            if (ProjType == ModContent.ProjectileType<FireBurstStarProj>()|| ProjType == ModContent.ProjectileType<FinalLightning>())
            {
                wandCenter = validPosition;
            }
            else if (ProjType == ModContent.ProjectileType<MagicMissleSpawner>())
            {
                wandCenter = wandPos + toward * 100;
            }
            /*Projectile projectile = Projectile.NewProjectileDirect(source,wandCenter , velocity, ProjType, 
                (int)(skillBasicDamage*damageScale), knockback);*/
            skillModPlayer.UseSkill(source);

            //释放火柱的方法
            //Projectile projectile = Projectile.NewProjectileDirect(source,wandCenter , velocity, ModContent.ProjectileType<WaterLaser>(), (int)(skillBasicDamage*damageScale), knockback);
            /*if (projectile.ModProjectile is WaterTrail waterTrail)
            {
                waterTrail.TargetCenter = Main.MouseWorld;
            }*/
            if(player.ownedProjectileCounts[ ModContent.ProjectileType<WandLightProj>()] < 1)Projectile.NewProjectile(source, position+velocity*1, velocity, ModContent.ProjectileType<WandLightProj>(), 0, 0);
            
            //KLBasicDust.SpawnDust(wandPos,ModContent.DustType<>());
            return false; //player.ownedProjectileCounts[type] < 1;
        }
    }
}
