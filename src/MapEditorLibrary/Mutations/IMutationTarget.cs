using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;

namespace MapEditorLibrary.Mutations;

/// <summary>
/// An interface for an object that mutations use to interact with the map.
/// </summary>
public interface IMutationTarget
{
    Map Map { get; }
    ITheater TheaterGraphicsData { get; }
    void AddRefreshPoint(Point2D point, int size = 10);
    void InvalidateMap();
    House ObjectOwner { get; }
    BrushSize BrushSize { get; }
    Randomizer Randomizer { get; }
    bool AutoLATEnabled { get; }
    LightingPreviewMode LightingPreviewState { get; }
    bool LightDisabledLightSources { get; }
    bool OnlyPaintOnClearGround { get; }
}
