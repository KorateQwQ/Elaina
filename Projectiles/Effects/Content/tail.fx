sampler uImage0 : register(s0); //MainColor
sampler uImage1 : register(s1); //MainShape
sampler uImage2 : register(s2); //MaskColor

float4x4 uTransform;
float uTime;
float uuTime;
float maxLength;

struct VSInput {
	float2 Pos : POSITION0;
	float4 Color : COLOR0;
	float3 Texcoord : TEXCOORD0;
};

struct PSInput {
	float4 Pos : SV_POSITION;
	float4 Color : COLOR0;
	float3 Texcoord : TEXCOORD0;
};

float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs((c.xxx + K.xyz - floor(c.xxx + K.xyz)) * 6.0 - K.www);
    return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}

float4 PixelShaderFunction(PSInput input) : COLOR0 {
    float3 coord = input.Texcoord;

    if (uTime == 996)
    {
        float4 color = tex2D(uImage0, float2(coord.x+uuTime%1, coord.y));//用时间做偏移
        float maxy = lerp(0.1, 0.35, coord.x / 0.8);
        if (maxy > 0.35)
            maxy = 0.35;
        float streath = smoothstep(maxy, 0, abs(0.5 - coord.y)); //越靠近图片边缘，透明度越低 
        float streath2 = 1;
        if (coord.x  <= 0.4)
        {
            streath2 = smoothstep(0, 0.4, coord.x); //越靠近图片边缘，透明度越低
            return color * streath * streath2;
        }
        if (coord.x >= 0.6 * maxLength)
        {
            streath2 = smoothstep(maxLength, 0.6 * maxLength, coord.x); //越靠近图片边缘，透明度越低
            return color * streath * streath2;
        }
        return color * streath;

    }
    else
    {
        float4 c1 = tex2D(uImage1, float2(coord.x, coord.y));
        float4 c3 = tex2D(uImage2, float2(0.5f, coord.y));
        float l = length(coord.y - 0.5);
        float res = smoothstep(0.05, 0, l); //超过0.4的部分为0,低于0.4的部分逐渐为1
        c1 *= c3;
        float4 c = tex2D(uImage0, float2(c1.r, 0));
        if (c.r < 0.1)
            return float4(0, 0, 0, 0);
        return c * coord.z;
    }


}

PSInput VertexShaderFunction(VSInput input)  {
	PSInput output;
	output.Color = input.Color;
	output.Texcoord = input.Texcoord;
	output.Pos = mul(float4(input.Pos, 0, 1), uTransform);
	return output;
}


technique Technique1 {
	pass ColorBar {
		VertexShader = compile vs_2_0 VertexShaderFunction();
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}