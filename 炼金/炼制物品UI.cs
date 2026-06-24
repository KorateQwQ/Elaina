using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;
using 伊蕾娜.Items;
using 伊蕾娜.Items.消耗品;

namespace 伊蕾娜.炼金
{
    public class 配方表UI : UIState
    {
        public static bool Visible = false;
        public UIPanel panel2;//UI画板
        public Item[] inv = new Item[10];
        static Asset<Texture2D> texture = ModContent.Request<Texture2D>("伊蕾娜/炼金/Icon_Locked");
        public override void OnInitialize()//初始化UI面板
        {
            inv[0] = new Item();
            inv[1] = new Item();
            inv[2] = new Item();
            panel2 = new UIPanel();
            //panel2.BackgroundColor = new(255, 255, 255, 255);
            //panel2.BorderColor = new Color(255, 255, 255, 255);
            //设置面板的宽度
            panel2.Width.Set(450, 0f);
            //设置面板的高度
            panel2.Height.Set(500f, 0f);
            //设置面板距离屏幕最左边的距离
            //panel.Left.Set(Main.screenWidth/2f-400, 0);
            panel2.Left.Set(Main.screenWidth / 2f + 200, 0);
            //设置面板距离屏幕最上端的距离
            panel2.Top.Set(0, 0.2f);
            //panel.Top.Set(Main.screenHeight/2f- 300, 0);
            //将这个面板注册到UIState
            Append(panel2);
        }
        public override void Update(GameTime gameTime)
        {

            base.Update(gameTime);
        }
        public override void Draw(SpriteBatch spriteBatch)
        {

            panel2.Left.Set(Main.screenWidth / 2f + 200, 0);
            panel2.Top.Set(Main.screenHeight / 2f - 300, 0f);
            panel2.Recalculate();
            base.Draw(spriteBatch);
            克隆配方(spriteBatch);
            究极魔力药水配方(spriteBatch);
            for (int i = 0; i < 4; i++)
                各色属性药水配方(spriteBatch, i);
            小动物药水配方(spriteBatch);
        }
        void 克隆配方(SpriteBatch spriteBatch)
        {
            inv[0] = new Item(0, 9999);
            inv[1] = new Item(0, 1);
            inv[2] = new Item(ItemID.Obsidian, 1);
            int context = 1;
            bool mouseText = false;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    Main.inventoryScale = 0.755f;
                    int num = (int)(Main.screenWidth / 2f + 220 + i * 66);
                    int num2 = (int)(Main.screenHeight / 2f - 280);
                    int slot = i + j * 10;

                    if (i == 0)
                    {

                        context = 1;
                        ItemSlot.Draw(spriteBatch, inv, context, slot, new Vector2(num, num2));
                        spriteBatch.Draw(texture.Value, new Vector2(num + 19, num2 + 17), new Rectangle(0, 0, 24, 34), Color.White, 0, new Vector2(12, 17), 0.7f, 0, 0);
                        spriteBatch.DrawString(FontAssets.ItemStack.Value, "=", new Vector2(num + 46, num2 + 10), Color.White);
                        spriteBatch.DrawString(FontAssets.ItemStack.Value, "x", new Vector2(num + 8, num2 + 20), Color.White);
                    }
                    else if (i == 1)
                    {
                        context = 4;
                        ItemSlot.Draw(spriteBatch, inv, context, slot, new Vector2(num, num2));
                        spriteBatch.Draw(texture.Value, new Vector2(num + 19, num2 + 17), new Rectangle(0, 0, 24, 34), Color.White, 0, new Vector2(12, 17), 0.7f, 0, 0);
                        spriteBatch.DrawString(FontAssets.ItemStack.Value, "+", new Vector2(num + 46, num2 + 10), Color.White);
                    }
                    else
                    {
                        context = 3;
                        ItemSlot.Draw(spriteBatch, inv, context, slot, new Vector2(num, num2));
                        if (i != 5)
                            spriteBatch.DrawString(FontAssets.ItemStack.Value, "x", new Vector2(num + 8, num2 + 20), Color.White);
                    }
                    if (Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0f, 0f, num, num2, (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale, (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.LocalPlayer.mouseInterface = true;
                        //ItemSlot.Handle(inv, context, slot);
                        //ItemSlot.OverrideHover(inv, context, slot);
                        ItemSlot.MouseHover(inv, context, slot);//显示物品属性
                        if (i < 2)
                            mouseText = true;

                    }

                }
            }
            if (mouseText)
            {
                string text = "Any \"Material\" that is neither a weapon nor equipment";
                if (Language.ActiveCulture.Name == "zh-Hans")
                    text = "任何非武器，装备的“材料”类物品";
                spriteBatch.DrawString(FontAssets.MouseText.Value, text, Main.MouseScreen + new Vector2(25, -5), Color.Red);
            }
        }
        public void 究极魔力药水配方(SpriteBatch spriteBatch)
        {
            inv[0] = new Item(ModContent.ItemType<究极魔力药水>(), 1);
            inv[1] = new Item(ItemID.LesserManaPotion, 1);
            inv[2] = new Item(ItemID.DirtBlock, 3);
            inv[3] = new Item(ItemID.ClayBlock, 3);
            inv[4] = new Item(ItemID.StoneBlock, 3);
            inv[5] = new Item(ItemID.MudBlock, 3);
            int context = 1;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    Main.inventoryScale = 0.755f;
                    int num = (int)(Main.screenWidth / 2f + 220 + i * 66);
                    int num2 = (int)(Main.screenHeight / 2f - 280 + 56);
                    int slot = i + j * 10;
                    if (i == 0)
                    {
                        context = 1;
                        spriteBatch.DrawString(FontAssets.ItemStack.Value, "=", new Vector2(num + 46, num2 + 10), Color.White);
                    }
                    else if (i == 1)
                    {
                        context = 4;
                        spriteBatch.DrawString(FontAssets.ItemStack.Value, "+", new Vector2(num + 46, num2 + 10), Color.White);
                    }
                    else
                    {
                        context = 3;
                        if (i != 5)
                            spriteBatch.DrawString(FontAssets.ItemStack.Value, "/", new Vector2(num + 46, num2 + 10), Color.White);
                    }
                    ItemSlot.Draw(spriteBatch, inv, context, slot, new Vector2(num, num2));

                    if (Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0f, 0f, num, num2, (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale, (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.LocalPlayer.mouseInterface = true;
                        //ItemSlot.Handle(inv, context, slot);
                        //ItemSlot.OverrideHover(inv, context, slot);
                        ItemSlot.MouseHover(inv, context, slot);//显示物品属性

                    }

                }
            }
            //spriteBatch.DrawString(FontAssets.ItemStack.Value, "6666666", Main.MouseScreen, Color.White);

        }
        public void 各色属性药水配方(SpriteBatch spriteBatch, int type)
        {
            int context = 1;

            inv[0] = new Item(ModContent.ItemType<潋滟药水>(), 1);
            inv[1] = new Item(ItemID.MagicPowerPotion, 1);
            inv[2] = new Item(ItemID.FallenStar, 10);
            inv[3] = new Item(ItemID.Coral, 10);
            if (type == 1)
            {
                inv[0] = new Item(ModContent.ItemType<焰火药水>(), 1);
                inv[3] = new Item(ItemID.Fireblossom, 10);
            }
            if (type == 2)
            {
                inv[0] = new Item(ModContent.ItemType<寒霜药水>(), 1);
                inv[3] = new Item(ItemID.Shiverthorn, 10);
            }
            if (type == 3)
            {
                inv[0] = new Item(ModContent.ItemType<雷引药水>(), 1);
                inv[1] = new Item(ModContent.ItemType<潋滟药水>(), 1);
                inv[2] = new Item(ModContent.ItemType<焰火药水>(), 1);
                inv[3] = new Item(ModContent.ItemType<寒霜药水>(), 1);
            }
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    Main.inventoryScale = 0.755f;
                    int num = (int)(Main.screenWidth / 2f + 220 + i * 66);
                    int num2 = (int)(Main.screenHeight / 2f - 280) + 56 * (type + 2);
                    int slot = i + j * 10;
                    if (i == 0)
                    {
                        spriteBatch.DrawString(FontAssets.ItemStack.Value, "=", new Vector2(num + 46, num2 + 10), Color.White);
                    }
                    else if (i == 1 && type != 3)
                    {
                        context = 4;//主
                        spriteBatch.DrawString(FontAssets.ItemStack.Value, "+", new Vector2(num + 46, num2 + 10), Color.White);
                    }
                    else
                    {
                        context = 3;
                        if (i != 3)
                            spriteBatch.DrawString(FontAssets.ItemStack.Value, "+", new Vector2(num + 46, num2 + 10), Color.White);
                    }
                    if (Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0f, 0f, num, num2, (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale, (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.LocalPlayer.mouseInterface = true;
                        //ItemSlot.Handle(inv, context, slot);
                        //ItemSlot.OverrideHover(inv, context, slot);
                        ItemSlot.MouseHover(inv, context, slot);//显示物品属性
                    }

                    ItemSlot.Draw(spriteBatch, inv, context, slot, new Vector2(num, num2));
                }
            }
        }
        void 小动物药水配方(SpriteBatch spriteBatch)
        {
            inv[0] = new Item(ModContent.ItemType<小动物药水>(), 1);
            inv[1] = new Item(ItemID.GenderChangePotion, 1);
            inv[2] = new Item(ItemID.FallenStar, 10);
            int context = 1;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    Main.inventoryScale = 0.755f;
                    int num = (int)(Main.screenWidth / 2f + 220 + i * 66);
                    int num2 = (int)(Main.screenHeight / 2f - 280 + 56 * 6);
                    int slot = i + j * 10;

                    if (i == 0)
                    {

                        context = 1;
                        ItemSlot.Draw(spriteBatch, inv, context, slot, new Vector2(num, num2));
                        //spriteBatch.Draw(texture.Value, new Vector2(num + 19, num2 + 17), new Rectangle(0, 0, 24, 34), Color.White, 0, new Vector2(12, 17), 0.7f, 0, 0);
                        spriteBatch.DrawString(FontAssets.ItemStack.Value, "=", new Vector2(num + 46, num2 + 10), Color.White);
                        //spriteBatch.DrawString(FontAssets.ItemStack.Value, "x", new Vector2(num + 8, num2 + 20), Color.White);
                    }
                    else if (i == 1)
                    {
                        context = 4;
                        ItemSlot.Draw(spriteBatch, inv, context, slot, new Vector2(num, num2));
                        spriteBatch.DrawString(FontAssets.ItemStack.Value, "+", new Vector2(num + 46, num2 + 10), Color.White);
                    }
                    else
                    {
                        context = 3;
                        ItemSlot.Draw(spriteBatch, inv, context, slot, new Vector2(num, num2));

                    }
                    if (Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0f, 0f, num, num2, (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale, (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.LocalPlayer.mouseInterface = true;
                        //ItemSlot.Handle(inv, context, slot);
                        //ItemSlot.OverrideHover(inv, context, slot);
                        ItemSlot.MouseHover(inv, context, slot);//显示物品属性

                    }

                }
            }

        }

    }

    public class 炼制物品UI : UIState
    {
        public static bool Visible = false;

        public UIPanel panel;//UI画板

        //public UIElement ItemSlot;//物品槽
        public UIImageButton ComfirmButton;//确认炼制按钮（开始炼制）
        public UIImageButton CancelButton;//取消按钮，并取出所有物品
        public Item[] inv = new Item[3];
        public override void OnInitialize()//初始化UI面板
        {
            inv[0] = new Item();
            inv[1] = new Item();
            inv[2] = new Item();
            panel = new UIPanel();
            panel.BackgroundColor = new(0, 0, 0, 0);
            panel.BorderColor = new Color(0, 0, 0, 0);
            //设置面板的宽度
            panel.Width.Set(100, 0f);
            //设置面板的高度
            panel.Height.Set(80f, 0f);
            //设置面板距离屏幕最左边的距离
            //panel.Left.Set(Main.screenWidth/2f-400, 0);
            panel.Left.Set(0, 0.5f);
            //设置面板距离屏幕最上端的距离
            panel.Top.Set(0, 0.5f);
            //panel.Top.Set(Main.screenHeight/2f- 300, 0);
            //将这个面板注册到UIState
            Append(panel);
            ComfirmButton = new UIImageButton(ModContent.Request<Texture2D>("伊蕾娜/炼金/Icon_Locked"));
            ComfirmButton.Width.Set(24, 0);
            ComfirmButton.Height.Set(24, 0);
            ComfirmButton.Left.Set(60, 0.5f);
            ComfirmButton.Top.Set(0, 0.5f);
            ComfirmButton.OnLeftClick += ComfirmButton_OnClick;
            Append(ComfirmButton);
            base.OnInitialize();

        }

        private void ComfirmButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
        {
            配方表UI.Visible = !配方表UI.Visible;
        }


        public override void Update(GameTime gameTime)
        {
            //设置面板距离屏幕最上端的距离
            if (!Main.LocalPlayer.active)
                Visible = false;

            base.Update(gameTime);
        }
        public override void Draw(SpriteBatch spriteBatch)
        {
            ComfirmButton.Left.Set(Main.screenWidth / 2f + 70, 0f);
            ComfirmButton.Top.Set(Main.screenHeight / 2f - (190), 0f);
            //ComfirmButton.SetVisibility(1, 0.5f);
            ComfirmButton.Recalculate();
            int context = 4;//4是可以存放和取出道具的格子
            Main.inventoryScale = 0.755f;
            if (Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0f, 0f, 73f, Main.instance.invBottom, 560f * Main.inventoryScale, 224f * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
                Main.LocalPlayer.mouseInterface = true;
            Asset<Texture2D> texture = ModContent.Request<Texture2D>("伊蕾娜/炼金/炼制物品UI");
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            spriteBatch.Draw(texture.Value, new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f) + new Vector2(0, -120), new Rectangle(0, 0, 154, 73), Color.White,
                0, new Vector2(texture.Width(), texture.Height()) * 0.5f, 2f, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.Default, RasterizerState.CullNone, null, Main.UIScaleMatrix);
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 1; j++)
                {
                    context = 3;
                    if (i == 1)
                    {
                        Main.inventoryScale = 1.755f;
                        context = 4;
                    }
                    else
                        Main.inventoryScale = 0.755f;
                    int num = (int)(new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f).X + i * 56 - 100);
                    if (i == 2)
                        num += 50;
                    int num2 = (int)(new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f).Y - 180);
                    if (i != 1)
                        num2 += 28;
                    int slot = i + j * 10;
                    if (Utils.FloatIntersect(Main.mouseX, Main.mouseY, 0f, 0f, num, num2, (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale, (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale) && !PlayerInput.IgnoreMouseInterface)
                    {
                        Main.LocalPlayer.mouseInterface = true;
                        ItemSlot.Handle(inv, context, slot);
                        //ItemSlot.OverrideHover(inv, context, slot);
                        //ItemSlot.MouseHover(inv, context, slot);//显示物品属性
                    }

                    ItemSlot.Draw(spriteBatch, inv, context, slot, new Vector2(num, num2));
                }
            }
            //物品栏绘制覆盖
            /*
            Main.inventoryScale = 0.85f;
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    int num7 = (int)(20f + (float)(i * 56) * Main.inventoryScale);
                    int num8 = (int)(20f + (float)(j * 56) * Main.inventoryScale);
                    int num9 = i + j * 10;
                    if (Main.mouseX >= num7 && (float)Main.mouseX <= (float)num7 + (float)TextureAssets.InventoryBack.Width() * Main.inventoryScale && Main.mouseY >= num8 && (float)Main.mouseY <= (float)num8 + (float)TextureAssets.InventoryBack.Height() * Main.inventoryScale && !PlayerInput.IgnoreMouseInterface)
                    {
                        if (Main.HoverItem.stack > 0)
                        {
                            ItemSlot.Draw(spriteBatch, ref Main.HoverItem, context, new Vector2(num7, num8));
                        }

                    }

                }
            }*/
            base.Draw(spriteBatch);
        }
    }


    public class 炼制物品UISystem : ModSystem
    {
        public 炼制物品UI UI;
        public 配方表UI RecipeUI;
        internal UserInterface UIInterface;
        internal UserInterface SkillUIUserInterface;

        public override void Load()
        {
            UI = new 炼制物品UI();//实例化
            RecipeUI = new 配方表UI();
            if (Main.netMode != NetmodeID.Server)
            {
                UI.Activate();//初始化
                RecipeUI.Activate();
            }
            UIInterface = new UserInterface();
            SkillUIUserInterface = new UserInterface();
            UIInterface.SetState(UI);
            base.Load();
        }
        public override void UpdateUI(GameTime gameTime)
        {
            base.UpdateUI(gameTime);
            //当Visible为true时（当UI开启时）
            if (炼制物品UI.Visible)
                //如果exampleUserInterface不是null（非空）就执行Update方法
                UIInterface?.Update(gameTime);
            if (配方表UI.Visible)
                UIInterface?.Update(gameTime);
        }
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            //寻找一个名字为Vanilla: Mouse Text的绘制层，也就是绘制鼠标字体的那一层，并且返回那一层的索引
            int MouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
            //寻找到索引时
            if (MouseTextIndex != -1)
            {
                //往绘制层集合插入一个成员，第一个参数是插入的地方的索引，第二个参数是绘制层
                layers.Insert(MouseTextIndex, new LegacyGameInterfaceLayer(
                   //这里是绘制层的名字
                   "Elaina: SystemUI",
                   //这里是匿名方法
                   delegate
                   {
                       //当Visible开启时（当UI开启时）
                       if (炼制物品UI.Visible)
                           //绘制UI（运行exampleUI的Draw方法）
                           UI.Draw(Main.spriteBatch);
                       if (配方表UI.Visible)
                           RecipeUI.Draw(Main.spriteBatch);
                       return true;
                   },
                   //这里是绘制层的类型
                   InterfaceScaleType.UI)
               );
            }
            base.ModifyInterfaceLayers(layers);
        }
    }
}
