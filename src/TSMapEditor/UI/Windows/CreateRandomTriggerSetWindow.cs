using Rampastring.Tools;
using Rampastring.XNAUI;
using Rampastring.XNAUI.XNAControls;
using System;
using System.Collections.Generic;
using TSMapEditor.Models;
using TSMapEditor.UI.Controls;

namespace TSMapEditor.UI.Windows
{
    public class RandomTriggerSetTriggersCreatedEventArgs : EventArgs
    {
        public RandomTriggerSetTriggersCreatedEventArgs(Trigger baseTrigger)
        {
            BaseTrigger = baseTrigger;
        }

        public Trigger BaseTrigger { get; }
    }

    public class CreateRandomTriggerSetWindow : INItializableWindow
    {
        public CreateRandomTriggerSetWindow(WindowManager windowManager, Map map) : base(windowManager)
        {
            this.map = map;
        }

        private readonly Map map;

        private EditorTextBox tbName;
        private XNADropDown ddColor;
        private EditorNumberTextBox tbNumTriggers;
        private EditorNumberTextBox tbElapsedTime;
        private EditorNumberTextBox tbDelay;
        private XNADropDown ddTriggerType;
        private XNACheckBox cbEveryDiff;
        private EditorButton btnApply;

        public event EventHandler<RandomTriggerSetTriggersCreatedEventArgs> RandomTriggerSetTriggersCreated;

        public override void Initialize()
        {
            Name = nameof(CreateRandomTriggerSetWindow);
            base.Initialize();

            tbName = FindChild<EditorTextBox>(nameof(tbName));
            ddColor = FindChild<XNADropDown>(nameof(ddColor));
            tbNumTriggers = FindChild<EditorNumberTextBox>(nameof(tbNumTriggers));
            tbElapsedTime = FindChild<EditorNumberTextBox>(nameof(tbElapsedTime));
            tbDelay = FindChild<EditorNumberTextBox>(nameof(tbDelay));
            ddTriggerType = FindChild<XNADropDown>(nameof(ddTriggerType));
            cbEveryDiff = FindChild<XNACheckBox>(nameof(cbEveryDiff));
            btnApply = FindChild<EditorButton>(nameof(btnApply));

            ddColor.AddItem(Translate(this, "None", "None"));
            Array.ForEach(Trigger.SupportedColors, sc =>
            {
                ddColor.AddItem(sc.Name, sc.Value);
            });            

            btnApply.LeftClick += BtnApply_LeftClick;
        }

        public void BtnApply_LeftClick(object sender, EventArgs e)
        {
            if (!Validate())
            {
                return;
            }

            string name = tbName.Text;
            string color = ddColor.SelectedItem.Text == Translate(this, "None", "None") ? string.Empty : ddColor.SelectedItem.Text;
            int elapsedTime = tbElapsedTime.Value;
            int count = tbNumTriggers.Value;
            int delay = tbDelay.Value;
            int type = ddTriggerType.SelectedIndex;
            bool createForEveryDifficulty = cbEveryDiff.Checked;

            List<Trigger> baseTriggers = [];

            if (createForEveryDifficulty)
            {
                baseTriggers.Add(CreateRandomTriggersSet(name, elapsedTime, count, delay, color, type, Difficulty.Hard));
                baseTriggers.Add(CreateRandomTriggersSet(name, elapsedTime, count, delay, color, type, Difficulty.Medium));
                baseTriggers.Add(CreateRandomTriggersSet(name, elapsedTime, count, delay, color, type, Difficulty.Easy));
            } 
            else
            {
                baseTriggers.Add(CreateRandomTriggersSet(name, elapsedTime, count, delay, color, type));
            }

            Hide();
            RandomTriggerSetTriggersCreated?.Invoke(this, new RandomTriggerSetTriggersCreatedEventArgs(baseTriggers[0]));
        }

        private Trigger CreateRandomTriggersSet(string name, int elapsedTime, int count, int delay, string color, int type, Difficulty? difficulty = null)
        {
            if (difficulty != null)
            {
                name = $"{difficulty.ToString()[0]} {name}";
            }

            var baseTrigger = CreateBaseTrigger(name, elapsedTime, color, type, difficulty);
            var childTriggers = CreateChildTriggers(name, count, delay, color);
            AssociateTriggers(baseTrigger, childTriggers);

            return baseTrigger;
        }

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(tbName.Text))
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "ValidateMissingTriggerName.Title", "Missing Trigger Name"),
                    Translate(this, "ValidateMissingTriggerName.Description", "Please enter a name for the triggers"),
                    MessageBoxButtons.OK);
                return false;
            }

            if (tbNumTriggers.Value < 2)
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "InvalidTriggersNumber.Title", "Invalid Number of Triggers"),
                    Translate(this, "InvalidTriggersNumber.Description", "Please enter a value of 2 or more"),
                    MessageBoxButtons.OK);
                return false;
            }

            if (tbElapsedTime.Value < 0)
            {
                EditorMessageBox.Show(WindowManager, 
                    Translate(this, "InvalidElapsedTime.Title", "Invalid Elapsed Time"),
                    Translate(this, "InvalidElapsedTime.Description", "Please enter a value of 0 or more"),
                    MessageBoxButtons.OK);
                return false;
            }

            if (tbDelay.Value < 10)
            {
                EditorMessageBox.Show(WindowManager,
                    Translate(this, "InvalidRandomDelay.Title", "Invalid Random Delay"),
                    Translate(this, "InvalidRandomDelay.Description", "Please enter a value of 10 or more"),
                    MessageBoxButtons.OK);
                return false;
            }

            return true;
        }

        private Trigger CreateBaseTrigger(string name, int elapsedTime, string color, int type, Difficulty? difficulty) 
        {
            string triggerName = $"{name} base";

            var baseTrigger = new Trigger(map.GetNewUniqueInternalId());
            baseTrigger.Name = triggerName;
            baseTrigger.HouseType = "Neutral";

            if (!string.IsNullOrWhiteSpace(color))
            {
                baseTrigger.EditorColor = color;
            }

            if (difficulty != null)
            {                
                baseTrigger.Hard = difficulty == Difficulty.Hard;
                baseTrigger.Normal = difficulty == Difficulty.Medium;
                baseTrigger.Easy = difficulty == Difficulty.Easy;

                int diffGlobalVariableIndex = map.Rules.GlobalVariables.FindIndex(gv => gv.Name == $"Difficulty {difficulty}");

                if (diffGlobalVariableIndex < 0)
                {
                    Logger.Log($"{nameof(CreateRandomTriggerSetWindow)}.{nameof(CreateBaseTrigger)}: {difficulty} difficulty global variable not found!");                    
                } 
                else
                {
                    var globalSetCondition = new TriggerCondition();
                    globalSetCondition.ConditionIndex = 27; // Global Is Set
                    globalSetCondition.Parameters[1] = diffGlobalVariableIndex.ToString();

                    baseTrigger.Conditions.Add(globalSetCondition);
                }
            }            

            var elapsedTimeCondition = new TriggerCondition();
            elapsedTimeCondition.ConditionIndex = 13; // Elapsed Time
            elapsedTimeCondition.Parameters[1] = elapsedTime.ToString();

            baseTrigger.Conditions.Add(elapsedTimeCondition);

            map.Triggers.Add(baseTrigger);
            map.Tags.Add(new Tag() { ID = map.GetNewUniqueInternalId(), Name = baseTrigger.Name + " (tag)", Trigger = baseTrigger, Repeating = type });

            return baseTrigger;
        }

        private List<Trigger> CreateChildTriggers(string name, int count, int delay, string color)
        {
            List<Trigger> triggers = [];

            for (int i = 0; i < count; i++)
            {
                var childTrigger = new Trigger(map.GetNewUniqueInternalId());
                childTrigger.Name = $"{name} {i + 1}";
                childTrigger.HouseType = "Neutral";
                childTrigger.Disabled = true;
                
                if (!string.IsNullOrWhiteSpace(color))
                {
                    childTrigger.EditorColor = color;
                }

                var randomDelayCondition = new TriggerCondition();
                randomDelayCondition.ConditionIndex = 51; // Random Delay
                randomDelayCondition.Parameters[1] = delay.ToString();

                childTrigger.Conditions.Add(randomDelayCondition);

                map.Triggers.Add(childTrigger);
                map.Tags.Add(new Tag() { ID = map.GetNewUniqueInternalId(), Name = childTrigger.Name + " (tag)", Trigger = childTrigger, Repeating = 2 });

                triggers.Add(childTrigger);
            }

            return triggers;
        }

        private void AssociateTriggers(Trigger baseTrigger, List<Trigger> childTriggers)
        {            
            foreach (var childTrigger in childTriggers)
            {
                // base trigger needs to enable each of the child triggers
                var enableTriggerAction = new TriggerAction();
                enableTriggerAction.ActionIndex = 53; // Enable Trigger
                enableTriggerAction.Parameters[0] = "2";
                enableTriggerAction.Parameters[1] = childTrigger.ID;

                baseTrigger.Actions.Add(enableTriggerAction);

                // each child trigger needs to disable itself and each other child trigger
                foreach (var siblingTrigger in childTriggers)
                {
                    var disableTriggerAction = new TriggerAction();
                    disableTriggerAction.ActionIndex = 54; // Disable Trigger
                    disableTriggerAction.Parameters[0] = "2";
                    disableTriggerAction.Parameters[1] = siblingTrigger.ID;

                    childTrigger.Actions.Add(disableTriggerAction);
                }
            }
        }

        public void Open() 
        {
            Show();
            ResetValues();
        }

        public void ResetValues()
        {
            tbName.Text = string.Empty;
            ddColor.SelectedIndex = 0;
            tbNumTriggers.Value = 3;
            tbElapsedTime.Value = 100;
            tbDelay.Value = 10;
            ddTriggerType.SelectedIndex = 0;
            cbEveryDiff.Checked = false;
        }
    }
}
