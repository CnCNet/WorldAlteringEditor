using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Windowing;
using Rampastring.XNAUI.XNAControls;
using System;
using TSMapEditor.Settings;

namespace TSMapEditor.UI.Controls;

public class EditorWindow : XNAWindow
{
    private const string BackdropBlurEffectPath = "Shaders/BackdropBlur";
    private const int DropShadowTextureCoreSize = 2;

    private static Effect backdropBlurEffect;
    private static bool backdropBlurEffectLoadAttempted;
    private static Texture2D dropShadowTexture;
    private static int dropShadowTextureRadius;

    private RenderTarget2D halfScaleRenderTarget;
    private RenderTarget2D halfScaleBlurRenderTarget;
    private RenderTarget2D quarterScaleRenderTarget;
    private RenderTarget2D quarterScaleBlurRenderTarget;

    public EditorWindow(WindowManager windowManager) : base(windowManager)
    {
        DrawMode = ControlDrawMode.UNIQUE_RENDER_TARGET;
    }

    /// <summary>
    /// Whether this window uses the glass backdrop effect when enhanced
    /// graphical quality is enabled.
    /// </summary>
    public bool EnableGlassEffect { get; set; } = true;

    /// <summary>
    /// Whether this window draws a drop shadow outside its render bounds.
    /// </summary>
    public bool EnableDropShadow { get; set; } = true;

    public override void Initialize()
    {
        Color baseColor = UISettings.ActiveSettings.PanelBackgroundColor;
        var backgroundColor = new Color(baseColor.R / 2, baseColor.G / 2, baseColor.B / 2, 222);
        BackgroundTexture = AssetLoader.GetSolidUITexture(backgroundColor);
        PanelBackgroundDrawMode = PanelBackgroundImageDrawMode.STRETCHED;

        if (!backdropBlurEffectLoadAttempted)
        {
            backdropBlurEffectLoadAttempted = true;
            backdropBlurEffect = AssetLoader.LoadEffect(BackdropBlurEffectPath);
        }

        UIHelpers.AutoAssignTextBoxNextControls(this);
        base.Initialize();
    }

    public override void Kill()
    {
        DisposeGlassRenderTargets();

        base.Kill();
    }

    protected override bool FreeRenderTarget()
    {
        DisposeGlassRenderTargets();
        return base.FreeRenderTarget();
    }

    protected override void ParseControlINIAttribute(IniFile iniFile, string key, string value)
    {
        if (key == nameof(EnableGlassEffect))
        {
            EnableGlassEffect = Conversions.BooleanFromString(value, EnableGlassEffect);
        }
        else if (key == nameof(EnableDropShadow))
        {
            EnableDropShadow = Conversions.BooleanFromString(value, EnableDropShadow);
        }

        base.ParseControlINIAttribute(iniFile, key, value);
    }

    /// <summary>
    /// Draws the window's background, using a blurred copy of the content
    /// behind the window when the glass effect is available.
    /// </summary>
    protected override void DrawWindowBackground()
    {
        if (!TryDrawGlassBackdrop())
            DrawPanel();
    }

    protected override void DrawBehindUniqueRenderTarget(Rectangle renderRectangle)
    {
        base.DrawBehindUniqueRenderTarget(renderRectangle);

        if (!EnableDropShadow || !UserSettings.Instance.EnableWindowDropShadowEffect || Alpha <= 0.0f)
            return;

        var editorUISettings = (CustomUISettings)UISettings.ActiveSettings;
        var shadowColor = editorUISettings.WindowShadowColor;
        float blurRadius = Math.Max(1.0f, editorUISettings.WindowShadowBlurRadius);
        float inactiveStrength = Math.Clamp(editorUISettings.WindowInactiveShadowStrength, 0.0f, 1.0f);
        float strength = IsForeground ? 1.0f : inactiveStrength;
        byte effectiveAlpha = (byte)Math.Clamp((int)Math.Round(shadowColor.A * Alpha * strength), 0, 255);

        if (effectiveAlpha == 0)
            return;

        int textureRadius = Math.Max(1, (int)Math.Ceiling(blurRadius));
        EnsureDropShadowTexture(textureRadius);

        int scaledRadius = Math.Max(1, (int)Math.Round(blurRadius * Scaling));
        int scaledSpread = Math.Max(0, (int)Math.Round(editorUISettings.WindowShadowSpread * Scaling));
        int scaledOffsetX = (int)Math.Round(editorUISettings.WindowShadowOffsetX * Scaling);
        int scaledOffsetY = (int)Math.Round(editorUISettings.WindowShadowOffsetY * Scaling);

        var shadowBodyRectangle = new Rectangle(renderRectangle.X + scaledOffsetX - scaledSpread, renderRectangle.Y + scaledOffsetY - scaledSpread,
            ScaledWidth + scaledSpread * 2, ScaledHeight + scaledSpread * 2);
        var premultipliedShadowColor = Color.FromNonPremultiplied(shadowColor.R, shadowColor.G, shadowColor.B, effectiveAlpha);

        Renderer.PushSettings(new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, null, null, null));

        DrawDropShadowNineSlice(shadowBodyRectangle, textureRadius, scaledRadius, premultipliedShadowColor);

        Renderer.PopSettings();
    }

    private void EnsureDropShadowTexture(int radius)
    {
        if (dropShadowTexture != null &&
            !dropShadowTexture.IsDisposed &&
            dropShadowTexture.GraphicsDevice == GraphicsDevice &&
            dropShadowTextureRadius == radius)
        {
            return;
        }

        dropShadowTexture?.Dispose();
        dropShadowTextureRadius = radius;

        int textureSize = radius * 2 + DropShadowTextureCoreSize;
        var textureData = new Color[textureSize * textureSize];
        float standardDeviation = Math.Max(1.0f, radius / 3.0f);
        float gaussianDivisor = 2.0f * standardDeviation * standardDeviation;
        int coreEnd = radius + DropShadowTextureCoreSize - 1;

        for (int y = 0; y < textureSize; y++)
        {
            int distanceY = y < radius ? radius - y : Math.Max(0, y - coreEnd);

            for (int x = 0; x < textureSize; x++)
            {
                int distanceX = x < radius ? radius - x : Math.Max(0, x - coreEnd);
                float alpha = (float)Math.Exp(-(distanceX * distanceX + distanceY * distanceY) / gaussianDivisor);
                byte alphaByte = (byte)Math.Clamp((int)Math.Round(alpha * 255.0f), 0, 255);

                // Store premultiplied white so any theme color can safely tint it
                // with SpriteBatch's standard premultiplied alpha blending.
                textureData[y * textureSize + x] = new Color(alphaByte, alphaByte, alphaByte, alphaByte);
            }
        }

        dropShadowTexture = new Texture2D(GraphicsDevice, textureSize, textureSize);
        dropShadowTexture.SetData(textureData);
    }

    private static void DrawDropShadowNineSlice(Rectangle bodyRectangle, int sourceRadius, int destinationRadius, Color color)
    {
        int sourceMiddle = DropShadowTextureCoreSize;
        int sourceRight = sourceRadius + sourceMiddle;
        int destinationRight = bodyRectangle.Right;
        int destinationBottom = bodyRectangle.Bottom;

        DrawDropShadowSlice(new Rectangle(0, 0, sourceRadius, sourceRadius),
            new Rectangle(bodyRectangle.X - destinationRadius, bodyRectangle.Y - destinationRadius, destinationRadius, destinationRadius), color);
        DrawDropShadowSlice(new Rectangle(sourceRadius, 0, sourceMiddle, sourceRadius),
            new Rectangle(bodyRectangle.X, bodyRectangle.Y - destinationRadius, bodyRectangle.Width, destinationRadius), color);
        DrawDropShadowSlice(new Rectangle(sourceRight, 0, sourceRadius, sourceRadius),
            new Rectangle(destinationRight, bodyRectangle.Y - destinationRadius, destinationRadius, destinationRadius), color);
        DrawDropShadowSlice(new Rectangle(0, sourceRadius, sourceRadius, sourceMiddle),
            new Rectangle(bodyRectangle.X - destinationRadius, bodyRectangle.Y, destinationRadius, bodyRectangle.Height), color);
        DrawDropShadowSlice(new Rectangle(sourceRadius, sourceRadius, sourceMiddle, sourceMiddle), bodyRectangle, color);
        DrawDropShadowSlice(new Rectangle(sourceRight, sourceRadius, sourceRadius, sourceMiddle),
            new Rectangle(destinationRight, bodyRectangle.Y, destinationRadius, bodyRectangle.Height), color);
        DrawDropShadowSlice(new Rectangle(0, sourceRight, sourceRadius, sourceRadius),
            new Rectangle(bodyRectangle.X - destinationRadius, destinationBottom, destinationRadius, destinationRadius), color);
        DrawDropShadowSlice(new Rectangle(sourceRadius, sourceRight, sourceMiddle, sourceRadius),
            new Rectangle(bodyRectangle.X, destinationBottom, bodyRectangle.Width, destinationRadius), color);
        DrawDropShadowSlice(new Rectangle(sourceRight, sourceRight, sourceRadius, sourceRadius),
            new Rectangle(destinationRight, destinationBottom, destinationRadius, destinationRadius), color);
    }

    private static void DrawDropShadowSlice(Rectangle sourceRectangle, Rectangle destinationRectangle, Color color)
    {
        if (sourceRectangle.Width <= 0 || sourceRectangle.Height <= 0 ||
            destinationRectangle.Width <= 0 || destinationRectangle.Height <= 0)
        {
            return;
        }

        Renderer.DrawTexture(dropShadowTexture, sourceRectangle, destinationRectangle, color);
    }

    private bool TryDrawGlassBackdrop()
    {
        if (!EnableGlassEffect ||
            UserSettings.Instance == null ||
            !UserSettings.Instance.EnableWindowGlassEffect ||
            backdropBlurEffect == null ||
            BackdropRenderTarget == null ||
            Width <= 0 || Height <= 0)
        {
            DisposeGlassRenderTargets();
            return false;
        }

        var editorUISettings = (CustomUISettings)UISettings.ActiveSettings;
        float blurRadius = editorUISettings.WindowBlurRadius;
        float backdropSaturation = editorUISettings.WindowGlassBackdropSaturation;
        float noiseStrength = editorUISettings.WindowGlassNoiseStrength;
        var tint = editorUISettings.WindowGlassTint;
        var reflectionColor = editorUISettings.WindowGlassReflectionColor;
        float reflectionIntensity = editorUISettings.WindowGlassReflectionIntensity;

        if (blurRadius == 0f && reflectionIntensity == 0f)
        {
            DisposeGlassRenderTargets();
            return false;
        }

        if (!IsForeground)
        {
            tint = new Color(tint.R * 3 / 4, tint.G * 3 / 4, tint.B * 3 / 4, Math.Min(255, tint.A + 35));
            reflectionIntensity *= 0.18f;
        }

        EnsureGlassRenderTargets();

        var sourceRectangle = RenderRectangle();
        sourceRectangle.Width = ScaledWidth;
        sourceRectangle.Height = ScaledHeight;

        var shaderSettings = new SpriteBatchSettings(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, null, null, backdropBlurEffect);

        SetDownsampleParameters(BackdropRenderTarget, sourceRectangle, halfScaleRenderTarget);
        DrawGlassPass(BackdropRenderTarget, sourceRectangle, halfScaleRenderTarget, shaderSettings);

        float halfScaleBlurRadiusX = blurRadius * 0.5f * halfScaleRenderTarget.Width / sourceRectangle.Width;
        float halfScaleBlurRadiusY = blurRadius * 0.5f * halfScaleRenderTarget.Height / sourceRectangle.Height;

        SetBlurParameters(halfScaleRenderTarget, halfScaleBlurRadiusX, Vector2.UnitX);
        DrawGlassPass(halfScaleRenderTarget, new Rectangle(0, 0, halfScaleRenderTarget.Width, halfScaleRenderTarget.Height),
            halfScaleBlurRenderTarget, shaderSettings);

        SetBlurParameters(halfScaleBlurRenderTarget, halfScaleBlurRadiusY, Vector2.UnitY);
        DrawGlassPass(halfScaleBlurRenderTarget, new Rectangle(0, 0, halfScaleBlurRenderTarget.Width, halfScaleBlurRenderTarget.Height),
            halfScaleRenderTarget, shaderSettings);

        var halfScaleRectangle = new Rectangle(0, 0, halfScaleRenderTarget.Width, halfScaleRenderTarget.Height);
        SetDownsampleParameters(halfScaleRenderTarget, halfScaleRectangle, quarterScaleRenderTarget);
        DrawGlassPass(halfScaleRenderTarget, halfScaleRectangle, quarterScaleRenderTarget, shaderSettings);

        float quarterScaleBlurRadiusX = blurRadius * quarterScaleRenderTarget.Width / sourceRectangle.Width;
        float quarterScaleBlurRadiusY = blurRadius * quarterScaleRenderTarget.Height / sourceRectangle.Height;

        SetBlurParameters(quarterScaleRenderTarget, quarterScaleBlurRadiusX, Vector2.UnitX);
        DrawGlassPass(quarterScaleRenderTarget, new Rectangle(0, 0, quarterScaleRenderTarget.Width, quarterScaleRenderTarget.Height),
            quarterScaleBlurRenderTarget, shaderSettings);

        SetBlurParameters(quarterScaleBlurRenderTarget, quarterScaleBlurRadiusY, Vector2.UnitY);
        backdropBlurEffect.Parameters["GlassTint"].SetValue(tint.ToVector4());
        backdropBlurEffect.Parameters["BackdropSaturation"].SetValue(backdropSaturation);
        backdropBlurEffect.Parameters["NoiseStrength"].SetValue(noiseStrength);
        backdropBlurEffect.Parameters["ReflectionColor"].SetValue(reflectionColor.ToVector4());
        backdropBlurEffect.Parameters["ReflectionIntensity"].SetValue(reflectionIntensity);
        backdropBlurEffect.Parameters["ScreenUVOffset"].SetValue(new Vector2(sourceRectangle.X / (float)BackdropRenderTarget.Width,
            sourceRectangle.Y / (float)BackdropRenderTarget.Height));
        backdropBlurEffect.Parameters["ScreenUVScale"].SetValue(new Vector2(sourceRectangle.Width / (float)BackdropRenderTarget.Width,
            sourceRectangle.Height / (float)BackdropRenderTarget.Height));
        backdropBlurEffect.Parameters["ScreenSize"].SetValue(new Vector2(BackdropRenderTarget.Width, BackdropRenderTarget.Height));
        backdropBlurEffect.CurrentTechnique = backdropBlurEffect.Techniques["GlassMaterial"];

        Renderer.PushSettings(shaderSettings);

        DrawTexture(quarterScaleBlurRenderTarget, new Rectangle(0, 0, quarterScaleBlurRenderTarget.Width, quarterScaleBlurRenderTarget.Height),
            new Rectangle(0, 0, Width, Height), Color.White);

        Renderer.PopSettings();

        return true;
    }

    private void SetDownsampleParameters(Texture2D sourceTexture, Rectangle sourceRectangle, RenderTarget2D destination)
    {
        var downsampleStep = new Vector2(sourceRectangle.Width / (2.0f * destination.Width * sourceTexture.Width),
            sourceRectangle.Height / (2.0f * destination.Height * sourceTexture.Height));

        backdropBlurEffect.Parameters["DownsampleStep"].SetValue(downsampleStep);
        backdropBlurEffect.CurrentTechnique = backdropBlurEffect.Techniques["Downsample"];
    }

    private void SetBlurParameters(Texture2D sourceTexture, float blurRadius, Vector2 blurDirection)
    {
        backdropBlurEffect.Parameters["TexelSize"].SetValue(new Vector2(1.0f / sourceTexture.Width, 1.0f / sourceTexture.Height));
        backdropBlurEffect.Parameters["BlurRadius"].SetValue(blurRadius);
        backdropBlurEffect.Parameters["BlurDirection"].SetValue(blurDirection);
        backdropBlurEffect.CurrentTechnique = backdropBlurEffect.Techniques["BlurOnly"];
    }

    private void DrawGlassPass(Texture2D sourceTexture, Rectangle sourceRectangle, RenderTarget2D destination, SpriteBatchSettings shaderSettings)
    {
        Renderer.PushRenderTarget(destination, shaderSettings);
        GraphicsDevice.Clear(Color.Transparent);

        DrawTexture(sourceTexture, sourceRectangle, new Rectangle(0, 0, destination.Width, destination.Height), Color.White);

        Renderer.PopRenderTarget();
    }

    private void EnsureGlassRenderTargets()
    {
        int halfWidth = Math.Max(2, (Width + 1) / 2);
        int halfHeight = Math.Max(2, (Height + 1) / 2);
        int quarterWidth = Math.Max(2, (Width + 3) / 4);
        int quarterHeight = Math.Max(2, (Height + 3) / 4);

        if (RenderTargetHasSize(halfScaleRenderTarget, halfWidth, halfHeight) &&
            RenderTargetHasSize(halfScaleBlurRenderTarget, halfWidth, halfHeight) &&
            RenderTargetHasSize(quarterScaleRenderTarget, quarterWidth, quarterHeight) &&
            RenderTargetHasSize(quarterScaleBlurRenderTarget, quarterWidth, quarterHeight))
        {
            return;
        }

        DisposeGlassRenderTargets();
        halfScaleRenderTarget = CreateGlassRenderTarget(halfWidth, halfHeight);
        halfScaleBlurRenderTarget = CreateGlassRenderTarget(halfWidth, halfHeight);
        quarterScaleRenderTarget = CreateGlassRenderTarget(quarterWidth, quarterHeight);
        quarterScaleBlurRenderTarget = CreateGlassRenderTarget(quarterWidth, quarterHeight);
    }

    private static bool RenderTargetHasSize(RenderTarget2D renderTarget, int width, int height) =>
        renderTarget != null &&
        !renderTarget.IsDisposed &&
        renderTarget.Width == width &&
        renderTarget.Height == height;

    private RenderTarget2D CreateGlassRenderTarget(int width, int height) =>
        new RenderTarget2D(GraphicsDevice, width, height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

    private void DisposeGlassRenderTargets()
    {
        halfScaleRenderTarget?.Dispose();
        halfScaleBlurRenderTarget?.Dispose();
        quarterScaleRenderTarget?.Dispose();
        quarterScaleBlurRenderTarget?.Dispose();

        halfScaleRenderTarget = null;
        halfScaleBlurRenderTarget = null;
        quarterScaleRenderTarget = null;
        quarterScaleBlurRenderTarget = null;
    }
}
