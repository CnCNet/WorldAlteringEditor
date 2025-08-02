using Rampastring.Tools;
using System;
using System.Collections.Generic;
using TSMapEditor.Models.Enums;

namespace TSMapEditor.CCEngine
{
    public class TriggerEventParam
    {
        public TriggerEventParam(TriggerParamType triggerParamType, string nameOverride, List<string> presetOptions = null)
        {
            TriggerParamType = triggerParamType;
            NameOverride = nameOverride;
            PresetOptions = presetOptions;
        }

        public TriggerParamType TriggerParamType { get; }
        public string NameOverride { get; }
        public List<string> PresetOptions { get; }
    }

    public class TriggerEventType
    {
        public const int DEF_PARAM_COUNT = 2;
        public const int MAX_PARAM_COUNT = 4;

        public TriggerEventType(int id)
        {
            ID = id;
        }

        public int ID { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }
        public TriggerEventParam[] Parameters { get; } = new TriggerEventParam[MAX_PARAM_COUNT];
        public bool Available { get; set; } = true;

        public int AdditionalParams
        {
            get
            {
                int additionalParams = 0;

                for (int i = DEF_PARAM_COUNT; i < MAX_PARAM_COUNT; i++)
                {
                    var param = Parameters[i];
                    if (param.TriggerParamType != TriggerParamType.Unused)
                        additionalParams++;
                }

                return additionalParams;
            }
        }

        public void ReadPropertiesFromIniSection(IniSection iniSection)
        {
            ID = iniSection.GetIntValue("IDOverride", ID);
            Name = iniSection.GetStringValue(nameof(Name), string.Empty);
            Description = iniSection.GetStringValue(nameof(Description), string.Empty);
            Available = iniSection.GetBooleanValue(nameof(Available), true);

            for (int i = 0; i < Parameters.Length; i++)
            {
                string key = $"P{i + 1}Type";
                string nameOverrideKey = $"P{i + 1}Name";
                string presetOptionsKey = $"P{i + 1}PresetOptions";

                if (!iniSection.KeyExists(key))
                {
                    Parameters[i] = new TriggerEventParam(TriggerParamType.Unused, null);
                    continue;
                }

                var triggerParamType = (TriggerParamType)Enum.Parse(typeof(TriggerParamType), iniSection.GetStringValue(key, string.Empty));
                string nameOverride = iniSection.GetStringValue(nameOverrideKey, null);
                if (triggerParamType == TriggerParamType.WaypointZZ && string.IsNullOrWhiteSpace(nameOverride))
                    nameOverride = "Waypoint";

                List<string> presetOptions = null;
                string presetOptionsString = iniSection.GetStringValue(presetOptionsKey, null);
                if (!string.IsNullOrWhiteSpace(presetOptionsString))
                {
                    presetOptions = new List<string>(presetOptionsString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
                }

                Parameters[i] = new TriggerEventParam(triggerParamType, nameOverride, presetOptions);
            }
        }
    }
}
