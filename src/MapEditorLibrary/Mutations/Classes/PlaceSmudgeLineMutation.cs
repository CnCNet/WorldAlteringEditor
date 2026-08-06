using MapEditorLibrary.GameMath;
using MapEditorLibrary.Models;

namespace MapEditorLibrary.Mutations.Classes;

/// <summary>
/// A mutation that places smudges in a line on the map.
/// </summary>
public class PlaceSmudgeLineMutation : Mutation
{
    public PlaceSmudgeLineMutation(IMutationTarget mutationTarget, SmudgeType smudgeType, Point2D sourceCoords, Direction direction, int length) : base(mutationTarget)
    {
        this.smudgeType = smudgeType;
        this.sourceCoords = sourceCoords;
        this.direction = direction;
        this.length = length;
    }

    private readonly SmudgeType smudgeType;
    private readonly Point2D sourceCoords;
    private readonly Direction direction;
    private readonly int length;

    private readonly List<CachedSmudge> oldSmudges = new List<CachedSmudge>(1);

    public override string GetDisplayString()
    {
        string directionString = Translate("Direction." + direction, Helpers.DirectionToName(direction));

        if (smudgeType == null)
        {
            return string.Format(Translate(this, "DisplayStringErase",
                "Erase smudges in a line from {0} towards {1} with length {2}"),
                sourceCoords, directionString, length);
        }

        return string.Format(Translate(this, "DisplayString",
            "Place smudge '{0}' in a line from {1} towards {2} with length {3}"),
            smudgeType.GetEditorDisplayName(), sourceCoords, directionString, length);
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

            if ((smudgeType == null && cell.Smudge != null) ||
                (smudgeType != null && (cell.Smudge == null || cell.Smudge.SmudgeType != smudgeType)))
            {
                oldSmudges.Add(new CachedSmudge(cell.CoordsToPoint(), cell.Smudge == null ? null : cell.Smudge.SmudgeType));
            }
            else
            {
                continue;
            }

            if (smudgeType != null)
                cell.Smudge = new Smudge() { SmudgeType = smudgeType, Position = cell.CoordsToPoint() };
            else
                cell.Smudge = null;
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
