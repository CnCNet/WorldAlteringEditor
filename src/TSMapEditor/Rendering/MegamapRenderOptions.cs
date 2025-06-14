using System;

namespace TSMapEditor.Rendering
{
    [Flags]
    public enum MegamapRenderOptions
    {
        None = 0,
        IncludeOnlyVisibleArea = 1,
        EmphasizeResources = 2,
        MarkPlayerSpots = 4,
        All = IncludeOnlyVisibleArea + EmphasizeResources + MarkPlayerSpots,
    }
}
