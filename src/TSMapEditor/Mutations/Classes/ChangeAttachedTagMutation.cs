using TSMapEditor.Models;
using TSMapEditor.UI;

namespace TSMapEditor.Mutations.Classes
{
    /// <summary>
    /// A mutation that allows changing a Techno's 
    /// (building/vehicle/infantry/aircraft) attached trigger tag.
    /// </summary>
    public class ChangeAttachedTagMutation : Mutation
    {
        public ChangeAttachedTagMutation(IMutationTarget mutationTarget, TechnoBase techno, Tag tag) : base(mutationTarget)
        {
            this.techno = techno;
            this.tag = tag;
        }

        private readonly TechnoBase techno;
        private readonly Tag tag;

        private Tag oldAttachedTag;

        public override string GetDisplayString()
        {
            return string.Format(Translate(this, "DisplayString", 
                "Change attached tag of '{0}' at {1} to '{2}'"),
                    techno.GetObjectType().GetEditorDisplayName(), techno.Position, tag.Name);
        }

        public override void Perform()
        {
            oldAttachedTag = techno.AttachedTag;
            techno.AttachedTag = tag;
        }

        public override void Undo()
        {
            techno.AttachedTag = oldAttachedTag;
        }
    }
}
