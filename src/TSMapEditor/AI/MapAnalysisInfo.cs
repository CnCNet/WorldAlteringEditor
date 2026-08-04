using System.Collections.Generic;

namespace TSMapEditor.AI;

public sealed class MapCellArea
{
    public MapCellArea(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }
}

public sealed class MapResourceFieldInfo
{
    public MapResourceFieldInfo(long totalValue, int cellCount, MapCellArea area)
    {
        TotalValue = totalValue;
        CellCount = cellCount;
        Area = area;
    }

    public long TotalValue { get; }
    public int CellCount { get; }
    public MapCellArea Area { get; }
}

public sealed class MapValidationResult
{
    public MapValidationResult(int revision, List<string> issues, List<MapCellArea> underdetailedAreas)
    {
        Revision = revision;
        Issues = issues;
        UnderdetailedAreas = underdetailedAreas;
    }

    public int Revision { get; }
    public bool HasIssues => Issues.Count > 0;
    public List<string> Issues { get; }
    public List<MapCellArea> UnderdetailedAreas { get; }
}
