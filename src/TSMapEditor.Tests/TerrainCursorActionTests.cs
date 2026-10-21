using System.Runtime.CompilerServices;
using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Graphics;
using MapEditorLibrary.Misc;
using MapEditorLibrary.Models;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Rampastring.XNAUI;
using Rampastring.XNAUI.Input;
using TSMapEditor.Rendering;
using TSMapEditor.UI;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.Tests;

[TestClass]
public class TerrainCursorActionTests
{
    [TestInitialize]
    public void InitializeKeyboardCommands() => KeyboardCommands.Instance = new KeyboardCommands();

    [TestMethod]
    [DataRow(1, 1, 3, 3, 1, false)]
    [DataRow(2, 2, 3, 3, 1, true)]
    [DataRow(3, 2, 2, 4, 6, true)]
    public void ShortAndLongPreviewsMatchCommittedTerrain(int tileWidth, int tileHeight, int brushWidth, int brushHeight, int length, bool autoLAT)
    {
        var fixture = new CursorFixture(brushWidth, brushHeight, tileWidth, tileHeight, autoLAT);
        var action = new PlaceTerrainCursorAction(fixture) { Tile = fixture.Tile };
        string[] before = fixture.Snapshot();
        action.LeftDown(new Point2D(25, 25));
        action.PreMapDraw(new Point2D(25 + length, 25));
        string[] preview = fixture.VisibleSnapshot();
        action.PostMapDraw(new Point2D(25 + length, 25));
        CollectionAssert.AreEqual(before, fixture.Snapshot());
        action.LeftClick(new Point2D(25 + length, 25));
        Assert.AreEqual(1, fixture.MutationManager.UndoList.Count);
        CollectionAssert.AreEqual(preview, fixture.Snapshot());
        fixture.MutationManager.Undo();
        CollectionAssert.AreEqual(before, fixture.Snapshot());
        fixture.MutationManager.Redo();
        CollectionAssert.AreEqual(preview, fixture.Snapshot());
    }

    [TestMethod]
    public void AdjustedEndpointCanCommitShortMultiCellLine()
    {
        var fixture = new CursorFixture(3, 3, 2, 2);
        var action = new PlaceTerrainCursorAction(fixture) { Tile = fixture.Tile };
        action.LeftDown(new Point2D(25, 25));
        action.LeftClick(new Point2D(24, 24));
        Assert.AreEqual(1, fixture.MutationManager.UndoList.Count);
        Assert.AreEqual(47, fixture.PaintedCells().Count);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CancellingPreviewRestoresMapWithoutAddingUndoEntry(bool inactive)
    {
        var fixture = new CursorFixture(3, 3, 2, 2, true);
        var action = new PlaceTerrainCursorAction(fixture) { Tile = fixture.Tile };
        string[] before = fixture.Snapshot();
        action.LeftDown(new Point2D(25, 25));
        action.PreMapDraw(new Point2D(31, 25));
        Assert.IsTrue(fixture.PaintedCells().Count > 0);
        fixture.BrushSize = new BrushSize(5, 5);
        if (inactive)
            action.InactiveUpdate();
        else
            action.OnActionExit();

        CollectionAssert.AreEqual(before, fixture.Snapshot());
        Assert.AreEqual(0, fixture.MutationManager.UndoList.Count);
    }

    [TestMethod]
    public void ReleasingShiftCommitsTheBrushLine()
    {
        var fixture = new CursorFixture(3, 3);
        var action = new PlaceTerrainCursorAction(fixture) { Tile = fixture.Tile };
        action.LeftDown(new Point2D(25, 25));
        fixture.WindowManager.Keyboard.DownKeys.Clear();
        action.Update(new Point2D(31, 25));
        Assert.AreEqual(1, fixture.MutationManager.UndoList.Count);
        Assert.AreEqual(27, fixture.PaintedCells().Count);
    }

    [TestMethod]
    [DataRow(1, 1)]
    [DataRow(2, 2)]
    public void ZeroLengthPreviewShowsBrushAndLeavesNoCommittedChanges(int tileWidth, int tileHeight)
    {
        var fixture = new CursorFixture(3, 3, tileWidth, tileHeight);
        var action = new PlaceTerrainCursorAction(fixture) { Tile = fixture.Tile };
        string[] before = fixture.Snapshot();
        action.LeftDown(new Point2D(25, 25));
        action.PreMapDraw(new Point2D(25, 25));
        Assert.AreEqual(9 * tileWidth * tileHeight, fixture.PaintedCells().Count);
        action.PostMapDraw(new Point2D(25, 25));
        action.LeftClick(new Point2D(25, 25));
        CollectionAssert.AreEqual(before, fixture.Snapshot());
        Assert.AreEqual(0, fixture.MutationManager.UndoList.Count);
    }
}

internal class CursorFixture : TerrainFixture, ICursorActionTarget
{
    internal CursorFixture(int brushWidth, int brushHeight, int tileWidth = 1, int tileHeight = 1, bool autoLAT = false)
        : base(brushWidth, brushHeight, tileWidth, tileHeight, autoLAT)
    {
        // Input handlers need keyboard state, not a native window or graphics device.
        WindowManager = (WindowManager)RuntimeHelpers.GetUninitializedObject(typeof(WindowManager));
        var keyboard = (RKeyboard)RuntimeHelpers.GetUninitializedObject(typeof(RKeyboard));
        keyboard.DownKeys = new List<Keys> { Keys.LeftShift };
        keyboard.PressedKeys = new List<Keys>();
        typeof(WindowManager).GetProperty(nameof(WindowManager.Keyboard)).SetValue(WindowManager, keyboard);
        var cursorType = typeof(WindowManager).GetProperty(nameof(WindowManager.Cursor));
        cursorType.SetValue(WindowManager, RuntimeHelpers.GetUninitializedObject(cursorType.PropertyType));
    }

    internal string[] VisibleSnapshot()
    {
        var snapshot = new string[Size * Size];
        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                MapTile cell = Map.GetTile(x, y);
                int index = cell.PreviewTileImage?.TileID ?? cell.TileIndex;
                int subTile = cell.PreviewTileImage != null ? cell.PreviewSubTileIndex : cell.SubTileIndex;
                int level = cell.PreviewLevel >= 0 ? cell.PreviewLevel : cell.Level;
                snapshot[y * Size + x] = $"{index}:{subTile}:{level}";
            }
        }

        return snapshot;
    }

    public WindowManager WindowManager { get; }
    public EditorGraphics EditorGraphics => null;
    public MutationManager MutationManager { get; } = new();
    public IMutationTarget MutationTarget => this;
    public bool Is2DMode => true;
    public DeletionMode DeletionMode => default;
    public CopiedMapData CopiedMapData { get; set; }
    public TechnoBase TechnoUnderCursor { get; set; }
    public TheaterGraphics TheaterGraphics => null;
    public Camera Camera => null;
    public Texture2D MinimapTexture => null;
    public HashSet<object> MinimapUsers { get; } = new();
}
