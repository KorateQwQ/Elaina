using KL.Drawing.ThreeD;
using KL.Extensions;
using Terraria.GameContent;

namespace 伊蕾娜.ElainaModSkills.Skills.Lightning;

public class LightningModelTest : ElainaBasicProjectile
{
    static ObjModel lightningModel;
    static ObjModel lightningModel2;

    static Effect lightningEffect;

    private float rotationY;
    private float rotationZ;
    private float rotationX;
    private Vector3 scale;
    private int type = 0;
    public override void Load()
    {
        lightningModel ??= ObjModel.Load("伊蕾娜.Models.Lightning_1");
        lightningModel2 ??= ObjModel.Load("伊蕾娜.Models.Lightning_2");
        lightningEffect = ModContent.Request<Effect>("伊蕾娜/Effects/Content/ThreeD/LightningEffect", AssetRequestMode.ImmediateLoad).Value;
        
        base.Load();
    }

    public override void SetDefaults()
    {
        Projectile.timeLeft = 20;
        rotationY = Main.rand.NextFloat(-6.28f, 6.28f);
        float offset = 0.3f;
        rotationZ = Main.rand.NextFloat(-offset, offset);
        rotationX = Main.rand.NextFloat(-offset, offset);
        scale = new Vector3(Main.rand.NextFloat(0.8f, 1.2f), Main.rand.NextFloat(2,2.5f), 1);
        type = Main.rand.Next(0,2);
        base.SetDefaults();
    }

    public override void AI()
    {
        base.AI();
    }

    public override bool ShouldUpdatePosition()
    {
        return false;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (lightningModel2 == null || lightningEffect == null)
        {
            return false;
        }
        EndBeginDraw3D(2);
        if(type==0)DrawMainLightning(new Vector2(scale.X,scale.Y),new Vector3(0,0,0),Projectile.Center);
        else if(type==1)DrawMainLightning2(new Vector2(scale.X*1.5f,scale.Y*1.5f),new Vector3(0,0,0),Projectile.Center);
        
        DrawMainLightning(new Vector2(scale.X*0.3f,scale.Y*0.7f),new Vector3(0.3f,-1,0.3f),Projectile.Center);
        DrawMainLightning2(new Vector2(scale.X*1f,scale.Y*0.8f),new Vector3(-0.3f,2,-0.3f),Projectile.Center);

        EndBeginDraw();
        return false;
    }
    
    void DrawMainLightning(Vector2 modelScale,Vector3 rotation,Vector2 position)
    {
        
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        VertexBuffer vertexBuffer = lightningModel2.GetOrCreateVertexBuffer(gd);
        Vector3 pivot = new Vector3(-2.4601f, -211.2323f, 0f);

        Matrix world = Matrix.CreateTranslation(-pivot) *
                       Matrix.CreateScale(new Vector3(modelScale,1)) *
                       Matrix.CreateRotationY(rotation.Y+rotationY) *
                       Matrix.CreateRotationZ(-MathHelper.Pi+rotation.Z) *
                       Matrix.CreateRotationX(rotation.X) *
                       Matrix.CreateTranslation(new Vector3(position, 0f));
        Texture2D whiteTexture = TextureAssets.MagicPixel.Value;
        Texture2D dissolveNoise = PerLinNoiseX;//ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/Eff_noise_40_02", AssetRequestMode.ImmediateLoad).Value;
        const int lifeTime = 20;
        const int dissolveStartTime = 5;
        int elapsedTime = lifeTime - Projectile.timeLeft;
        float dissolveProgress = MathHelper.Clamp(
            (elapsedTime - dissolveStartTime) / (float)(lifeTime - dissolveStartTime), 0f, 1f);


        lightningEffect.SetValue("uWorld", world);
        lightningEffect.SetValue("uViewProjection", GraphicsUtils.GetVPMatrix());
        lightningEffect.SetValue("uBaseColor", new Color(100, 200, 255,255).ToVector4() * 1.3f);
        lightningEffect.SetValue("uDissolveNoiseScale", new Vector2(1f, 10f)*0.1f);
        lightningEffect.SetValue("uDissolveThreshold", dissolveProgress * 1.05f);
        lightningEffect.SetValue("uDissolveEdgeWidth", 0.1f);
        lightningEffect.SetValue("uDissolveEdgeColor", Color.White.ToVector4());
        
        gd.Textures[0] = whiteTexture;
        gd.Textures[1] = dissolveNoise;

        gd.SetVertexBuffer(vertexBuffer);

        lightningEffect.CurrentTechnique.Passes[0].Apply();
        gd.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount);
    }
    
    void DrawMainLightning2(Vector2 modelScale,Vector3 rotation,Vector2 position)
    {
        
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        VertexBuffer vertexBuffer = lightningModel.GetOrCreateVertexBuffer(gd);
        Vector3 pivot = new Vector3(-0.6198f, -103.9256f, 0f);

        Matrix world = Matrix.CreateTranslation(-pivot) *
                       Matrix.CreateScale(new Vector3(modelScale,1)) *
                       Matrix.CreateRotationY(rotation.Y+rotationY) *
                       Matrix.CreateRotationZ(-MathHelper.Pi+rotation.Z) *
                       Matrix.CreateRotationX(rotation.X) *
                       Matrix.CreateTranslation(new Vector3(position, 0f));
        Texture2D whiteTexture = TextureAssets.MagicPixel.Value;
        Texture2D dissolveNoise = PerLinNoiseX;//ModContent.Request<Texture2D>("KL/Effects/Tex/Noise/Eff_noise_40_02", AssetRequestMode.ImmediateLoad).Value;
        const int lifeTime = 25;
        const int dissolveStartTime = 7;
        int elapsedTime = lifeTime - Projectile.timeLeft;
        float dissolveProgress = MathHelper.Clamp(
            (elapsedTime - dissolveStartTime) / (float)(lifeTime - dissolveStartTime), 0f, 1f);


        lightningEffect.SetValue("uWorld", world);
        lightningEffect.SetValue("uViewProjection", GraphicsUtils.GetVPMatrix());
        lightningEffect.SetValue("uBaseColor", new Color(100, 200, 255,255).ToVector4() * 1.3f);
        lightningEffect.SetValue("uDissolveNoiseScale", new Vector2(1f, 10f)*0.1f);
        lightningEffect.SetValue("uDissolveThreshold", dissolveProgress * 1.05f);
        lightningEffect.SetValue("uDissolveEdgeWidth", 0.1f);
        lightningEffect.SetValue("uDissolveEdgeColor", Color.White.ToVector4());
        
        gd.Textures[0] = whiteTexture;
        gd.Textures[1] = dissolveNoise;

        gd.SetVertexBuffer(vertexBuffer);

        lightningEffect.CurrentTechnique.Passes[0].Apply();
        gd.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexBuffer.VertexCount);
    }
}