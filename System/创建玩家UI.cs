using Homura;
using Humanizer;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Audio;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace 伊蕾娜.System
{
    public class 创建玩家UI : ModSystem
    {
        [JITWhenModsEnabled("Homura")]
        public static void CloseHomuraUI()
        {
            if (!selectElaina && Homura.UI.创建玩家UI.SelectHomuUi.choose && SelectHomuUi.choose)
            {
                SelectHomuUi.choose = false;
                createElaina = false;
            }
            else if (Homura.UI.创建玩家UI.SelectHomuUi.choose && SelectHomuUi.choose)
            {
                Homura.UI.创建玩家UI.SelectHomuUi.choose = false;
                Homura.UI.创建玩家UI.createHomura = false;
                Homura.UI.创建玩家UI.createMoeMura = false;
            }
        }

        public static bool selectElaina = false;

        public static bool createElaina = false;
        ElainaModplayer ELP => Main.LocalPlayer.GetModPlayer<ElainaModplayer>();
        static Asset<Texture2D> ElainaIcon;
        static Asset<Texture2D> IconBorder;

        public class SelectHomuUi : UIElement
        {
            private Asset<Texture2D> _texture;

            private float _visibilityActive = 1f;
            private float _visibilityInactive = 0.4f;
            private Asset<Texture2D> _borderTexture;
            public static bool choose = false;
            int time = 0;
            public SelectHomuUi(Asset<Texture2D> texture, Asset<Texture2D> texture3)
            {
                _texture = texture;
                _borderTexture = texture3;

                //Width.Set(texture2.Width(), 0f);
                //Height.Set(texture2.Height(), 0f);
            }

            public void SetHoverImage(Asset<Texture2D> texture)
            {
                _borderTexture = texture;
            }

            public void SetImage(Asset<Texture2D> texture)
            {
                _texture = texture;
                Width.Set(texture.Width(), 0f);
                Height.Set(texture.Height(), 0f);
            }

            public float FrameTime(float 最小值, float 最大值, int 所需时间)
            {
                float divide = 所需时间 / 最大值;
                float result = ((time++) % 所需时间);
                if (time > 所需时间) time = 0;
                if (result > 所需时间 / 2f)
                {
                    result = 所需时间 - result;
                }
                //Main.NewText(result);
                float result2 = MathHelper.Lerp(最小值, 最大值, result / (所需时间 / 2f));
                return result2;

            }
            protected override void DrawSelf(SpriteBatch spriteBatch)
            {
                if (ModLoader.HasMod("Homura"))
                {
                    CloseHomuraUI();

                }

                //Width.Set(_texture2.Width(), 0f);
                //Height.Set(_texture2.Height(), 0f);
                //Recalculate();
                //Top = StyleDimension.FromPercent(-0.25f);
                // Left = StyleDimension.FromPercent(-0.59f);
                float scaleW = Main.screenWidth / 1920f;
                float scaleH = Main.screenHeight / 1080f;
                Top = StyleDimension.FromPixels(-1040 * scaleH + 630);//StyleDimension.FromPixels(220f * scaleH);
                Left = StyleDimension.FromPixels(-950 * scaleW - 160f);
                if (ModLoader.HasMod("Homura"))
                {
                    Top = StyleDimension.FromPixels(-1040 * scaleH + 630);//StyleDimension.FromPixels(220f * scaleH);
                    Left = StyleDimension.FromPixels(-950 * scaleW - 40f);
                }
                //StyleDimension.FromPixels(-1110*Main.screenWidth/1920f + (1 - (Main.screenWidth / 1920f)) * -160);//1712(-1010), 1189
                //Console.WriteLine((1 - (Main.screenWidth / 1920f)));
                Recalculate();
                CalculatedStyle dimensions = GetDimensions();

                //Console.WriteLine(Main.GameUpdateCount);
                Color c = c = Color.White;

                spriteBatch.Draw(_texture.Value, dimensions.Position(), c * (choose ? _visibilityActive : _visibilityInactive));
                if (_borderTexture != null && base.IsMouseHovering)
                {
                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin((SpriteSortMode)0, BlendState.Additive, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);

                    for (int i = 0; i < 3; i++)
                        spriteBatch.Draw(_borderTexture.Value, dimensions.Position(),new Color(255,100,150) * FrameTime(0.5f, 1f, 120));

                    Main.spriteBatch.End();
                    Main.spriteBatch.Begin(0, BlendState.AlphaBlend, Main.DefaultSamplerState,
                        DepthStencilState.None, RasterizerState.CullNone, null, Main.UIScaleMatrix);
                }
            }

            public override void MouseOver(UIMouseEvent evt)
            {
                base.MouseOver(evt);
                SoundEngine.PlaySound(SoundID.MenuTick);
            }

            public override void MouseOut(UIMouseEvent evt)
            {
                base.MouseOut(evt);
            }

            public void SetVisibility(float whenActive, float whenInactive)
            {
                _visibilityActive = MathHelper.Clamp(whenActive, 0f, 1f);
                _visibilityInactive = MathHelper.Clamp(whenInactive, 0f, 1f);
            }
            public override void LeftClick(UIMouseEvent evt)
            {

                choose = !choose;
                if (choose)
                {
                    if (ModLoader.HasMod("Homura"))
                    {
                        selectElaina = true;
                        CloseHomuraUI();
                        selectElaina = false;
                    }
                    SoundEngine.PlaySound(SoundID.MenuOpen);
                    createElaina = true;
                }
                else
                {
                    selectElaina = false;

                    createElaina = false;
                    SoundEngine.PlaySound(SoundID.MenuClose);

                }

                base.LeftClick(evt);
            }

        }

        public override void Load()
        {
            On_UICharacterSelect.NewCharacterClick += On_UICharacterSelect_NewCharacterClick;
            On_UICharacterCreation.FinishCreatingCharacter += On_UICharacterCreation_FinishCreatingCharacter;
            On_UICharacterCreation.Draw += On_UICharacterCreation_Draw;
            On_UICharacterCreation.MakeBackAndCreatebuttons += On_UICharacterCreation_MakeBackAndCreatebuttons;
            ElainaIcon = ModContent.Request<Texture2D>("伊蕾娜/System/Icon", AssetRequestMode.ImmediateLoad);
            IconBorder = ModContent.Request<Texture2D>("伊蕾娜/System/Border", AssetRequestMode.ImmediateLoad);
            base.Load();
        }

        private void On_UICharacterCreation_MakeBackAndCreatebuttons(On_UICharacterCreation.orig_MakeBackAndCreatebuttons orig, UICharacterCreation self, UIElement outerContainer)
        {
            orig(self, outerContainer);
            /*UITextPanel<LocalizedText> uITextPanel2 = new UITextPanel<LocalizedText>(Language.GetText("UI.Create"), 0.7f, large: true)
            {
                Width = StyleDimension.FromPixelsAndPercent(-10f, 0.5f),
                Height = StyleDimension.FromPixels(50f),
                VAlign = 1f,
                HAlign = 1f,
                Top = StyleDimension.FromPixels(15f),
                Left = StyleDimension.FromPixels(-260f),
            };
            uITextPanel2.OnLeftMouseDown += Click_NamingAndCreating;
            uITextPanel2.SetSnapPoint("Create", 0);
            outerContainer.Append(uITextPanel2);*/

            SelectHomuUi ChooseHomura = new SelectHomuUi(ElainaIcon, IconBorder)
            {
                Width = StyleDimension.FromPixels(96f),
                Height = StyleDimension.FromPixels(96f),
                VAlign = 1f,
                HAlign = 1f,
                Top = StyleDimension.FromPixels(-225f),
                Left = StyleDimension.FromPixels(-1010f),
            };
            SelectHomuUi.choose = false;
            //ChooseHomura.mouseo
            ChooseHomura.SetSnapPoint("Create", 0);
            self.Append(ChooseHomura);

        }

        private void On_UICharacterCreation_Draw(On_UICharacterCreation.orig_Draw orig, UICharacterCreation self, SpriteBatch spriteBatch)
        {
            if (self.IsMouseHovering)
            {
                //Console.WriteLine("click");
            }

            orig(self, spriteBatch);
        }

        private void On_UICharacterCreation_FinishCreatingCharacter(On_UICharacterCreation.orig_FinishCreatingCharacter orig, UICharacterCreation self)
        {
            //最终角色结算

            if (createElaina)
            {
                Main.PendingPlayer.GetModPlayer<ElainaModplayer>().Elaina = true;
            }
            orig(self);
        }

        private void On_UICharacterSelect_NewCharacterClick(On_UICharacterSelect.orig_NewCharacterClick orig, UICharacterSelect self, UIMouseEvent evt, UIElement listeningElement)
        {
            createElaina = false;
            orig(self, evt, listeningElement);

        }
    }
}
