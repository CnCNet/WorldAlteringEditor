using MapEditorLibrary.CCEngine;
using MapEditorLibrary.CCEngine.TileData;
using MapEditorLibrary.Models;

namespace MapEditorLibrary;

/// <summary>
/// An interface for an object that can be used to fetch 
/// game logic related information about a theater.
/// </summary>
public interface ITheater
{
    int GetTileSetId(int uniqueTileIndex);
    int TileCount { get; }
    ITileImage GetTile(int id);
    int GetOverlayFrameCount(OverlayType overlayType);
    Theater Theater { get; }
}
