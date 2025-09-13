using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that changes the owner of an object.
    /// </summary>
    public class ChangeTechnoOwnerMutation : Mutation
    {
        public ChangeTechnoOwnerMutation(TechnoBase techno, House newOwner, IMutationTarget mutationTarget) : base(mutationTarget)
        {
            this.techno = techno;
            this.oldOwner = techno.Owner;
            this.newOwner = newOwner;
        }

        private readonly TechnoBase techno;
        private readonly House oldOwner;
        private readonly House newOwner;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString",
                "Change owner of {0} at {1} to {2}"),
                    techno.GetObjectType().GetEditorDisplayName(), techno.Position, newOwner.ININame);
        }

        public override void Perform()
        {
            techno.Owner = newOwner;
            MutationTarget.AddRefreshPoint(techno.Position);
        }

        public override void Undo()
        {
            techno.Owner = oldOwner;
            MutationTarget.AddRefreshPoint(techno.Position);
        }
    }
}
