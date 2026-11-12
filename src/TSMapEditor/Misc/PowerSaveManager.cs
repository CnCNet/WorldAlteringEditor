using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using System;
using TSMapEditor.Settings;

namespace TSMapEditor.Misc;

/// <summary>
/// Manages the editor's frame rate.
/// </summary>
public class PowerSaveManager : IPowerSaveManager
{
    // We have two ways to save power:
    //
    // 1) Throttle the application's frame rate when it is not the active window. This affects both rendering AND input latency.
    //
    // 2) Skip rendering most frames when there is nothing animated on the screen and no user input,
    //    regardless of whether our window is active or not.
    //    This only affects rendering as Draw calls are skipped; TargetElapsedTime is kept as normal
    //    so any input from the user is handled at regular speed, resulting in a quick screen refresh.
    //
    // The input-based refresh is handled by XNAUI's WindowManager: it calls RequestRedraw through the
    // IPowerSaveManager interface whenever it detects input.

    // The following times are all expressed in milliseconds:

    /// <summary>
    /// TargetElapsedTime when the application window is inactive.
    /// </summary>
    private const double InactivePowerSavingTime = 100.0;

    /// <summary>
    /// How long, at most, to wait between re-rendering the screen when we have nothing new to render.
    /// </summary>
    private const double RenderingPowerSavingTime = 500.0;

    public PowerSaveManager(Game game)
    {
        this.game = game;
        SetTargetFPS();
    }

    private readonly Game game;

    private bool redrawRequested = false;
    private bool wasActiveOnPreviousFrame;
    private DateTime lastDrawTime;

    public void RequestRedraw()
    {
        redrawRequested = true;
    }

    private void SetTargetFPS()
    {
        game.TargetElapsedTime = TimeSpan.FromMilliseconds(1000.0 / UserSettings.Instance.TargetFPS);
        RequestRedraw();
    }

    public void Update()
    {
        // If we changed from active to inactive or back, update our target framerate.
        if (game.IsActive != wasActiveOnPreviousFrame)
        {
            if (!game.IsActive)
                game.TargetElapsedTime = TimeSpan.FromMilliseconds(InactivePowerSavingTime); // Don't run at max FPS if we're not the active window
            else
                SetTargetFPS(); // If we just became active, restore full framerate

            wasActiveOnPreviousFrame = game.IsActive;
        }
    }

    public bool ShouldDraw()
    {
        DateTime now = DateTime.Now;
        if (now - lastDrawTime > TimeSpan.FromMilliseconds(RenderingPowerSavingTime))
            RequestRedraw();

        if (redrawRequested)
        {
            lastDrawTime = DateTime.Now;
            redrawRequested = false;
            return true;
        }

        return false;
    }
}
