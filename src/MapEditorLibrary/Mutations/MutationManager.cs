namespace MapEditorLibrary.Mutations;

/// <summary>
/// The Undo / Redo system.
/// </summary>
public class MutationManager
{
    public const string MCPMutationOrigin = "MCP";

    public List<IMutation> UndoList { get; } = new List<IMutation>();
    public List<IMutation> RedoList { get; } = new List<IMutation>();

    public int Revision { get; private set; }

    /// <summary>
    /// Raised when a guarded mutation could not be undone or redone because the map
    /// has changed in a way that would make the operation unsafe. The mutation is
    /// removed from the history so that the rest of the history stays usable.
    /// </summary>
    public event EventHandler<MutationDiscardedEventArgs> MutationDiscarded;

    private long nextMutationId = 1;

    /// <summary>
    /// Performs a new mutation on the map.
    /// </summary>
    /// <param name="mutation">The mutation to perform.</param>
    public void PerformMutation(IMutation mutation)
    {
        mutation.Perform();
        RedoList.Clear();
        UndoList.Add(mutation);
        Revision++;
    }

    /// <summary>
    /// Performs a new MCP mutation on the map and records information about its source.
    /// </summary>
    public void PerformMutation(Mutation mutation, string origin, string toolName, MutationAffectedCells affectedCells)
    {
        ArgumentNullException.ThrowIfNull(affectedCells);

        PerformMutation(mutation);
        AssignHistoryMetadata(mutation, origin, toolName, affectedCells);
    }

    /// <summary>
    /// Records a mutation whose effect has already been applied to the map, so that
    /// it can be undone and redone like any other mutation. Use this when the change
    /// has to be committed atomically by other code before it is known to have succeeded.
    /// </summary>
    public void RecordPerformedMutation(Mutation mutation, string origin, string toolName, MutationAffectedCells affectedCells)
    {
        ArgumentNullException.ThrowIfNull(mutation);
        ArgumentNullException.ThrowIfNull(affectedCells);

        RedoList.Clear();
        UndoList.Add(mutation);
        Revision++;
        AssignHistoryMetadata(mutation, origin, toolName, affectedCells);
    }

    private void AssignHistoryMetadata(Mutation mutation, string origin, string toolName, MutationAffectedCells affectedCells)
    {
        mutation.SetHistoryMetadata(new MutationHistoryMetadata(nextMutationId, Revision,
            string.IsNullOrWhiteSpace(origin) ? MCPMutationOrigin : origin,
            string.IsNullOrWhiteSpace(toolName) ? mutation.GetType().Name : toolName,
            affectedCells));

        nextMutationId++;
    }

    public bool CanUndo() => UndoList.Count > 0;

    /// <summary>
    /// Undoes the last mutation performed to the map, or the last chain of mutations
    /// if multiple mutations have the same <see cref="Mutation.EventID"/>.
    /// </summary>
    public void Undo()
    {
        if (!CanUndo())
            return;

        int lastMutationEventId = UndoList[UndoList.Count - 1].EventID;

        if (lastMutationEventId < 0)
        {
            UndoOne();
            return;
        }

        while (CanUndo() && UndoList[UndoList.Count - 1].EventID == lastMutationEventId)
        {
            UndoOne();
        }
    }

    /// <summary>
    /// Returns the latest mutation performed to the map.
    /// </summary>
    public IMutation GetLatestMutation() => UndoList.Count > 0 ? UndoList[^1] : null;

    /// <summary>
    /// Undoes the last mutation performed to the map.
    /// </summary>
    public void UndoOne()
    {
        if (!CanUndo())
            return;

        int lastUndoIndex = UndoList.Count - 1;
        IMutation mutation = UndoList[lastUndoIndex];

        if (mutation is IGuardedMutation guardedMutation)
        {
            string blockReason = guardedMutation.GetUndoBlockReason();
            if (blockReason != null)
            {
                UndoList.RemoveAt(lastUndoIndex);
                Revision++;
                MutationDiscarded?.Invoke(this, new MutationDiscardedEventArgs(mutation, blockReason, isUndo: true));
                return;
            }
        }

        mutation.Undo();
        RedoList.Add(mutation);
        UndoList.RemoveAt(lastUndoIndex);
        Revision++;
    }

    public bool CanRedo() => RedoList.Count > 0;

    /// <summary>
    /// Redoes the last un-done mutation on the map.
    /// </summary>
    public void Redo()
    {
        if (!CanRedo())
            return;

        int lastRedoIndex = RedoList.Count - 1;
        IMutation mutation = RedoList[lastRedoIndex];

        if (mutation is IGuardedMutation guardedMutation)
        {
            string blockReason = guardedMutation.GetRedoBlockReason();
            if (blockReason != null)
            {
                RedoList.RemoveAt(lastRedoIndex);
                Revision++;
                MutationDiscarded?.Invoke(this, new MutationDiscardedEventArgs(mutation, blockReason, isUndo: false));
                return;
            }
        }

        mutation.Perform();
        UndoList.Add(mutation);
        RedoList.RemoveAt(lastRedoIndex);
        Revision++;
    }

    public void ClearUndoAndRedoLists()
    {
        UndoList.Clear();
        RedoList.Clear();
    }
}
