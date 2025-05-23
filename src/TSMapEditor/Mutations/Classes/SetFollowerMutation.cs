﻿using TSMapEditor.Models;
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
            return $"Set follower of vehicle {followedUnit.ObjectType.GetEditorDisplayName()} at {followedUnit.Position} " +
                $"to {followerUnit.ObjectType.GetEditorDisplayName()} at {followerUnit.Position}";
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
