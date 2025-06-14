using Rampastring.XNAUI;
using TSMapEditor.Models;
using TSMapEditor.Rendering;
using TSMapEditor.UI.CursorActions;

namespace TSMapEditor.UI.Sidebar
{
    /// <summary>
    /// A sidebar panel for listing infantry.
    /// </summary>
    public class InfantryListPanel : ObjectListPanel
    {
        public InfantryListPanel(WindowManager windowManager, EditorState editorState,
            Map map, TheaterGraphics theaterGraphics, ICursorActionTarget cursorActionTarget) 
            : base(windowManager, editorState, map, theaterGraphics)
        {
            infantryPlacementAction = new InfantryPlacementAction(cursorActionTarget);
            infantryPlacementAction.ActionExited += InfantryPlacementAction_ActionExited;
        }

        private void InfantryPlacementAction_ActionExited(object sender, System.EventArgs e)
        {
            ObjectTreeView.SelectedNode = null;
        }

        private readonly InfantryPlacementAction infantryPlacementAction;

        protected override void InitObjects()
        {
            InitObjectsBase(Map.Rules.InfantryTypes, TheaterGraphics.InfantryTextures);
        }

        protected override void ObjectSelected()
        {
            if (ObjectTreeView.SelectedNode == null)
                return;

            infantryPlacementAction.InfantryType = (InfantryType)ObjectTreeView.SelectedNode.Tag;
            EditorState.CursorAction = infantryPlacementAction;
        }
    }
}
