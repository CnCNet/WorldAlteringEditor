using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that allows placing smudge collections in a line on the map.
/// </summary>
public class PlaceSmudgeCollectionLineMutation : Mutation
{
    public PlaceSmudgeCollectionLineMutation(IMutationTarget mutationTarget, SmudgeCollection smudgeCollection, Point2D sourceCoords, Direction direction, int length) : base(mutationTarget)
    {
        this.smudgeCollection = smudgeCollection;
        this.sourceCoords = sourceCoords;
        this.direction = direction;
        this.length = length;
    }

    private readonly SmudgeCollection smudgeCollection;
    private readonly Point2D sourceCoords;
    private readonly Direction direction;
    private readonly int length;

    private readonly List<CachedSmudge> oldSmudges = new List<CachedSmudge>(1);

    public override string GetDisplayString()
    {
        string directionString = Translate("Direction." + direction, Helpers.DirectionToName(direction));
        return string.Format(Translate(this, "DisplayString",
            "Place smudge collection '{0}' in a line from {1} towards {2} with length {3}"),
            smudgeCollection.Name, sourceCoords, directionString, length);
    }

    public override void Perform()
    {
        oldSmudges.Clear();

        Point2D step = Helpers.VisualDirectionToPoint(direction);

        for (int i = 0; i <= length; i++)
        {
            Point2D currentCellCoords = sourceCoords + step.ScaleBy(i);
            var cell = MutationTarget.Map.GetTile(currentCellCoords);
            if (cell == null)
                continue;

            oldSmudges.Add(new CachedSmudge(cell.CoordsToPoint(), cell.Smudge == null ? null : cell.Smudge.SmudgeType));

            var collectionEntry = smudgeCollection.Entries[MutationTarget.Randomizer.GetRandomNumber(0, smudgeCollection.Entries.Length - 1)];
            cell.Smudge = new Smudge() { SmudgeType = collectionEntry.SmudgeType, Position = cell.CoordsToPoint() };
        }

        MutationTarget.AddRefreshPoint(sourceCoords, length + 2);
    }

    public override void Undo()
    {
        foreach (var oldSmudge in oldSmudges)
        {
            var cell = Map.GetTile(oldSmudge.CellCoords);

            if (oldSmudge.SmudgeType == null)
                cell.Smudge = null;
            else
                cell.Smudge = new Smudge() { SmudgeType = oldSmudge.SmudgeType, Position = oldSmudge.CellCoords };
        }

        MutationTarget.AddRefreshPoint(sourceCoords, length + 2);
    }
}
