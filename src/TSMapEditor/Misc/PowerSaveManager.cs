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
    // 2) Skip rendering frames when there is nothing animated on the screen and no user input,
    //    regardless of whether our window is active or not.
    //    This only affects rendering as Draw calls are skipped; TargetElapsedTime is kept as normal
    //    so any input from the user is handled at regular speed, resulting in a quick screen refresh.
    //
    // The input-based refresh is handled by XNAUI's WindowManager: it calls RequestRedraw through the
    // IPowerSaveManager interface whenever it detects input.

    // Keep running updates between redraws so input and UI state can request the next frame.
    // Use a slower update interval while the window is inactive.
    private const double InactiveUpdateIntervalMilliseconds = 100.0;

    public PowerSaveManager(Game game)
    {
        this.game = game;
        wasActiveOnPreviousFrame = game.IsActive;
        UpdateTargetElapsedTime(wasActiveOnPreviousFrame);
    }

    private readonly Game game;

    private bool redrawRequested;
    private bool wasActiveOnPreviousFrame;

    public void RequestRedraw()
    {
        redrawRequested = true;
    }

    private void UpdateTargetElapsedTime(bool isActive)
    {
        double intervalMilliseconds = isActive ? 1000.0 / UserSettings.Instance.TargetFPS : InactiveUpdateIntervalMilliseconds;
        game.TargetElapsedTime = TimeSpan.FromMilliseconds(intervalMilliseconds);
        RequestRedraw();
    }

    public void Update()
    {
        bool isActive = game.IsActive;
        if (isActive == wasActiveOnPreviousFrame)
            return;

        wasActiveOnPreviousFrame = isActive;
        UpdateTargetElapsedTime(isActive);
    }

    public bool ShouldDraw()
    {
        if (!redrawRequested)
            return false;

        redrawRequested = false;

        return true;
    }
}
