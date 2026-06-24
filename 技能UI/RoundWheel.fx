sampler tex : register(s0);

float4 color; // 扇区颜色
int index; // 当前扇区的索引
int count; // 扇区总数
float rangeIn; // 中心占用范围（0~1)
float rangeOut;
float fadeRot; // 相邻扇区淡化过渡角度
float expand; // 扇形扩大的因子，用于动态调整扇形的大小和位置
float fadeDis; // 控制中心和边缘淡化的范围
float lerp;

float PI = 3.14159265359;
float warpAngle(float angle)
{
    // 将angle转换为[0, 2π]范围内的值
    while (angle < 0)
        angle += 2 * PI;
    while (angle >= 2 * PI)
        angle -= 2 * PI;
    return angle;
}

float4 RoundWheel(float2 coords : TEXCOORD0) : COLOR0
{
    float4 trans = float4(0,0,0,0);
    float2 target = coords - float2(0.5, 0.5);
    float dis = length(target) / 0.5;
    float rot = warpAngle(atan2(target.y, target.x));
    
    float per = PI / count;
    float rotAlpha;
    float delta = warpAngle(rot - (index * per * 2 - per));
    float right = per * 2 - fadeRot;
    if (delta < fadeRot)
        rotAlpha = delta / fadeRot;
    else if (delta > right)
        rotAlpha = 1 - (delta - right) / fadeRot;
    else 
        rotAlpha = 1;
    
    float disAlpha;
    right = rangeOut * expand * lerp - fadeDis;
    delta = dis - rangeIn * expand * lerp;
    if (delta < fadeDis)
        disAlpha = delta / fadeDis;
    else if (delta > right)
        disAlpha = 1 - (delta - right) / fadeDis;
    else 
        disAlpha = 1;
    
    return color * min(rotAlpha, disAlpha) * lerp;
}

technique T
{
    pass RoundWheel
    {
        PixelShader = compile ps_3_0 RoundWheel();
    }
}