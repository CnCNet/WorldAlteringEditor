using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

public class DeleteTubeMutation : Mutation
{
    public DeleteTubeMutation(IMutationTarget mutationTarget, Point2D cellCoords) : base(mutationTarget)
    {
        this.cellCoords = cellCoords;
    }

    private readonly Point2D cellCoords;

    private Tube deletedTube;
    private int deletedTubeIndex;

    public override string GetDisplayString()
    {
        return string.Format(Translate(this, "DisplayString",
            "Delete tunnel tube of length {0} at {1}"),
                deletedTube.Directions.Count, cellCoords);
    }

    public override void Perform()
    {
        int index = MutationTarget.Map.Tubes.FindIndex(t => t.EntryPoint == cellCoords || t.ExitPoint == cellCoords);

        if (index > -1)
        {
            deletedTubeIndex = index;
            deletedTube = MutationTarget.Map.Tubes[index];
            Map.Tubes.RemoveAt(index);
            MutationTarget.InvalidateMap();
        }
        else
        {
            throw new InvalidOperationException($"{nameof(DeleteTubeMutation)}.{nameof(Perform)}: No tunnel found at tile {cellCoords}");
        }
    }

    public override void Undo()
    {
        MutationTarget.Map.Tubes.Insert(deletedTubeIndex, deletedTube);
        MutationTarget.InvalidateMap();
    }
}
