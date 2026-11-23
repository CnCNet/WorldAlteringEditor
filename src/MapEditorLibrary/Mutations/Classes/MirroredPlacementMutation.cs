namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// Keeps counterparts together in both directions through the edit history.
/// </summary>
public class MirroredPlacementMutation : Mutation
{
    public MirroredPlacementMutation(IMutationTarget mutationTarget, IReadOnlyList<IMutation> placements, int eventID)
        : base(mutationTarget)
    {
        this.placements = placements;
        EventID = eventID;
    }

    private readonly IReadOnlyList<IMutation> placements;

    public override string GetDisplayString() => Translate(this, "DisplayString", "Mirrored placement");

    public override void Perform()
    {
        int completed = 0;
        try
        {
            for (; completed < placements.Count; completed++)
                placements[completed].Perform();
        }
        catch
        {
            for (int i = completed - 1; i >= 0; i--)
                placements[i].Undo();

            throw;
        }
    }

    public override void Undo()
    {
        for (int i = placements.Count - 1; i >= 0; i--)
            placements[i].Undo();
    }
}
