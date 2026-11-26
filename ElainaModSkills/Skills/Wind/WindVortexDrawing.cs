using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static System.MathF;

namespace 伊蕾娜.ElainaModSkills.Skills.Wind;

internal sealed class WindVortexDrawing
{
    internal const float CloseSeconds = .72f;
    private const float Tau = MathHelper.TwoPi;
    private const int MaxGrains = 704;
    // The dust triangles and halo quads occupy disjoint ranges during the particle pass.
    private const int HaloStart = MaxGrains * 3;
    private static readonly short[] HaloIndices = MakeHaloIndices();
    private readonly VertexPositionColorTexture[] mesh = new VertexPositionColorTexture[HaloStart + MaxGrains * 4];

    private static short[] MakeHaloIndices()
    {
        var indices = new short[MaxGrains * 6];
        for (int i = 0; i < MaxGrains; i++) {
            int first = i * 4, index = i * 6;
            indices[index] = (short)first;
            indices[index + 1] = indices[index + 4] = (short)(first + 1);
            indices[index + 2] = indices[index + 3] = (short)(first + 2);
            indices[index + 5] = (short)(first + 3);
        }
        return indices;
    }
    private readonly List<Panel> panels = new(40);
    private int used;
    private record struct Blade(float Radius, float Flat, float Angle, float Span, float Weight, float Cos, float Sin, float Alpha, float Offset);
    private record struct Panel(Blade Blade, float Start, float End, float Depth, bool Outer);
    private static float Random(int seed) { double a = Math.Sin(seed*78.233+.619)*43758.5453; return (float)(a-Math.Floor(a)); }
    private static float Smooth(float x) { x=Math.Clamp(x,0,1); return x*x*(3-2*x); }
    private static float Fraction(float x) => x-Floor(x);
    private static Color WithAlpha(Color c,float a) { c.A=(byte)(255*Math.Clamp(a,0,1)); return c; }
    private static VertexPositionColorTexture V(Vector2 p,Color c,float u=0,float v=0) => new(new Vector3(p,0),c,new Vector2(u,v));
    private void Flush(GraphicsDevice device, Effect effect,string technique)
    {
        if (used==0) return;
        effect.CurrentTechnique = effect.Techniques[technique];
        effect.CurrentTechnique.Passes[0].Apply();
        device.DrawUserPrimitives(PrimitiveType.TriangleList,mesh,0,used/3);
        used=0;
    }
    private void Quad(VertexPositionColorTexture a,VertexPositionColorTexture b,VertexPositionColorTexture c,VertexPositionColorTexture d)
    {
        mesh[used++]=a; mesh[used++]=b; mesh[used++]=c;
        mesh[used++]=c; mesh[used++]=b; mesh[used++]=d;
    }
    private void Collect(float time,float stop,int variation)
    {
        panels.Clear();
        for (int i=0;i<8;i++) {
            float period=.56f+i*.022f, offset=i*.618f, clock=time/period+offset;
            int cycle=(int)Floor(clock), seed=cycle*53+i*383+variation;
            if (cycle>Floor(stop/period+offset)) continue;
            float age=Fraction(clock), peak=Math.Clamp((age-.18f)/.54f,0,1);
            float tilt=-.32f+(Random(seed)-.5f)*(i%3==0?2.25f:.62f);
            var blade=new Blade((124+Random(seed+1)*108)*(.72f+.62f*age),.16f+.31f*Random(seed+2),
                Random(seed+3)*Tau+age*3.2f+Smooth(peak)*2.5f,1.65f+Random(seed+4)*1.5f+.85f*Sin(peak*PI),
                4+Random(seed+5)*10,Cos(tilt),Sin(tilt),Smooth(age/.16f)*(1-Smooth((age-.60f)/.4f)),2*Random(seed+23));
            float tail=blade.Angle-blade.Span, start=0;
            for (int k=(int)Floor(tail/(PI/2))+1; start<1; k++) {
                float end=Math.Min(1,(k*(PI/2)-tail)/blade.Span);
                float normalZ=Sin(tail+blade.Span*(start+end)*.5f);
                panels.Add(new(blade,start,end,blade.Radius*normalZ*Sqrt(1-blade.Flat*blade.Flat),normalZ>0));
                start=end;
            }
        }
        panels.Sort(static (a,b)=>a.Depth.CompareTo(b.Depth));
    }
    private static void Section(Blade b,float u,Color c,float density,out VertexPositionColorTexture a,out VertexPositionColorTexture d)
    {
        float angle=b.Angle-b.Span*(1-u), radial=b.Radius*Sin(angle), x=b.Radius*Cos(angle);
        c.G = (byte)(255 * Abs(Sin(angle))); // Continuous facing for emission at the face transition.
        float arch=Max(0,Sin(PI*u)), w=b.Weight*0.95f*Pow(arch,.82f)*(.36f+1.35f*u);
        float elevation=Sqrt(1-b.Flat*b.Flat), y=radial*b.Flat+b.Weight*(1.5f*arch-.8f*u)*elevation;
        Vector2 center=new(x*b.Cos-y*b.Sin,x*b.Sin+y*b.Cos), half=new Vector2(-b.Sin,b.Cos)*(w*elevation);
        float padding = c.R == 0 ? 0 : 1;
        half *= 1 + 2 * padding;
        // Width-weighted cross coordinates stay continuous across the diagonal
        // of a tapering quad. Position.Z carries width only for the blade pass.
        float fullWidth = 2 * w * elevation;
        float texU = u * density + b.Offset;
        float edgeDistance = fullWidth * (.5f + padding);
        a = new(new Vector3(center + half, fullWidth), c, new(texU, -edgeDistance));
        d = new(new Vector3(center - half, fullWidth), c, new(texU, edgeDistance));
    }
    private void DrawBlades(GraphicsDevice device, Effect effect,bool front,float density)
    {
        foreach(var panel in panels) {
            if ((panel.Depth>=0)!=front) continue;
            var b=panel.Blade;
            Color c=WithAlpha(panel.Outer?Color.White:Color.Black,b.Alpha);
            int n=Math.Max(2,(int)Ceiling((panel.End-panel.Start)*b.Span*20));
            Section(b,panel.Start,c,density,out var a,out var d);
            for(int i=1;i<=n;i++) {
                Section(b,MathHelper.Lerp(panel.Start,panel.End,(float)i/n),c,density,out var e,out var f);
                Quad(a,d,e,f); a=e;d=f;
            }
        }
        Flush(device,effect,"BladePass");
    }
    private readonly record struct Grain(float Offset,float Angle,float Duration,float Reach,bool Leaf);
    private readonly Grain[] grains=MakeGrains();
    private static Grain[] MakeGrains()
    {
        var particles=new Grain[MaxGrains];
        for(int i=0;i<particles.Length;i++) particles[i]=new(Random(i+400),Random(i+700)*Tau,.95f+1.3f*Random(i+900),150+195*Random(i+1100),i%11==0);
        return particles;
    }
    private void DrawParticles(GraphicsDevice device, Effect fx, float time, float stop, float opacity)
    {
        float rangeScale = WindVortexSettings.ParticleRangeScale;
        if (rangeScale <= 0f || opacity <= 0f) return;
        float strength = Math.Max(0f, WindVortexSettings.ParticleBloomStrength);
        fx.Parameters["ParticleRgbGain"].SetValue(1f + strength);
        int haloCount = 0;
        int count = (int)Round(Math.Clamp(WindVortexSettings.ParticleDensity, 0f, 4f) * 176);
        Color dust = WindVortexSettings.ParticleColor, leaf = WindVortexSettings.LeafColor;
        for(int i=0;i<count;i++) {
            var p=grains[i]; float clock=time/p.Duration+p.Offset;
            if(Floor(clock)>Floor(stop/p.Duration+p.Offset)) continue;
            float age=Fraction(clock), q=1-age, r=36+p.Reach*Pow(q,1.3f), a=p.Angle+age*1.55f+Pow(age,4)*2.4f;
            float x = r * Cos(a);
            Vector2 pos = new Vector2(x, r * Sin(a) * .70f - x * .18f) * rangeScale;
            float dr=-1.3f*p.Reach*Pow(q,.3f), da=1.55f+9.6f*age*age*age;
            float vx=dr*Cos(a)-r*Sin(a)*da; Vector2 velocity=new(vx,.70f*(dr*Sin(a)+r*Cos(a)*da)-vx*.18f);
            float length=velocity.Length(); Vector2 dir=velocity/Max(.001f,length), normal=new(-dir.Y,dir.X);
            float tail=Min(24,length/p.Duration*(p.Leaf?.055f:.036f));
            Color c=WithAlpha(p.Leaf?leaf:dust,opacity*Smooth(age/.16f)*(1-Smooth((age-.94f)/.06f))*(p.Leaf?.84f:.7f));
            float width = p.Leaf ? 1.8f : .65f;
            Vector2 tip=pos+dir, back=pos-dir*tail+normal*.18f, rib=pos-dir*(tail*.26f)-normal*width;
            if (strength > 0f && c.A > 0) {
                Vector2 glowCenter = (tip + rib) * .5f;
                Vector2 along = dir * (4f + .78f * tail);
                Vector2 across = normal * (3f + 1.4f * width + .08f * tail);
                int first = HaloStart + haloCount++ * 4;
                mesh[first] = V(glowCenter - along - across, c, 0, 0);
                mesh[first + 1] = V(glowCenter + along - across, c, 1, 0);
                mesh[first + 2] = V(glowCenter - along + across, c, 0, 1);
                mesh[first + 3] = V(glowCenter + along + across, c, 1, 1);
            }
            // Expand around the centroid; UVs keep the original triangle's barycentrics.
            Vector2 center=(tip+back+rib)/3;
            const float pad=4, low=(1-pad)/3, high=low+pad;
            mesh[used++]=V(center+(tip-center)*pad,c,high,low);
            mesh[used++]=V(center+(back-center)*pad,c,low,high);
            mesh[used++]=V(center+(rib-center)*pad,c,low,low);
        }
        if (haloCount > 0) {
            fx.CurrentTechnique = fx.Techniques["ParticleHaloPass"];
            fx.CurrentTechnique.Passes[0].Apply();
            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, mesh, HaloStart,
                haloCount * 4, HaloIndices, 0, haloCount * 2);
        }
        Flush(device, fx, "DustPass");
    }

    private void DrawSquare(GraphicsDevice device,Effect fx,float r,Color tint,string technique)
    {
        Quad(V(new(-r,-r),tint,0,0),V(new(r,-r),tint,1,0),V(new(-r,r),tint,0,1),V(new(r,r),tint,1,1));
        Flush(device,fx,technique);
    }

    internal void Draw(GraphicsDevice device,Effect fx,Texture2D mask,Texture2D noise,Texture2D haze,Texture2D scene,Matrix localToScreen,
        float time,float stop,float distortion,float rotation,float travel,int variation)
    {
        float end=Math.Clamp((time-stop)/CloseSeconds,0,1), opening=Smooth(Min(stop,time)/.27f);
        if (opening<=0) return;
        var vp=device.Viewport;
        var projection=Matrix.CreateOrthographicOffCenter(0,vp.Width,vp.Height,0,0,1);
        var p=fx.Parameters;
        p["HazeTexture"].SetValue(haze);
        p["MaskTexture"].SetValue(mask); p["NoiseTexture"].SetValue(noise); p["SceneTexture"].SetValue(scene??noise);
        p["SceneWeights"].SetValue(scene==null?Vector2.UnitY:Vector2.UnitX);
        p["Motion"].SetValue(new Vector4(time+end*end*.92f,distortion,rotation,WindVortexSettings.CoreTension));
        p["Surface"].SetValue(new Vector4(WindVortexSettings.TextureDensity,travel,WindVortexSettings.BloomStrength,WindVortexSettings.InnerBrightness));
        float darkRadius = Math.Max(1f, WindVortexSettings.DarkBaseRadius);
        float darkOpacity = Math.Clamp(WindVortexSettings.DarkBaseOpacity, 0f, 1f);
        p["DarkBase"].SetValue(new Vector4(WindVortexSettings.DarkBaseColor.ToVector3(), darkOpacity));
        p["DarkShape"].SetValue(new Vector2(darkRadius, Math.Max(.05f, WindVortexSettings.DarkBaseFalloff)));
        device.BlendState=BlendState.AlphaBlend; device.DepthStencilState=DepthStencilState.None; device.RasterizerState=RasterizerState.CullNone;
        float bladeScale = WindVortexSettings.BladeScale;
        float coreScale = WindVortexSettings.CoreScale;
        var openingTransform = Matrix.CreateScale(opening) * localToScreen;
        var bladeTransform = Matrix.CreateScale(bladeScale) * openingTransform * projection;
        var core = Matrix.CreateScale(coreScale * (.12f + .88f * (1 - Smooth(end)))) * openingTransform;
        bool drawCore = coreScale > 0f && end < 1f;
        Color tint=WithAlpha(Color.White,1-end*end*end);
        if (drawCore) {
            p["Transform"].SetValue(core*projection);
            if (darkOpacity > 0f) DrawSquare(device,fx,darkRadius,tint,"DarkBasePass");
            DrawSquare(device,fx,246,tint,"HazePass");
        }
        p["Transform"].SetValue(localToScreen*projection);
        DrawParticles(device, fx, time, stop, opening * (1 - Smooth(end)));
        if (bladeScale > 0f) {
            Collect(time,stop,variation);
            p["Transform"].SetValue(bladeTransform);
            DrawBlades(device,fx,false,WindVortexSettings.TextureDensity);
        }
        if (drawCore) {
            p["LensBasis"].SetValue(new Vector4(core.M11/vp.Width,core.M12/vp.Height,core.M21/vp.Width,core.M22/vp.Height));
            p["Transform"].SetValue(core*projection);
            DrawSquare(device,fx,120,tint,"CorePass");
        }
        if (bladeScale > 0f) {
            p["Transform"].SetValue(bladeTransform);
            DrawBlades(device,fx,true,WindVortexSettings.TextureDensity);
        }
    }
}
