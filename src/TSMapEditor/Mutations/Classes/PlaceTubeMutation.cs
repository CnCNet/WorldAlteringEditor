using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    public class PlaceTubeMutation : Mutation
    {
        public PlaceTubeMutation(IMutationTarget mutationTarget, Tube tube) : base(mutationTarget)
        {
            this.tube = tube;
        }

        private readonly Tube tube;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString", 
                "Place tunnel tube of length {0} at {1}"),
                    tube.Directions.Count, tube.EntryPoint);
        }

        public override void Perform()
        {
            MutationTarget.Map.Tubes.Add(tube);
            TubeRefreshHelper.MapViewRefreshTube(tube, MutationTarget);
        }

        public override void Undo()
        {
            MutationTarget.Map.Tubes.Remove(tube);
            TubeRefreshHelper.MapViewRefreshTube(tube, MutationTarget);
        }
    }
}
