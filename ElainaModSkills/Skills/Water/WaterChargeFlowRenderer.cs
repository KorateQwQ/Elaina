using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace 伊蕾娜.ElainaModSkills.Skills.Water;

// Continuous incoming ribbons that wrap around the sphere before being absorbed.
// Pure FNA rendering so the preview and the projectile use exactly the same geometry.
internal sealed class WaterChargeFlowRenderer : IDisposable
{
    private static int Segments => 88;
    private readonly GraphicsDevice device;
    private readonly Effect effect;
    private readonly Func<string, Texture2D> loadTexture;
    private readonly SamplerState trailSampler = new() { Filter = TextureFilter.Linear,
        AddressU = TextureAddressMode.Wrap, AddressV = TextureAddressMode.Clamp };
    private VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[(Segments + 1) * 2];
    private int[] indices = new int[Segments * 6];

    internal WaterChargeFlowRenderer(GraphicsDevice device, Effect material,
        Func<string, Texture2D> loadTexture)
    {
        vertices = new VertexPositionColorTexture[(Segments + 1) * 2];
        this.device = device;
        this.loadTexture = loadTexture;
        effect = material.Clone();
        for (int i = 0; i < Segments; i++)
        {
            int v = i * 2, j = i * 6;
            indices[j] = v; indices[j + 1] = v + 1; indices[j + 2] = v + 2;
            indices[j + 3] = v + 1; indices[j + 4] = v + 3; indices[j + 5] = v + 2;
        }
    }

    // Caller ends its SpriteBatch first. Center is already relative to the camera.
    internal void Draw(Vector2 center, float waterRadius, float seconds, int seed,
        bool foreground, Matrix transform, float opacity = 1f)
    {
        // 调参区：直接修改这些局部变量，应用 C# 热重载后下一次 Draw 使用新值。
        // 材质每次从资源缓存按当前路径获取，不会锁在构造时；路径不带扩展名。
        string trailPath = "伊蕾娜/ElainaModSkills/Skills/Water/Eff_Splash_322_288"; // 青蓝水体与浪花形状
        string foamPath = trailPath; // 用同一张图的白色区域绘制浪花，与水体轮廓对齐
        string splashPath = "伊蕾娜/ElainaModSkills/Skills/Water/Eff_Splash_4001"; // 2列×6行破浪图集
        string noisePath = "KL/Effects/Tex/Noise/Eff_Noise_DHL_01"; // 扰动噪声
        string glowPath = "KL/Effects/Tex/Trail/LightTrail"; // 柔光
        float widthMultiplier = 1f; // 条带宽度倍率
        float brightness = 1f; // 整体亮度
        float foamStrength = 1.15f; // 白色浪花不透明度，0 关闭；不受 mainColor 染色
        float foamThreshold = .42f; // 提取材质白色区域的阈值，越低白色浪花越多
        float foamSoftness = .12f; // 浪花边缘柔和度
        float waterOpacity = .78f; // 青蓝水体透明度
        float splashStrength = .9f; // 翘出水流边缘的白色浪花，0关闭
        float splashSize = 1f; // 破浪片大小
        float foamSpread = .035f; // 白沫横向展开，避免只剩一根细白线
        Color foamColor = new Color(245, 253, 255); // 实色白沫，正常透明混合
        float noiseStrength = .025f; // 横向扰动幅度
        float textureRepeat = 1f; // 纹理重复次数
        float textureScrollSpeed = 2.1f; // 纹理滚动速度，正数向球心
        float noiseScrollSpeed = .3f; // 噪声滚动速度
        float flowSpeedMultiplier = 3.2f; // 整股水流向球心移动的速度倍率
        int streamCount = 9; // 每次蓄力只生成这一轮，不循环
        float spawnInterval = .05f; // 每3帧错峰汇入一股，增加总数而不同时爆发
        float spawnReach = 430f; // 外侧起点距离基准，叠加各股远近差异
        float stopEmissionSeconds = .45f; // 提前停止生成新水流
        float finishSeconds = .8f; // 最晚48帧全部收进球里（球50帧长满、60帧完成显现）
        float headTaperLength = .28f; // 前端收尖占条带长度的比例，越大头部越细长
        float headTaperPower = 1.35f; // 前端收细程度，越大越尖
        float wrapTurns = 1.12f; // 接触后沿倾斜球面缠绕的圈数，同一条水流连续进入
        float surfaceRadiusScale = 256f / 255f * .35f; // 对齐 DrawBall 的 512px 贴图实际半径
        float absorptionStart = .72f; // 绕行后段才逐渐融入球体，之前保留水纹
        float approachCurl = .55f; // 从远处开始盘旋，逐渐增加角速度；不在接触点突然转向

        bool trailUsesAlpha = true; // 彩色透明底图=true；黑底灰度图（如 Eff_Trail_1679）=false
        float materialColorStrength = 0f; // 去掉原贴图青绿偏色，水体颜色统一由 mainColor 控制
        float textureLength = 180f; // 每次纹理重复对应的路径像素长度，越大水纹越舒展
        float ribbonWidth = 28f; // 满蓄力条带宽度（像素），主体中段保持均匀
        float glowStrength = .12f; // 柔光强度，避免把水流糊成光团

        Color mainColor = new Color(128, 191, 255, 255); // 对齐水球 ColorB 到 ColorA 之间的浅蓝色

        if (seconds <= 0 || seconds >= finishSeconds || opacity <= 0) return;
        // Segments 可热调；同步调整顶点和索引，避免只改属性后数组越界。
        int segments = Math.Clamp(Segments, 16, 256);
        if (vertices.Length != (segments + 1) * 2 || indices.Length != segments * 6)
        {
            vertices = new VertexPositionColorTexture[(segments + 1) * 2];
            indices = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                int v = i * 2, j = i * 6;
                indices[j] = v; indices[j + 1] = v + 1; indices[j + 2] = v + 2;
                indices[j + 3] = v + 1; indices[j + 4] = v + 3; indices[j + 5] = v + 2;
            }
        }
        Texture2D waterTexture = loadTexture(trailPath);
        Texture2D foamTexture = loadTexture(foamPath);
        Texture2D splashTexture = loadTexture(splashPath);
        Texture2D noiseTexture = loadTexture(noisePath);
        Texture2D glowTexture = loadTexture(glowPath);
        BlendState blend = device.BlendState;
        DepthStencilState oldDepth = device.DepthStencilState;
        RasterizerState rasterizer = device.RasterizerState;
        SamplerState sampler = device.SamplerStates[0];
        Texture texture = device.Textures[0];
        SamplerState noiseSampler = device.SamplerStates[1];
        Texture previousNoise = device.Textures[1];
        try
        {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            effect.Parameters["Transform"].SetValue(transform * Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width,
                device.Viewport.Height, 0, -1, 1));
            effect.Parameters["Time"].SetValue(seconds);
            effect.Parameters["NoiseStrength"].SetValue(MathHelper.Clamp(noiseStrength, 0, .5f));
            effect.Parameters["TextureRepeat"].SetValue(MathHelper.Clamp(textureRepeat, .1f, 8f));
            effect.Parameters["TextureScrollSpeed"].SetValue(MathHelper.Clamp(textureScrollSpeed, -3, 3));
            effect.Parameters["NoiseScrollSpeed"].SetValue(MathHelper.Clamp(noiseScrollSpeed, -3, 3));
            effect.Parameters["FoamThreshold"].SetValue(MathHelper.Clamp(foamThreshold, .05f, .95f));
            effect.Parameters["FoamSoftness"].SetValue(MathHelper.Clamp(foamSoftness, .01f, .4f));
            effect.Parameters["FoamSpread"].SetValue(MathHelper.Clamp(foamSpread, 0, .12f));
            float charge = Smooth(seconds / (50f / 60f));
            // Fixed reach while charging: the source end must not expand outwards as the ball grows.
            float reach = spawnReach;
            int count = Math.Clamp(streamCount, 1, 12);
            for (int stream = 0; stream < count; stream++)
            {
                float key = stream * 17.13f + seed * 1.37f;
                // Alternate distant, middle and near streams in time, not one ring of equal ribbons.
                float depth = (stream % 5) switch { 0 => .18f, 1 => .9f, 2 => .4f, 3 => 1f, _ => .55f };
                bool inFront = depth > .7f;
                float spawnTime = .02f + stream * Math.Max(.025f, spawnInterval);
                if (spawnTime >= stopEmissionSeconds || spawnTime >= finishSeconds) continue;
                float age = seconds - spawnTime;
                if (age <= 0) continue;
                float pulseLength = .5f + Hash(key + 11) * .22f;
                float speed = (.9f + Hash(key + 19) * .3f) * MathHelper.Clamp(flowSpeedMultiplier, .1f, 8f);
                float duration = Math.Min((1f + pulseLength) / speed, finishSeconds - spawnTime);
                if (age >= duration) continue;
                float head = age / duration * (1f + pulseLength);
                DrawPulse(head);

                void DrawPulse(float head)
                {
                    float tail = head - pulseLength;
                    float start = Math.Max(0, tail), end = Math.Min(1, head);
                    if (start >= end) return;
                    // Golden-angle ordering separates successive arrivals; seed rotates the whole set.
                    float angle = stream * 2.399963f + Hash(seed + 3) * MathHelper.TwoPi;
                    Vector2 radial = new(MathF.Cos(angle), MathF.Sin(angle));
                    Vector2 side = new(-radial.Y, radial.X);
                    float length = reach * (.6f + depth * .45f + Hash(key + 3) * .18f);
                    float surfaceRadius = waterRadius * surfaceRadiusScale;
                    float sweep = MathHelper.TwoPi * wrapTurns;
                    float tilt = .62f + Hash(key + 9) * .2f;
                    float lift = MathF.Sqrt(1 - tilt * tilt) * (inFront ? 1 : -1);
                    const float join = .48f;
                    float curl = MathHelper.Clamp(approachCurl, 0, 1);
                    float contactAngleFraction = (1 - curl) * join + curl * join * join * join;
                    float totalAngle = sweep / (1 - contactAngleFraction);
                    Vector3 Path(float t)
                    {
                        // One inward spiral from the source: both angular motion and radial
                        // contraction already begin far away. Radius settles with zero slope
                        // and curvature, so the surface never acts like a deflecting obstacle.
                        float theta = totalAngle * ((1 - curl) * t + curl * t * t * t - contactAngleFraction);
                        float wrap = MathHelper.Clamp((t - join) / (1 - join), 0, 1);
                        float approach = MathF.Pow(Math.Max(0, 1 - t / join), 2.2f);
                        float radius = surfaceRadius * 1.025f + (length - surfaceRadius * 1.025f) * approach
                            - surfaceRadius * (.08f * Smooth(wrap)
                                + .14f * Smooth((wrap - absorptionStart) / (1 - absorptionStart)));
                        return new Vector3(radial * MathF.Cos(theta) + side * (MathF.Sin(theta) * tilt),
                            MathF.Sin(theta) * lift) * radius;
                    }
                    float FrontWeight(Vector3 point) => Smooth((point.Z / Math.Max(1, surfaceRadius) + .09f) / .18f);
                    float strength = Smooth(age / .045f) * (.7f + charge * .3f) * opacity
                        * MathHelper.Clamp(brightness, 0, 3f) * (.45f + depth * .55f);
                    float width = ribbonWidth * (.36f + depth * .72f) * (.85f + Hash(key + 5) * .3f)
                        * MathHelper.Clamp(widthMultiplier, .1f, 4f);

                    // Arc-length lookup: pulse motion, vertex spacing and material UV all use
                    // distance along the spiral, rather than its nonuniform parameter.
                    Span<float> distances = stackalloc float[segments + 1];
                    Vector3 previous = Path(0);
                    distances[0] = 0;
                    for (int j = 1; j <= segments; j++)
                    {
                        Vector3 point = Path(j / (float)segments);
                        distances[j] = distances[j - 1] + Vector3.Distance(previous, point);
                        previous = point;
                    }
                    float totalLength = distances[segments];
                    for (int layer = 0; layer < 3; layer++)
                    {
                        int lookup = 0;
                        for (int i = 0; i <= segments; i++)
                        {
                            float along = MathHelper.Lerp(start, end, i / (float)segments);
                            float distance = along * totalLength;
                            while (lookup < segments - 1 && distances[lookup + 1] < distance) lookup++;
                            float fraction = (distance - distances[lookup])
                                / Math.Max(.001f, distances[lookup + 1] - distances[lookup]);
                            float t = (lookup + fraction) / segments;
                            Vector3 point = Path(t);
                            Vector2 pos = center + new Vector2(point.X, point.Y);
                            Vector3 direction = Path(Math.Min(1, t + .001f)) - Path(Math.Max(0, t - .001f));
                            Vector2 tangent = new(direction.X, direction.Y);
                            Vector2 normal = tangent.LengthSquared() > .001f
                                ? Vector2.Normalize(new Vector2(-tangent.Y, tangent.X)) : side;
                            float phase = MathHelper.Clamp((along - tail) / pulseLength, 0, 1);
                            // Keep the middle full; give the inward-moving head a longer, finer tip.
                            float headTaper = MathF.Pow(Smooth((1 - phase)
                                / MathHelper.Clamp(headTaperLength, .05f, .6f)),
                                MathHelper.Clamp(headTaperPower, .5f, 3f));
                            float tips = Smooth(phase / .14f) * headTaper;
                            float outerFade = Smooth(along / .055f);
                            float wrap = MathHelper.Clamp((t - join) / (1 - join), 0, 1);
                            float intoBall = 1 - Smooth((wrap - absorptionStart) / (1 - absorptionStart));
                            float surfaceBlend = Smooth(wrap / .16f);
                            float frontWeight = FrontWeight(point);
                            float depthWeight = foreground ? frontWeight : 1 - frontWeight;
                            // Near streams shrink into depth; far streams grow slightly before absorption.
                            float perspective = MathHelper.Lerp(1f, inFront ? .5f : 1.15f, Smooth(along));
                            float halfWidth = width * .5f * tips * outerFade * perspective
                                * MathHelper.Lerp(1f, .82f, surfaceBlend) * MathHelper.Lerp(.3f, 1f, intoBall);
                            float alpha = strength * Smooth(phase / .4f) * Smooth((1 - phase) / .12f)
                                * outerFade * intoBall * depthWeight * MathHelper.Lerp(1f, .8f, surfaceBlend);
                            Color color;
                            if (layer == 0)
                            {
                                halfWidth *= 1.65f;
                                color = new Color( mainColor.R,mainColor.G,mainColor.B, 0) * (alpha * glowStrength);
                            }
                            else if (layer == 1)
                                color = mainColor * (alpha * MathHelper.Clamp(waterOpacity, 0, 1));
                            else
                            {
                                // Real white coverage on the water, not additive blue-tinted glints.
                                // Same geometry and UV as the body keeps the foam on its crests.
                                color = foamColor * MathHelper.Clamp(alpha * foamStrength * MathHelper.Lerp(1f, .7f, surfaceBlend), 0, 1);
                            }
                            float uv = distance / Math.Max(32f, textureLength) + Hash(key + 13);
                            vertices[i * 2] = new(new Vector3(pos - normal * halfWidth, 0), color, new Vector2(uv, 0));
                            vertices[i * 2 + 1] = new(new Vector3(pos + normal * halfWidth, 0), color, new Vector2(uv, 1));
                        }
                        effect.Parameters["Layer"].SetValue((float)layer);
                        effect.Parameters["UseTextureAlpha"].SetValue(trailUsesAlpha);
                        effect.Parameters["MaterialColorStrength"].SetValue(layer == 1 ? materialColorStrength : 0f);
                        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            device.Textures[0] = layer == 0 ? glowTexture : layer == 1 ? waterTexture : foamTexture;
                            device.Textures[1] = noiseTexture;
                            device.SamplerStates[0] = layer == 0 ? SamplerState.LinearClamp : trailSampler;
                            device.SamplerStates[1] = SamplerState.LinearWrap;
                            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0,
                                vertices.Length, indices, 0, segments * 2);
                        }
                    }

                    // Short-lived foam crests ride the moving water. Fixed pulse-local
                    // positions prevent random flickering; sprite fragments extend beyond
                    // the ribbon silhouette without inflating the ribbon itself.
                    for (int crest = 0; crest < (inFront ? 2 : 1); crest++)
                    {
                        float phase = .38f + crest * .29f;
                        float along = tail + phase * pulseLength;
                        if (along < .08f || along >= 1f || splashStrength <= 0) continue;
                        float distance = along * totalLength;
                        int lookup = 0;
                        while (lookup < segments - 1 && distances[lookup + 1] < distance) lookup++;
                        float t = (lookup + (distance - distances[lookup])
                            / Math.Max(.001f, distances[lookup + 1] - distances[lookup])) / segments;
                        Vector3 point = Path(t);
                        Vector2 pos = center + new Vector2(point.X, point.Y);
                        Vector3 direction = Path(Math.Min(1, t + .001f)) - Path(Math.Max(0, t - .001f));
                        Vector2 tangent = new(direction.X, direction.Y);
                        if (tangent.LengthSquared() < .001f) continue;
                        tangent.Normalize();
                        Vector2 normal = new(-tangent.Y, tangent.X);
                        float sign = Hash(key + crest * 9 + 7) > .5f ? 1f : -1f;
                        float wrap = MathHelper.Clamp((t - join) / (1 - join), 0, 1);
                        float surfaceBlend = Smooth(wrap / .16f);
                        float crestFade = Smooth((along - .08f) / .12f)
                            * (1 - Smooth((wrap - absorptionStart) / (1 - absorptionStart)));
                        float frontWeight = FrontWeight(point);
                        float alpha = MathHelper.Clamp(strength * crestFade * splashStrength
                            * (foreground ? frontWeight : 1 - frontWeight) * MathHelper.Lerp(1f, .5f, surfaceBlend), 0, 1);
                        // A little of the crest overlaps the water, the rest breaks its edge.
                        float splashWidth = width * MathHelper.Lerp(1f, inFront ? .5f : 1.15f, Smooth(along))
                            * MathHelper.Lerp(1f, .7f, surfaceBlend);
                        pos += normal * sign * splashWidth * .28f;
                        float halfLength = splashWidth * .9f * splashSize;
                        float halfHeight = splashWidth * .25f * splashSize;
                        int column = (stream + crest + seed) & 1;
                        int row = 1 + (stream + crest + seed) % 3;
                        float u0 = column * .5f + .002f, u1 = (column + 1) * .5f - .002f;
                        float v0 = row / 6f + .002f, v1 = (row + 1) / 6f - .002f;
                        Color color = foamColor * alpha;
                        vertices[0] = new(new Vector3(pos - tangent * halfLength - normal * halfHeight, 0), color, new Vector2(u0, v0));
                        vertices[1] = new(new Vector3(pos - tangent * halfLength + normal * halfHeight, 0), color, new Vector2(u0, v1));
                        vertices[2] = new(new Vector3(pos + tangent * halfLength - normal * halfHeight, 0), color, new Vector2(u1, v0));
                        vertices[3] = new(new Vector3(pos + tangent * halfLength + normal * halfHeight, 0), color, new Vector2(u1, v1));
                        effect.Parameters["Layer"].SetValue(3f);
                        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            device.Textures[0] = splashTexture;
                            device.SamplerStates[0] = SamplerState.LinearClamp;
                            device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, 4, indices, 0, 2);
                        }
                    }
                }
            }
        }
        finally
        {
            device.BlendState = blend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = rasterizer;
            device.SamplerStates[0] = sampler;
            device.Textures[0] = texture;
            device.SamplerStates[1] = noiseSampler;
            device.Textures[1] = previousNoise;
        }
    }

    private static float Hash(float n) { float x = MathF.Sin(n * 127.1f + 311.7f) * 43758.5453f; return x - MathF.Floor(x); }
    private static float Smooth(float t) { t = MathHelper.Clamp(t, 0, 1); return t * t * (3 - 2 * t); }

    public void Dispose()
    {
        effect.Dispose();
        trailSampler.Dispose();
        // Textures belong to ModContent (or the preview's cache).
    }
}
