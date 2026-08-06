using MapEditorLibrary.Configuration;
using MapEditorLibrary.GameMath;

namespace MapEditorLibrary.Mutations.Classes.HeightMutations;

/// <summary>
/// A mutation for "smartly" lowering ground, with automatic application of ramps,
/// allowing steep ramps.
/// </summary>
public class LowerGroundMutation : LowerGroundMutationBase
{
    public LowerGroundMutation(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget, originCell, brushSize)
    {
    }

    protected override bool AllowSteep => true;

    public override string GetDisplayString()
    {
        return string.Format(Translate(this, "DisplayString", "Lower ground at {0} with a brush size of {1} using steep ramps"),
            OriginCell, BrushSize);
    }

    public override void Perform() => LowerGround();
}
