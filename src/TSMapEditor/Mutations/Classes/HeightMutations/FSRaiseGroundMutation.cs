using TSMapEditor.GameMath;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes.HeightMutations
{
    /// <summary>
    /// A mutation for "smartly" raising ground, with automatic application of ramps,
    /// using non-steep ramps only.
    /// </summary>
    public class FSRaiseGroundMutation : RaiseGroundMutationBase
    {
        public FSRaiseGroundMutation(IMutationTarget mutationTarget, Point2D originCell, BrushSize brushSize) : base(mutationTarget, originCell, brushSize)
        {
        }

        protected override bool AllowSteep => false;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString", "Raise ground at {0} with a brush size of {1} using non-steep ramps"),
                OriginCell, BrushSize);
        }

        public override void Perform() => RaiseGround();
    }
}
