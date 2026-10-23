using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Mutations;
using MapEditorLibrary.Mutations.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TSMapEditor.Tests;

[TestClass]
public class TerrainLineMutationTests
{
    [TestMethod]
    [DataRow(1, 1, 7)]
    [DataRow(3, 3, 27)]
    [DataRow(5, 5, 55)]
    [DataRow(2, 4, 32)]
    public void LineUsesSelectedBrushFootprint(int width, int height, int expectedCount)
    {
        var fixture = new TerrainFixture(width, height);
        var mutation = new PlaceTerrainLineMutation(fixture, fixture.Map.GetTile(20, 20), Direction.SE, 6, fixture.Tile);
        mutation.Perform();

        Assert.AreEqual(expectedCount, fixture.PaintedCells().Count);
        Assert.IsTrue(fixture.PaintedCells().Contains(new Point2D(26 + width - 1, 20 + height - 1)));
    }

    [TestMethod]
    [DataRow(Direction.SE, 45, 20, 20, 29, 25)]
    [DataRow(Direction.S, 68, 20, 20, 29, 31)]
    [DataRow(Direction.SW, 36, 20, 20, 23, 31)]
    [DataRow(Direction.W, 74, 14, 20, 23, 31)]
    [DataRow(Direction.NW, 45, 14, 20, 23, 25)]
    [DataRow(Direction.N, 68, 14, 14, 23, 25)]
    [DataRow(Direction.NE, 36, 20, 14, 23, 25)]
    [DataRow(Direction.E, 74, 20, 14, 29, 25)]
    public void SparseMultiCellLinesUseBrushInEveryDirection(Direction direction, int count, int minX, int minY, int maxX, int maxY)
    {
        var fixture = new TerrainFixture(2, 3, 2, 2, missingSubTile: 1);
        string[] before = fixture.Snapshot();
        var mutation = new PlaceTerrainLineMutation(fixture, fixture.Map.GetTile(20, 20), direction, 6, fixture.Tile);
        mutation.Perform();
        var cells = fixture.PaintedCells();
        Assert.AreEqual(count, cells.Count);
        Assert.AreEqual(minX, cells.Min(p => p.X));
        Assert.AreEqual(minY, cells.Min(p => p.Y));
        Assert.AreEqual(maxX, cells.Max(p => p.X));
        Assert.AreEqual(maxY, cells.Max(p => p.Y));
        string[] after = fixture.Snapshot();
        mutation.Undo();
        CollectionAssert.AreEqual(before, fixture.Snapshot());
        mutation.Perform();
        CollectionAssert.AreEqual(after, fixture.Snapshot());
    }

    [TestMethod]
    public void MultiCellBrushCopiesCompleteTilesAndSkipsMissingSubTiles()
    {
        var fixture = new TerrainFixture(2, 3, 2, 2, missingSubTile: 1);
        var mutation = new PlaceTerrainLineMutation(fixture, fixture.Map.GetTile(20, 20), Direction.SE, 0, fixture.Tile);
        mutation.Perform();

        Assert.AreEqual(18, fixture.PaintedCells().Count);
        Assert.AreEqual(0, fixture.Map.GetTile(21, 20).TileIndex);
        Assert.AreEqual(0, fixture.Map.GetTile(23, 20).TileIndex);
        Assert.AreEqual(2, fixture.Map.GetTile(22, 25).SubTileIndex);
        Assert.AreEqual(3, fixture.Map.GetTile(23, 25).SubTileIndex);
    }

    [TestMethod]
    public void OverlappingBrushesRaiseEachCellOnlyOnceAndUndoRestoresExactData()
    {
        var fixture = new TerrainFixture(3, 3, tileHeightOffset: 1);
        string[] before = fixture.Snapshot();
        var manager = new MutationManager();
        manager.PerformMutation(new PlaceTerrainLineMutation(fixture, fixture.Map.GetTile(20, 20), Direction.SE, 6, fixture.Tile));
        Assert.AreEqual(27, fixture.PaintedCells().Count);
        Assert.IsTrue(fixture.PaintedCells().All(p => fixture.Map.GetTile(p).Level == 4));
        string[] after = fixture.Snapshot();
        manager.Undo();
        CollectionAssert.AreEqual(before, fixture.Snapshot());
        manager.Redo();
        CollectionAssert.AreEqual(after, fixture.Snapshot());
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void RedoUsesBrushAndAutoLATSettingsFromOriginalPlacement(bool autoLAT)
    {
        var fixture = new TerrainFixture(3, 3, autoLAT: autoLAT);
        var manager = new MutationManager();
        manager.PerformMutation(new PlaceTerrainLineMutation(fixture, fixture.Map.GetTile(20, 20), Direction.SE, 6, fixture.Tile));
        string[] after = fixture.Snapshot();
        manager.Undo();
        fixture.BrushSize = new BrushSize(5, 5);
        fixture.AutoLATEnabled = !autoLAT;
        manager.Redo();
        CollectionAssert.AreEqual(after, fixture.Snapshot());
    }

    [TestMethod]
    public void BrushClipsAtMapBoundaryAndUndoDoesNotTouchOtherCells()
    {
        var fixture = new TerrainFixture(5, 5, 2, 2);
        string[] before = fixture.Snapshot();
        var mutation = new PlaceTerrainLineMutation(fixture, fixture.Map.GetTile(62, 62), Direction.SE, 6, fixture.Tile);
        mutation.Perform();
        Assert.AreEqual(4, fixture.PaintedCells().Count);
        mutation.Undo();
        CollectionAssert.AreEqual(before, fixture.Snapshot());
    }

    [TestMethod]
    public void AutoLATTransitionsMatchFullBrushAndUndoRestoresNeighbor()
    {
        var fixture = new TerrainFixture(3, 3, autoLAT: true);
        fixture.Map.GetTile(19, 20).ChangeTileIndex(1, 0);
        string[] before = fixture.Snapshot();
        var manager = new MutationManager();
        manager.PerformMutation(new PlaceTerrainLineMutation(fixture, fixture.Map.GetTile(20, 20), Direction.SE, 6, fixture.Tile));
        var ground = fixture.PaintedCells();
        Assert.AreEqual(28, ground.Count);
        foreach (Point2D p in ground)
        {
            int missingNeighbors = (ground.Contains(p + new Point2D(0, -1)) ? 0 : 1)
                | (ground.Contains(p + new Point2D(1, 0)) ? 0 : 2)
                | (ground.Contains(p + new Point2D(0, 1)) ? 0 : 4)
                | (ground.Contains(p + new Point2D(-1, 0)) ? 0 : 8);
            Assert.AreEqual(missingNeighbors == 0 ? 1 : 2 + missingNeighbors, fixture.Map.GetTile(p).TileIndex, $"Incorrect transition at {p}");
        }

        string[] after = fixture.Snapshot();
        manager.Undo();
        CollectionAssert.AreEqual(before, fixture.Snapshot());
        manager.Redo();
        CollectionAssert.AreEqual(after, fixture.Snapshot());
    }
}
