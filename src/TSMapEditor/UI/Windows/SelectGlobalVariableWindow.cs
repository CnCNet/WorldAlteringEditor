using System;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using TSMapEditor.Models;

namespace TSMapEditor.UI.Windows
{
    public class SelectGlobalVariableWindow : SelectObjectWindow<GlobalVariable>
    {
        public SelectGlobalVariableWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        public override void Initialize()
        {
            Name = nameof(SelectGlobalVariableWindow);
            base.Initialize();
        }

        protected override void LbObjectList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbObjectList.SelectedItem == null)
            {
                SelectedObject = null;
                return;
            }

            SelectedObject = (GlobalVariable)lbObjectList.SelectedItem.Tag;
        }

        protected override void ListObjects()
        {
            lbObjectList.Clear();

            foreach (GlobalVariable globalVariable in map.Rules.GlobalVariables)
            {
                lbObjectList.AddItem(new XNAListBoxItem() { Text = $"{globalVariable.Index} {globalVariable.Name}", Tag = globalVariable });
                if (globalVariable == SelectedObject)
                    lbObjectList.SelectedIndex = lbObjectList.Items.Count - 1;
            }
        }
    }
}
