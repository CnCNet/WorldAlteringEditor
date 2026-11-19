using MapEditorLibrary.CCEngine;
using Microsoft.Xna.Framework;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Graphics;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Settings;

namespace TSMapEditor.UI.Controls;

public class TileSetListBox : XNAListBox
{
    public TileSetListBox(WindowManager windowManager, int tileSetCount) : base(windowManager)
    {
        tileSetIsFavourite = new bool[tileSetCount];
        favouriteTileSetTexture = AssetLoader.LoadUITexture("star.png");
    }

    private readonly bool[] tileSetIsFavourite;

    private readonly TextureReference favouriteTileSetTexture;

    public bool IsTileSetFavourite(TileSet tileSet) => tileSetIsFavourite[tileSet.Index];

    public void SetTileSetAsFavourite(TileSet tileSet)
    {
        if (!IsTileSetFavourite(tileSet))
        {
            tileSetIsFavourite[tileSet.Index] = true;
            UserSettings.Instance.PinnedTileSets.Add(tileSet.SetName);
            _ = UserSettings.Instance.SaveSettingsAsync();
        }
    }

    public void ClearFavouriteStatus(TileSet tileSet)
    {
        if (IsTileSetFavourite(tileSet))
        {
            tileSetIsFavourite[tileSet.Index] = false;
            UserSettings.Instance.PinnedTileSets.Remove(tileSet.SetName);
            _ = UserSettings.Instance.SaveSettingsAsync();
        }
    }

    public void ClearAllFavourites()
    {
        for (int i = 0; i < tileSetIsFavourite.Length; i++)
            tileSetIsFavourite[i] = false;

        UserSettings.Instance.PinnedTileSets.ClearAll();
        _ = UserSettings.Instance.SaveSettingsAsync();
    }

    protected override void DrawListBoxItem(int index, int y)
    {
        base.DrawListBoxItem(index, y);
        var tileSet = (TileSet)Items[index].Tag;

        if (!IsTileSetFavourite(tileSet))
            return;

        int lineWidth = Width - 2 - (EnableScrollbar && ScrollBar.IsDrawn() ? ScrollBar.Width : 0);

        int textureYOffset = (LineHeight - favouriteTileSetTexture.Height) / 2;

        DrawTexture(favouriteTileSetTexture, new Point(lineWidth - 1 - favouriteTileSetTexture.Width, y + textureYOffset), Color.White);
    }
}
