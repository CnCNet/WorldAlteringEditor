#pragma enable_d3d11_debug_symbols

#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

// Shader for rendering Tiberian Sun alpha images to an alpha map.

bool UseAlphaChannel;

Texture2D SpriteTexture;
sampler2D SpriteTextureSampler : register(s0)
{
    Texture = <SpriteTexture>; // this is set by spritebatch
    AddressU = clamp;
    AddressV = clamp;
    MipFilter = Point;
    MinFilter = Point;
    MagFilter = Point;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float lightIndex = round(tex2D(SpriteTextureSampler, input.TextureCoordinates).a * 255.0);

    // Skip neutral pixels so blend rounding can never change the destination.
    clip(abs(lightIndex - 127.0) - 0.5);

    // The blend state scales the destination by twice this value.
    float halfMultiplier = lightIndex / (2.0 * 127.0);
    return UseAlphaChannel ? float4(0, 0, 0, halfMultiplier) : float4(halfMultiplier, 0, 0, 0);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};