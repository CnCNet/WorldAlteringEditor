using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    public class SetFollowerMutation : Mutation
    {
        public SetFollowerMutation(IMutationTarget mutationTarget, Unit followedUnit, Unit followerUnit) : base(mutationTarget)
        {
            this.followedUnit = followedUnit;
            this.followerUnit = followerUnit;
        }

        private Unit followedUnit;
        private Unit followerUnit;
        private Unit oldFollowerUnit;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString", 
                "Set follower of vehicle {0} at {1} " +
                "to {2} at {3}"),
                    followedUnit.ObjectType.GetEditorDisplayName(), followedUnit.Position, 
                    followerUnit.ObjectType.GetEditorDisplayName(), followerUnit.Position);
        }

        public override void Perform()
        {
            oldFollowerUnit = followedUnit.FollowerUnit;
            followedUnit.FollowerUnit = followerUnit;
        }

        public override void Undo()
        {
            followedUnit.FollowerUnit = oldFollowerUnit;
        }
    }
}
