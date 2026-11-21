using System.ComponentModel;
using MapEditorLibrary.Mutations;

namespace MapEditorMCP.Infos;

internal static class MapMutationKinds
{
    public const string Map = "Map";
    public const string Scripting = "Scripting";
}

internal sealed class MapMutationHistoryEntry
{
    public MapMutationHistoryEntry(MutationHistoryMetadata metadata, int revision, string editorToolName, string description,
        string kind, bool canUndo, bool canRedo, string blockReason)
    {
        HasMetadata = metadata != null;
        MutationId = metadata?.MutationId;
        Revision = revision;
        CreatedRevision = metadata?.CreatedRevision;
        CreatedAtUtc = metadata?.CreatedAtUtc;
        Origin = metadata?.Origin ?? "Editor";
        ToolName = metadata?.ToolName ?? editorToolName;
        Description = description;
        Kind = kind;
        AffectedCells = metadata?.AffectedCells;
        CanUndo = canUndo && HasMetadata && blockReason == null;
        CanRedo = canRedo && HasMetadata && blockReason == null;
        BlockReason = blockReason;
    }

    public bool HasMetadata { get; }
    public long? MutationId { get; }
    public int Revision { get; }
    public int? CreatedRevision { get; }
    public DateTime? CreatedAtUtc { get; }
    public string Origin { get; }
    public string ToolName { get; }
    public string Description { get; }

    [Description("\"Map\" for terrain, overlay, object, and other cell-based edits; \"Scripting\" for TaskForce, Script, " +
        "TeamType, AITrigger, local variable, Trigger, and Tag change sets.")]
    public string Kind { get; }

    public MutationAffectedCells AffectedCells { get; }
    public bool CanUndo { get; }
    public bool CanRedo { get; }

    [Description("Set when this is the next entry to undo or redo but doing so is not safe because the map was changed " +
        "outside the undo history since. Explains what changed.")]
    public string BlockReason { get; }
}

internal sealed class MapMutationHistoryInfo
{
    public MapMutationHistoryInfo(int revision, int undoCount, int redoCount,
        List<MapMutationHistoryEntry> undoHistory, List<MapMutationHistoryEntry> redoHistory)
    {
        Revision = revision;
        UndoCount = undoCount;
        RedoCount = redoCount;
        UndoHistory = undoHistory;
        RedoHistory = redoHistory;
    }

    public int Revision { get; }
    public int UndoCount { get; }
    public int RedoCount { get; }
    public bool CanUndo => UndoHistory.Count > 0 && UndoHistory[0].CanUndo;
    public bool CanRedo => RedoHistory.Count > 0 && RedoHistory[0].CanRedo;
    public List<MapMutationHistoryEntry> UndoHistory { get; }
    public List<MapMutationHistoryEntry> RedoHistory { get; }
}

internal sealed class MapMutationOperationResult
{
    public MapMutationOperationResult(int revision, MapMutationHistoryEntry mutation, int undoCount, int redoCount, bool canUndo, bool canRedo)
    {
        Revision = revision;
        Mutation = mutation;
        UndoCount = undoCount;
        RedoCount = redoCount;
        CanUndo = canUndo;
        CanRedo = canRedo;
    }

    public int Revision { get; }
    public MapMutationHistoryEntry Mutation { get; }
    public int UndoCount { get; }
    public int RedoCount { get; }
    public bool CanUndo { get; }
    public bool CanRedo { get; }
}

internal sealed class MapMutationBatchUndoResult
{
    public MapMutationBatchUndoResult(int revision, List<MapMutationHistoryEntry> undoneMutations, int undoCount, int redoCount, bool canUndo, bool canRedo)
    {
        Revision = revision;
        UndoneMutations = undoneMutations;
        UndoCount = undoCount;
        RedoCount = redoCount;
        CanUndo = canUndo;
        CanRedo = canRedo;
    }

    public int Revision { get; }

    [Description("Undone entries in the order they were undone (newest first). They can be redone with redo_latest in reverse order.")]
    public List<MapMutationHistoryEntry> UndoneMutations { get; }

    public int UndoCount { get; }
    public int RedoCount { get; }
    public bool CanUndo { get; }
    public bool CanRedo { get; }
}

internal sealed class ScriptingFieldChangeInfo
{
    public ScriptingFieldChangeInfo(string field, string before, string after)
    {
        Field = field;
        Before = before;
        After = after;
    }

    public string Field { get; }
    public string Before { get; }
    public string After { get; }
}

internal sealed class ScriptingElementChangeReviewInfo
{
    public string Kind { get; init; }

    [Description("\"Created\", \"Updated\", or \"Deleted\".")]
    public string ChangeType { get; init; }

    public string Id { get; init; }
    public int? Index { get; init; }
    public string Name { get; init; }
    public string ContentHashBefore { get; init; }
    public string ContentHashAfter { get; init; }

    [Description("Whether the element currently matches the state this entry left it in (after the change while the entry is in the " +
        "undo history, before the change while it is in the redo history). False means it was changed outside the history since.")]
    public bool MatchesExpectedCurrentState { get; init; }

    [Description("Canonical fields that differ between the before and after state. For created elements every field has a null " +
        "\"before\"; for deleted elements every field has a null \"after\". Trigger and script parameters are raw persisted values.")]
    public List<ScriptingFieldChangeInfo> FieldChanges { get; init; }
}

internal sealed class MapActionReviewInfo
{
    public MapMutationHistoryEntry Entry { get; init; }

    [Description("\"Undo\" if the action is currently applied (it is in the undo history), \"Redo\" if it has been undone.")]
    public string HistoryStack { get; init; }

    [Description("Zero-based position from the top of its stack; 0 means it is the next entry to undo or redo.")]
    public int PositionFromTop { get; init; }

    [Description("Number of entries above this one in its stack that were not created through MCP (manual edits by the user). " +
        "undo_through refuses to cross them.")]
    public int NonMcpEntriesAbove { get; init; }

    [Description("For map edits with known affected cells: a suggested cell rectangle (with a small margin) to pass to " +
        "screenshot_map_region, inspect_map_region, or move_camera to look at the result.")]
    public MapCellRectangleInfo SuggestedViewRegion { get; init; }

    [Description("For scripting change sets: each changed element with a field-level before/after diff.")]
    public List<ScriptingElementChangeReviewInfo> ScriptingChanges { get; init; }
}

internal sealed class MapCellRectangleInfo
{
    public MapCellRectangleInfo(int x, int y, int width, int height)
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
