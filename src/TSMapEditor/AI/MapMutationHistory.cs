using System.Collections.Generic;
using TSMapEditor.Mutations;

namespace TSMapEditor.AI;

public sealed class MapMutationHistoryEntry
{
    public MapMutationHistoryEntry(MutationHistoryMetadata metadata, int revision, string editorToolName, string description, bool canUndo, bool canRedo)
    {
        HasMetadata = metadata != null;
        MutationId = metadata?.MutationId;
        Revision = revision;
        CreatedRevision = metadata?.CreatedRevision;
        Origin = metadata?.Origin ?? "Editor";
        ToolName = metadata?.ToolName ?? editorToolName;
        Description = description;
        AffectedCells = metadata?.AffectedCells;
        CanUndo = canUndo && HasMetadata;
        CanRedo = canRedo && HasMetadata;
    }

    public bool HasMetadata { get; }
    public long? MutationId { get; }
    public int Revision { get; }
    public int? CreatedRevision { get; }
    public string Origin { get; }
    public string ToolName { get; }
    public string Description { get; }
    public MutationAffectedCells AffectedCells { get; }
    public bool CanUndo { get; }
    public bool CanRedo { get; }
}

public sealed class MapMutationHistoryInfo
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

public sealed class MapMutationOperationResult
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
