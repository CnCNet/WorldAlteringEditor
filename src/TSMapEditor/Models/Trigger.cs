using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using TSMapEditor.CCEngine;
using TSMapEditor.Misc;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.Models
{
    /// <summary>
    /// A map trigger.
    /// </summary>
    public class Trigger : IIDContainer
    {
        public static NamedColor[] SupportedColors => NamedColors.GenericSupportedNamedColors;

        public Trigger(string id) { ID = id; }

        public string GetInternalID() => ID;
        public void SetInternalID(string id) => ID = id;

        public string ID { get; private set; }
        public string HouseType { get; set; }

        /// <summary>
        /// The linked trigger ID loaded from the map.
        /// Do not use post map loading.
        /// </summary>
        public string LinkedTriggerId { get; set; } = Constants.NoneValue2;
        public Trigger LinkedTrigger { get; set; }
        public string Name { get; set; }
        public bool Disabled { get; set; }
        public bool Easy { get; set; } = true;
        public bool Normal { get; set; } = true;
        public bool Hard { get; set; } = true;

        public List<TriggerCondition> Conditions { get; private set; } = new List<TriggerCondition>();
        public List<TriggerAction> Actions { get; private set; } = new List<TriggerAction>();


        private string _editorColor;
        /// <summary>
        /// Editor-only. The color of the trigger in the UI.
        /// If null, the trigger should be displayed with the default UI text color.
        /// </summary>
        public string EditorColor
        {
            get => _editorColor;
            set
            {
                _editorColor = value;

                if (_editorColor != null)
                {
                    int index = Array.FindIndex(SupportedColors, c => c.Name == value);
                    if (index > -1)
                    {
                        XNAColor = SupportedColors[index].Value;
                    }
                    else
                    {
                        // Only allow assigning colors that actually exist in the color table
                        _editorColor = null;
                    }
                }
                    
            }
        }

        public Color XNAColor;

        /// <summary>
        /// Creates and returns a deep clone of this trigger.
        /// </summary>
        /// <param name="uniqueId">The unique ID of the new trigger.</param>
        public Trigger Clone(string uniqueId)
        {
            Trigger clone = (Trigger)MemberwiseClone();
            clone.ID = uniqueId;
            clone.Name = Name + Translate(this, "CloneName", " (Clone)");

            // Deep clone the events and actions

            clone.Conditions = new List<TriggerCondition>(Conditions.Capacity);
            foreach (var condition in Conditions)
            {
                clone.Conditions.Add(condition.DoClone());
            }

            clone.Actions = new List<TriggerAction>(Actions.Capacity);
            foreach (var action in Actions)
            {
                clone.Actions.Add(action.DoClone());
            }

            return clone;
        }

        public void WriteToIniFile(IniFile iniFile, EditorConfig editorConfig)
        {
            // Write entry to [Triggers]
            string linkedTriggerId = LinkedTrigger == null ? Constants.NoneValue1 : LinkedTrigger.ID;
            iniFile.SetStringValue("Triggers", ID,
                $"{HouseType},{linkedTriggerId},{Name}," +
                $"{Helpers.BoolToIntString(Disabled)}," +
                $"{Helpers.BoolToIntString(Easy)},{Helpers.BoolToIntString(Normal)},{Helpers.BoolToIntString(Hard)},0");

            // Write entry to [Events]
            var conditionDataString = new ExtendedStringBuilder(true, ',');
            conditionDataString.Append(Conditions.Count);
            foreach (var condition in Conditions)
            {
                conditionDataString.Append(condition.ConditionIndex);
                for (int i = 0; i < TriggerCondition.DEF_PARAM_COUNT; i++)
                    conditionDataString.Append(condition.ParamToString(i));

                var triggerEventType = editorConfig.TriggerEventTypes[condition.ConditionIndex];
                for (int i = 0; i < triggerEventType.AdditionalParams; i++)
                {
                    conditionDataString.Append(condition.ParamToString(TriggerCondition.DEF_PARAM_COUNT + i));
                }
            }

            iniFile.SetStringValue("Events", ID, conditionDataString.ToString());

            // Write entry to [Actions]
            var actionDataString = new ExtendedStringBuilder(true, ',');
            actionDataString.Append(Actions.Count);
            foreach (var action in Actions)
            {
                actionDataString.Append(action.ActionIndex);
                for (int i = 0; i < TriggerAction.PARAM_COUNT - 1; i++)
                    actionDataString.Append(action.ParamToString(i));

                // Special handling for param #6 because Westwood <3
                int specialIndex = TriggerAction.PARAM_COUNT - 1;

                if (editorConfig.TriggerActionTypes.TryGetValue(action.ActionIndex, out var triggerActionType))
                {
                    if (triggerActionType.Parameters[specialIndex].TriggerParamType != TriggerParamType.Unused)
                    {
                        // If P7Type has been explicitly defined, write it as normal
                        actionDataString.Append(action.ParamToString(specialIndex));
                    }
                    else
                    {
                        // Otherwise we need to write P7 as 'A' as it signifies waypoint 0.
                        actionDataString.Append("A");
                    }
                }
                else
                {
                    // Unknown trigger action type, do not modify P7
                    actionDataString.Append(action.ParamToString(6));
                }
            }

            iniFile.SetStringValue("Actions", ID, actionDataString.ToString());

            // Write entry to [EditorTriggerInfo]
            if (EditorColor != null)
                iniFile.SetStringValue("EditorTriggerInfo", ID, EditorColor);
        }

        public void ParseEditorInfo(IniFile iniFile)
        {
            EditorColor = iniFile.GetStringValue("EditorTriggerInfo", ID, null);
        }

        public void ParseConditions(string data, EditorConfig editorConfig)
        {
            if (string.IsNullOrWhiteSpace(data))
                return;

            string[] dataArray = data.Split(',');

            int eventCount = Conversions.IntFromString(dataArray[0], -1);
            if (eventCount < 0)
                return;

            int startIndex = 1;
            for (int i = 0; i < eventCount; i++)
            {
                int conditionIndex = Conversions.IntFromString(dataArray[startIndex], -1);
                if (!editorConfig.TriggerEventTypes.TryGetValue(conditionIndex, out TriggerEventType triggerEventType))
                {
                    throw new INIConfigException("The map contains a trigger event that is not defined in the editor's config. To prevent data loss, the map cannot be loaded. Event index: " + conditionIndex);
                }

                int additionalParams = triggerEventType.AdditionalParams;

                var triggerEvent = TriggerCondition.ParseFromArray(dataArray, startIndex, additionalParams);

                if (triggerEvent == null)
                    return;

                if (additionalParams > 0)
                    startIndex += TriggerCondition.DEF_PARAM_COUNT + additionalParams + 1;
                else
                    startIndex += TriggerCondition.DEF_PARAM_COUNT + 1;

                Conditions.Add(triggerEvent);
            }
        }

        public void ParseActions(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
                return;

            string[] dataArray = data.Split(',');

            int actionCount = Conversions.IntFromString(dataArray[0], -1);
            if (actionCount < 0)
                return;

            int startIndex = 1;
            for (int i = 0; i < actionCount; i++)
            {
                var triggerAction = TriggerAction.ParseFromArray(dataArray, startIndex);
                if (triggerAction == null)
                    return;

                startIndex += TriggerAction.INI_VALUE_COUNT;
                Actions.Add(triggerAction);
            }
        }

        /// <summary>
        /// Parses and creates a trigger instance from a trigger data line
        /// in a Tiberian Sun / Red Alert 2 map file.
        /// </summary>
        /// <param name="id">The ID of the trigger.</param>
        /// <param name="data">The data line.</param>
        /// <returns>A Trigger instance if the parsing succeeds, otherwise null.</returns>
        public static Trigger ParseTrigger(string id, string data)
        {
            // [Triggers]
            // ID=HOUSE,LINKED_TRIGGER,NAME,DISABLED,EASY,NORMAL,HARD,REPEATING
            // https://modenc.renegadeprojects.com/Triggers
            // the 'REPEATING' field here is unused by the game, so we ignore it

            string[] parts = data.Split(',');
            if (parts.Length < 7)
                return null;

            return new Trigger(id)
            {
                HouseType = parts[0],
                LinkedTriggerId = parts[1],
                Name = parts[2],
                Disabled = Conversions.BooleanFromString(parts[3], false),
                Easy = Conversions.BooleanFromString(parts[4], true),
                Normal = Conversions.BooleanFromString(parts[5], true),
                Hard = Conversions.BooleanFromString(parts[6], true),
            };
        }

        public void Serialize(MemoryStream memoryStream)
        {
            StreamHelpers.WriteUnicodeString(memoryStream, HouseType);
            StreamHelpers.WriteUnicodeString(memoryStream, Name);

            StreamHelpers.WriteBool(memoryStream, Disabled);
            StreamHelpers.WriteBool(memoryStream, Easy);
            StreamHelpers.WriteBool(memoryStream, Normal);
            StreamHelpers.WriteBool(memoryStream, Hard);

            StreamHelpers.WriteUnicodeString(memoryStream, EditorColor);

            StreamHelpers.WriteInt(memoryStream, Conditions.Count);
            foreach (var condition in Conditions)
            {
                condition.Serialize(memoryStream);
            }

            StreamHelpers.WriteInt(memoryStream, Actions.Count);
            foreach (var action in Actions)
            {
                action.Serialize(memoryStream);
            }
        }

        public void Deserialize(MemoryStream memoryStream)
        {
            HouseType = StreamHelpers.ReadUnicodeString(memoryStream);
            Name = StreamHelpers.ReadUnicodeString(memoryStream);                        
            
            Disabled = StreamHelpers.ReadBool(memoryStream);
            Easy = StreamHelpers.ReadBool(memoryStream);
            Normal = StreamHelpers.ReadBool(memoryStream);
            Hard = StreamHelpers.ReadBool(memoryStream);

            EditorColor = StreamHelpers.ReadUnicodeString(memoryStream);            
            
            int conditionCount = StreamHelpers.ReadInt(memoryStream);
            Conditions = new List<TriggerCondition>(conditionCount);
            for (int i = 0; i < conditionCount; i++)
            {
                var condition = new TriggerCondition();
                condition.Deserialize(memoryStream);
                Conditions.Add(condition);
            }

            int actionCount = StreamHelpers.ReadInt(memoryStream);
            Actions = new List<TriggerAction>(actionCount);
            for (int i = 0; i < actionCount; i++)
            {
                var action = new TriggerAction();
                action.Deserialize(memoryStream);
                Actions.Add(action);
            }
        }

        public static void CopyToClipboard(Trigger trigger, Tag tag)
        {
            if (trigger == null || tag == null)
                return;

            byte[] bytes;
            using var memoryStream = new MemoryStream();

            trigger.Serialize(memoryStream);
            tag.Serialize(memoryStream);

            bytes = memoryStream.ToArray();
            Clipboard.SetData(Constants.ClipboardTriggerFormatValue, bytes);
        }

        public static (Trigger, Tag) GetTriggerAndTagFromClipboard(Map map)
        {
            if (!HasTriggerInClipboard())
                return (null, null);

            var bytes = (byte[])Clipboard.GetData(Constants.ClipboardTriggerFormatValue);
            using var memoryStream = new MemoryStream(bytes);

            var trigger = new Trigger(map.GetNewUniqueInternalId());
            trigger.Deserialize(memoryStream);

            var tag = new Tag();
            tag.ID = map.GetNewUniqueInternalId();
            tag.Deserialize(memoryStream);
            tag.Trigger = trigger;

            return (trigger, tag);
        }

        public static bool HasTriggerInClipboard()
        {
            return Clipboard.ContainsData(Constants.ClipboardTriggerFormatValue);
        }
    }
}
