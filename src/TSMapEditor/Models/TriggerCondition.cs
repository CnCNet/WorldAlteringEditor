using Rampastring.Tools;
using System;
using System.IO;
using TSMapEditor.CCEngine;

namespace TSMapEditor.Models
{
    /// <summary>
    /// A trigger condition ("Event").
    /// </summary>
    public class TriggerCondition : ICloneable
    {
        public const int DEF_PARAM_COUNT = 2;
        public const int MAX_PARAM_COUNT = 4;

        public TriggerCondition()
        {
            for (int i = 0; i < Parameters.Length; i++)
            {
                if (i < DEF_PARAM_COUNT)
                    Parameters[i] = "0";
                else
                    Parameters[i] = string.Empty;
            }
        }

        public TriggerCondition(TriggerEventType triggerEventType)
        {
            for (int i = 0; i < Parameters.Length; i++)
            {
                if (i < DEF_PARAM_COUNT)
                {
                    Parameters[i] = "0";                    
                }
                else if (i < DEF_PARAM_COUNT + triggerEventType.AdditionalParams)
                {
                    Parameters[i] = "0";
                }
                else
                {
                    Parameters[i] = string.Empty;
                }
            }
        }

        public int ConditionIndex { get; set; }

        public string[] Parameters { get; private set; } = new string[MAX_PARAM_COUNT];

        public string ParamToString(int index)
        {
            if (string.IsNullOrWhiteSpace(Parameters[index]))
                return "0";

            return Parameters[index];
        }

        public object Clone() => DoClone();

        public TriggerCondition DoClone()
        {
            TriggerCondition clone = (TriggerCondition)MemberwiseClone();
            clone.Parameters = new string[Parameters.Length];
            Array.Copy(Parameters, clone.Parameters, Parameters.Length);

            return clone;
        }

        public static TriggerCondition ParseFromArray(string[] array, int startIndex, int additionalParams)
        {
            if (startIndex + DEF_PARAM_COUNT >= array.Length)
                return null;

            var triggerCondition = new TriggerCondition();
            triggerCondition.ConditionIndex = Conversions.IntFromString(array[startIndex], -1);
            for (int i = 0; i < DEF_PARAM_COUNT; i++)
                triggerCondition.Parameters[i] = array[startIndex + 1 + i];

            if (startIndex + DEF_PARAM_COUNT + additionalParams >= array.Length)
                return null;

            for (int i = 0; i < additionalParams; i++)
            {
                triggerCondition.Parameters[i + DEF_PARAM_COUNT] = array[startIndex + DEF_PARAM_COUNT + 1 + i];
            }

            if (triggerCondition.ConditionIndex < 0)
                return null;

            return triggerCondition;
        }

        public void Serialize(MemoryStream memoryStream)
        {
            StreamHelpers.WriteInt(memoryStream, ConditionIndex);

            foreach (var parameter in Parameters)
            {
                StreamHelpers.WriteUnicodeString(memoryStream, parameter);
            }
        }

        public void Deserialize(MemoryStream memoryStream)
        {   
            ConditionIndex = StreamHelpers.ReadInt(memoryStream);

            for (int i = 0; i < Parameters.Length; i++)
            {
                Parameters[i] = StreamHelpers.ReadUnicodeString(memoryStream);
            }
        }
    }
}
