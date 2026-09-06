// Dynamic vertical rainbow color effect.
// The source texture is used only as an alpha mask; its RGB color is ignored.
sampler uImage0 : register(s0);
// Noise input. Bind a grayscale or RG noise texture to GraphicsDevice.Textures[1].
sampler uImage1 : register(s1);

// Advance the rainbow phase over time. Recommended value:
// Main.GlobalTimeWrappedHourly * 0.5f
float uTime;

// Length of one HSV color band in texture UV space.
// 0.3 means red -> yellow -> green ... transitions are spaced about 0.3 UV units apart.
float uColorInterval = 0.3f;

// Overall rainbow brightness. 1.0 is the default brightness.
float uBrightness = 1.0f;

// Noise UV tiling. Larger values make the heat waves smaller and denser.
float2 uNoiseScale = float2(1.0f, 1.0f);

// Noise movement per unit of uTime.
float2 uNoiseSpeed = float2(0.1f, 0.0f);

// Maximum material UV displacement. 0 disables heat distortion.
float uDistortionStrength = 0.0f;

// Keeps heat distortion mostly horizontal. 0 = horizontal only, 1 = full RG displacement.
float uHeatVerticalStrength = 0.2f;

float4 baseColor = float4(0,0,0,0);

float3 HsvToRgb(float3 hsv)
{
    float4 k = float4(1.0f, 2.0f / 3.0f, 1.0f / 3.0f, 3.0f);
    float3 p = abs(frac(hsv.xxx + k.xyz) * 6.0f - k.www);
    return hsv.z * lerp(k.xxx, saturate(p - k.xxx), hsv.y);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0, float4 vertexColor : COLOR0) : COLOR0
{
    // Remap noise from [0, 1] to [-1, 1]. The vertical component is reduced
    // so the result reads as a horizontal heat shimmer instead of a global drift.
    float2 noiseCoords = frac(coords * uNoiseScale + uTime * uNoiseSpeed);
    float2 noiseOffset = tex2D(uImage1, noiseCoords).rg * 2.0f - 1.0f;
    noiseOffset.y *= uHeatVerticalStrength;

    // Distort both the material silhouette and its rainbow coordinates.
    float2 distortedCoords = saturate(coords + noiseOffset * uDistortionStrength);
    float4 material = tex2D(uImage0, distortedCoords);

    // Transparent material pixels stay transparent. Material RGB is intentionally ignored.
    if (material.a <= 0.001f)
        return float4(0.0f, 0.0f, 0.0f, 0.0f);

    // UV.y is 0 at the top and 1 at the bottom.
    // Six hue sections make one complete rainbow. uColorInterval controls
    // the UV length of each section, so larger values spread the colors out.
    float rainbowLength = max(uColorInterval, 0.001f) * 6.0f;
    float hue = frac((1.0f - distortedCoords.y) / rainbowLength + uTime);
    float3 rainbow = HsvToRgb(float3(hue, 1.0f, 1.0f));
    rainbow *= max(uBrightness, 0.0f);

    // Keep the distorted material silhouette and SpriteBatch opacity,
    // but not the material's original RGB color.
    return float4((rainbow + baseColor.rgb), material.a * vertexColor.a);
}

technique Technique1
{
    pass DynamicRainbow
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
