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
    // We need to read from the main texture first,
    // otherwise the output will be black!
    float lightDelta = tex2D(SpriteTextureSampler, input.TextureCoordinates).a - 0.5;

    return UseAlphaChannel ? float4(0, 0, 0, lightDelta) : float4(lightDelta, 0, 0, 0);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};