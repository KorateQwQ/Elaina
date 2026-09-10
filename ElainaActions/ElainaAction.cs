using System;
using KL.ActionsSystem;
using KL.Extensions;
using Terraria.DataStructures;
using 伊蕾娜.ElainaAttribute;
using 伊蕾娜.Managers;

namespace 伊蕾娜.ElainaActions;

public abstract class ElainaAction :AnimAction 
{
    protected Player owner;
    
    protected Vector2 WandCenter => ArmCenter.Floor()+new Vector2(1, 0).RotatedBy(owner.itemRotation+owner.fullRotation)*45f;
    
    protected virtual Vector2 StarCenter => WandCenter;

    protected Vector2 ArmCenter
    {
        get
        {
            Vector2 rotationCenter = owner.position.Floor() ;//+ drawInfo.rotationOrigin这东西我看了本来就是0，暂时不考虑
            Vector2 wandStartCenter = owner.MountedCenter.Floor() + new Vector2(-3 * owner.direction, -4* owner.gravDir+owner.gfxOffY)+ArmDrawingFixer.GetFrontArmTotalOffset(owner)*0.5f;

            wandStartCenter = rotationCenter + (wandStartCenter - rotationCenter).RotatedBy(owner.fullRotation);
            return wandStartCenter.Floor();
        }
    }

    
    protected virtual bool DrawWandTrail => true;

    protected virtual bool DrawActionStar => true;

    protected virtual int WandTrailLength => 30;

    private Vector2[] wandTrailPositions = Array.Empty<Vector2>();

    private int wandTrailPointCount;

    protected static Texture2D wandTex; 

    protected ElainaAction(int totalFrame) : base(totalFrame)
    {
        wandTex ??= ModContent.Request<Texture2D>("伊蕾娜/Items/魔杖", AssetRequestMode.ImmediateLoad).Value;
    }

    public override void OnStart(ActionModPlayer actionPlayer)
    {
        owner = actionPlayer.Player;
        ResetWandTrail();
        base.OnStart(actionPlayer);
    }

    public override void Update(ActionModPlayer actionPlayer, int actionFrame, float actionProgress)
    {
        base.Update(actionPlayer, actionFrame, actionProgress);
        SyncItemRotationToArmRotation();

    }

    public override void Draw(ActionModPlayer actionPlayer, ref PlayerDrawSet drawInfo, int actionFrame, float actionProgress,
        ActionDrawLayerType drawLayerType)
    {

        if (drawInfo.shadow == 0)
        {
            if (drawLayerType == ActionDrawLayerType.UnderArm)
            {
                UpdateWandTrail();
                DrawWand(ref drawInfo);
            }
            if(drawLayerType==ActionDrawLayerType.OverPlayer)
            {
                base.Draw(actionPlayer, ref drawInfo, actionFrame, actionProgress, drawLayerType);
                DrawAutoWandTrail(actionProgress);
                if (DrawActionStar)
                {
                    DrawStar(ref drawInfo,actionProgress);
                }
                if (DrawWandTrail || DrawActionStar)
                {
                    EndBeginDraw(0,1);
                }

                return;
            }
        }

        base.Draw(actionPlayer, ref drawInfo, actionFrame, actionProgress, drawLayerType);
    }

    private void ResetWandTrail()
    {
        if (wandTrailPositions.Length != WandTrailLength)
        {
            wandTrailPositions = new Vector2[WandTrailLength];
        }

        Array.Clear(wandTrailPositions);
        wandTrailPointCount = 0;
    }

    private void UpdateWandTrail()
    {
        if (!DrawWandTrail || wandTrailPositions.Length == 0||Main.gamePaused)
        {
            return;
        }

        for (int i = wandTrailPositions.Length - 1; i > 0; i--)
        {
            wandTrailPositions[i] = wandTrailPositions[i - 1];
        }

        wandTrailPositions[0] = WandCenter;
        wandTrailPointCount = Math.Min(wandTrailPointCount + 1, wandTrailPositions.Length);
    }

    // 绘制魔杖，魔杖贴图是斜着45度所以要做特殊处理，此绘制方式为从玩家肩膀位置指向鼠标位置
    protected virtual void DrawWand(ref PlayerDrawSet drawInfo)
    {
        SpriteEffects spriteEffects = SpriteEffects.None;
        if (owner.direction == -1)
        {
            spriteEffects |= SpriteEffects.FlipHorizontally;
        }

        //这里原版已经帮我处理了好像，不需要弄了
        if (owner.gravDir == -1f)
        {
            //spriteEffects |= SpriteEffects.FlipVertically;
        }

        Vector2 wandStartCenter = ArmCenter;
        float wandRotation = (WandCenter -wandStartCenter).ToRotation();
        Vector2 wandOrigin = owner.direction == 1
            ? new Vector2(0, wandTex.Height)
            : new Vector2(wandTex.Width, wandTex.Height);
        float wandRotationOffset = owner.direction == 1
            ? MathHelper.PiOver4
            : MathHelper.PiOver4 * 3f;

        //魔杖到手的距离
        float distance = 10f;
        drawInfo.DrawDataCache.Add(new DrawData(
            wandTex,
            wandStartCenter+ new Vector2(1, 0).RotatedBy(owner.itemRotation+owner.fullRotation)*distance-Main.screenPosition,
            null,
            Color.White,
            wandRotation+ wandRotationOffset,
            wandOrigin,
            new Vector2(0.5f),
            spriteEffects,
            0)
        {
            ignorePlayerRotation = true
        });

    }
    private void DrawAutoWandTrail(float actionProgress)
    {
        if (!DrawWandTrail || wandTrailPointCount < 2)
        {
            return;
        }

        Vector2[] trailPositions = new Vector2[wandTrailPointCount];
        Array.Copy(wandTrailPositions, trailPositions, wandTrailPointCount);

        ElainaTrailVisualStyle.DrawWandTrail(trailPositions, actionProgress);
    }

    void SyncItemRotationToArmRotation()
    {
        //float rotation = (Main.MouseWorld - player.Center).ToRotation();
        float rotation = owner.compositeFrontArm.rotation * owner.gravDir;
        float armTextureDirection = owner.direction == 1
            ? 1.8f
            : MathHelper.Pi - 1.8f;
        Vector2 toward = new Vector2(1, 0).RotatedBy((rotation + armTextureDirection) * owner.gravDir);
        //player.itemAnimation = 1;
        //player.SetCompositeArmFront(true,Player.CompositeArmStretchAmount.Full,rotation -1.8f*player.direction);
        //if(towardToMouse) player.direction = toward.X < 0 ? -1 : 1;//玩家朝向根据鼠标

        toward.SafeNormalize(Vector2.One);
        Vector2 itemOffset = new Vector2(-2, -2 * owner.gravDir);
        owner.itemLocation = owner.MountedCenter + itemOffset + ArmDrawingFixer.GetFrontArmTotalOffset(owner) + toward * 7 * owner.direction;
        owner.itemRotation = toward.ToRotation();//武器朝向
    }
 
    protected virtual void DrawStar(ref PlayerDrawSet drawInfo,float actionProgress)
    {
        ElainaTrailVisualStyle.DrawDefaultStar(ref drawInfo, StarCenter.Floor(), actionProgress);
    }
}
