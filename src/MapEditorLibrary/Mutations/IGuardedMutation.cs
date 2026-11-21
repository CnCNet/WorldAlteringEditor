namespace MapEditorLibrary.Mutations;

/// <summary>
/// A mutation that can detect when undoing or redoing it is no longer safe,
/// for example because a change made outside the undo system has since
/// modified the data that the mutation would restore.
/// </summary>
public interface IGuardedMutation : IMutation
{
    /// <summary>
    /// Returns a human-readable reason why the mutation cannot currently be undone,
    /// or null if undoing it is safe.
    /// </summary>
    string GetUndoBlockReason();

    /// <summary>
    /// Returns a human-readable reason why the mutation cannot currently be redone,
    /// or null if redoing it is safe.
    /// </summary>
    string GetRedoBlockReason();
}

public class MutationDiscardedEventArgs : EventArgs
{
    public MutationDiscardedEventArgs(IMutation mutation, string reason, bool isUndo)
    {
        Mutation = mutation;
        Reason = reason;
        IsUndo = isUndo;
    }

    public IMutation Mutation { get; }
    public string Reason { get; }

    /// <summary>
    /// True if the mutation was discarded while undoing it, false if while redoing it.
    /// </summary>
    public bool IsUndo { get; }
}
