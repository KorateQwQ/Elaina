sampler iChannel0 : register(s0);
sampler iChannel1 : register(s1);//遮罩，直接绘制
sampler iChannel2 : register(s2);//噪声，用于扰动内部形状
sampler iChannel3 : register(s3);//噪声，用于扰动整个球体
sampler iChannel4 : register(s4);//消融噪声

float2 TotalDrawSize;//整体绘制尺寸缩放，避免扰动后出框
float DistortStrength;//内部形状扰动强度
float2 DistortTiling;//内部扰动采样平铺
float2 DistortUTime;//内部扰动滚动

float ShapeDistortStrength;//球体形状扰动强度（影响球体轮廓）
float2 ShapeDistortTiling;//球体形状扰动采样平铺
float2 ShapeDistortUTime;//球体形状扰动滚动

float RotWorldX;
float RotWorldY;
float RotWorldZ;
float RotLocalX;
float RotLocalY;
float RotLocalZ;

bool clipImageX = false;
bool clipImageY = false;

//内部纹理缩放
float2 ImageScale;
//遮罩纹理缩放
float2 MaskScale;

//纹理风格化颜色，水球使用两种蓝色以及白色
float4 ColorA;
float4 ColorB;
float4 ColorC;
//风格化阈值
float2 ColorThreshold;
//纹理渐变范围
float smoothWidth;
//纹理流动
float2 ImageUTime;

//消融阈值
float FadeThreshold;
//消融贴图大小
float2 FadeTiling = float2(1.0f, 1.0f);

float4 MainPS(float2 texCoord : TEXCOORD0) : COLOR0
{
    //RotWorldX = iTime*2.0f;
    const float PI = 3.14159265;

    float2 uv = (texCoord * 2.0 - 1.0) / max(TotalDrawSize, float2(0.0001, 0.0001));
    float2 shapeNoiseUV = uv * ShapeDistortTiling + ShapeDistortUTime;
    float2 shapeNoise = tex2D(iChannel3, frac(shapeNoiseUV)).rg * 2.0 - 1.0;
    float2 shapeUV = uv + shapeNoise * ShapeDistortStrength;
    float r2 = dot(shapeUV, shapeUV);
    if (r2 > 1.0)
    {
        return float4(0.0, 0.0, 0.0, 0.0);
    }

    float z = sqrt(1.0 - r2);
    float3 posFront = float3(uv.x, uv.y, z);

    float2 noiseUVFront = posFront.xy * DistortTiling + DistortUTime;
    float2 noiseFront = tex2D(iChannel2, frac(noiseUVFront)).rg * 2.0 - 1.0;
    posFront.xy += noiseFront * DistortStrength;
    posFront = normalize(posFront);

    float cwx = cos(RotWorldY);
    float swx = sin(RotWorldY);
    float cwy = cos(RotWorldX);
    float swy = sin(RotWorldX);
    float cwz = cos(RotWorldZ);
    float swz = sin(RotWorldZ);

    float clx = cos(RotLocalY);
    float slx = sin(RotLocalY);
    float cly = cos(RotLocalX);
    float sly = sin(RotLocalX);
    float clz = cos(RotLocalZ);
    float slz = sin(RotLocalZ);

    float3 worldXFront = float3(posFront.x, posFront.y * cwx - posFront.z * swx, posFront.y * swx + posFront.z * cwx);
    float3 worldYFront = float3(worldXFront.x * cwy + worldXFront.z * swy, worldXFront.y, -worldXFront.x * swy + worldXFront.z * cwy);
    float3 worldZFront = float3(worldYFront.x * cwz - worldYFront.y * swz, worldYFront.x * swz + worldYFront.y * cwz, worldYFront.z);

    float3 localXFront = float3(worldZFront.x, worldZFront.y * clx - worldZFront.z * slx, worldZFront.y * slx + worldZFront.z * clx);
    float3 localYFront = float3(localXFront.x * cly + localXFront.z * sly, localXFront.y, -localXFront.x * sly + localXFront.z * cly);
    float3 localZFront = float3(localYFront.x * clz - localYFront.y * slz, localYFront.x * slz + localYFront.y * clz, localYFront.z);

    float uFront = atan2(localZFront.x, localZFront.z) / (2.0 * PI) + 0.5;
    float vFront = asin(localZFront.y) / PI + 0.5;
    

    float2 scale = max(ImageScale, float2(0.0001, 0.0001));
    float2 scaledUVFront = (float2(uFront, vFront) - 0.5) / scale + 0.5;
    
    bool outFrontX = clipImageX && (scaledUVFront.x < 0.0 || scaledUVFront.x > 1.0);
    bool outFrontY = clipImageY && (scaledUVFront.y < 0.0 || scaledUVFront.y > 1.0);


    float4 frontColor = (outFrontX || outFrontY) ? float4(0.0, 0.0, 0.0, 0.0) : tex2D(iChannel0, scaledUVFront+frac(ImageUTime));

    float2 fadeUVFront = (float2(uFront, vFront) - 0.5) / FadeTiling + 0.5;
    float4 fadeColor = (outFrontX || outFrontY) ? float4(0.0, 0.0, 0.0, 0.0) : tex2D(iChannel4, fadeUVFront);
    if (fadeColor.r<FadeThreshold)
            return float4(0.0, 0.0, 0.0, 0.0);

    //最终颜色根据ColorThreshold与frontColor.R进行选择，
    //当frontColor.R>ColorThreshold.X 颜色为ColorA,ColorThreshold.Y<frontColor.R<ColorThreshold.X 颜色为ColorB,
    //当frontColor.R<ColorThreshold.Y 颜色为ColorC
    float t1 = smoothstep(ColorThreshold.y - smoothWidth, ColorThreshold.y + smoothWidth, frontColor.r);
    float t2 = smoothstep(ColorThreshold.x - smoothWidth, ColorThreshold.x + smoothWidth, frontColor.r);
    float4 finalColor = lerp(ColorC, ColorB, t1);
    finalColor = lerp(finalColor, ColorA, t2);

    float2 scale2 = max(MaskScale, float2(0.0001, 0.0001));
    float2 scaledUV2 = (float2(texCoord.x, texCoord.y) - 0.5) / scale2 + 0.5;

    //额外绘制一个遮罩贴在球面上当高光
    if (frontColor.a>0.0f)
        return min(finalColor+tex2D(iChannel1, scaledUV2), float4(1.2, 1.2, 1.2, 1.0));
    return float4(0.0, 0.0, 0.0, 0.0);
}

technique Technique1
{
    pass Apply
    {
        PixelShader = compile ps_3_0 MainPS();
    }

}