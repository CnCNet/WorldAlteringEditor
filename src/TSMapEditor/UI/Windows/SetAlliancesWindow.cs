using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class SetAlliancesWindow : INItializableWindow
    {
        public SetAlliancesWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        public event EventHandler AlliesUpdated;

        private readonly Map map;

        private XNAPanel panelCheckBoxes;
        private EditorButton btnApply;

        private List<XNACheckBox> checkBoxes = new List<XNACheckBox>();

        public override void Initialize()
        {
            Name = nameof(SetAlliancesWindow);
            base.Initialize();

            panelCheckBoxes = FindChild<XNAPanel>(nameof(panelCheckBoxes));

            btnApply = FindChild<EditorButton>("btnApply");
            btnApply.LeftClick += BtnApply_LeftClick;
        }

        private void BtnApply_LeftClick(object sender, EventArgs e)
        {
            var alliedHouses = checkBoxes.FindAll(chk => chk.Checked).Select(chk => (House)chk.Tag).ToList();
            foreach (var house in alliedHouses)
            {
                foreach (var otherHouse in alliedHouses)
                {
                    if (otherHouse == house)
                        continue;

                    if (!house.Allies.Contains(otherHouse))
                        house.Allies.Add(otherHouse);                    
                }
            }

            AlliesUpdated?.Invoke(this, EventArgs.Empty);

            Hide();
        }

        public void Open()
        {
            RefreshCheckBoxes();

            Show();
        }

        private void RefreshCheckBoxes()
        {
            checkBoxes.ForEach(chk => panelCheckBoxes.RemoveChild(chk));
            checkBoxes.Clear();

            int y = 0;

            bool useTwoColumns = map.Houses.Count > 8;
            bool isSecondColumn = false;

            foreach (var house in map.Houses)
            {
                var checkBox = new XNACheckBox(WindowManager);
                checkBox.Name = "chk" + house.ININame;
                checkBox.X = isSecondColumn ? 150 : 0;
                checkBox.Y = y;
                checkBox.Text = house.ININame;
                checkBox.Tag = house;
                panelCheckBoxes.AddChild(checkBox);
                checkBoxes.Add(checkBox);

                if (!useTwoColumns || isSecondColumn)
                    y = checkBox.Bottom + Constants.UIVerticalSpacing;

                if (useTwoColumns)
                    isSecondColumn = !isSecondColumn;
            }

            panelCheckBoxes.Height = checkBoxes.Count > 0 ? checkBoxes[checkBoxes.Count - 1].Bottom : 0;
            btnApply.Y = panelCheckBoxes.Bottom + Constants.UIEmptyTopSpace;
            Height = btnApply.Bottom + Constants.UIEmptyBottomSpace;

            CenterOnParent();
        }
    }
}
