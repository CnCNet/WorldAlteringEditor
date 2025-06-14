#pragma enable_d3d11_debug_symbols

#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0
#define PS_SHADERMODEL ps_4_0
#endif

// Shader for rendering an alpha light map on a full map texture.

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

Texture2D RenderSurface;
sampler2D RenderSurfaceTextureSampler
{
    Texture = <RenderSurface>;
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
    float alphaTex = tex2D(SpriteTextureSampler, input.TextureCoordinates).r;
    float4 surfaceColor = tex2D(RenderSurfaceTextureSampler, input.TextureCoordinates);    
    return surfaceColor * (alphaTex * 2.0);
}

technique SpriteDrawing
{
    pass P0
    {
        AlphaBlendEnable = FALSE;
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};