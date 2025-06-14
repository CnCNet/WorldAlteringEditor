using Rampastring.Tools;
using System;

namespace TSMapEditor.Models
{
    /// <summary>
    /// A trigger condition ("Event").
    /// </summary>
    public class TriggerCondition : ICloneable
    {
        public const int DEF_PARAM_COUNT = 2;
        public const int MAX_PARAM_COUNT = 3;

        public TriggerCondition()
        {
            for (int i = 0; i < Parameters.Length - 1; i++)
                Parameters[i] = "0";

            Parameters[MAX_PARAM_COUNT - 1] = string.Empty;
        }

        public int ConditionIndex { get; set; }

        public string[] Parameters { get; private set; } = new string[MAX_PARAM_COUNT];

        public string ParamToString(int index)
        {
            if (string.IsNullOrWhiteSpace(Parameters[index]))
            {
                if (index == MAX_PARAM_COUNT - 1)
                    return string.Empty;

                return "0";
            }

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

        public static TriggerCondition ParseFromArray(string[] array, int startIndex, bool useP3)
        {
            if (startIndex + DEF_PARAM_COUNT >= array.Length)
                return null;

            var triggerCondition = new TriggerCondition();
            triggerCondition.ConditionIndex = Conversions.IntFromString(array[startIndex], -1);
            for (int i = 0; i < DEF_PARAM_COUNT; i++)
                triggerCondition.Parameters[i] = array[startIndex + 1 + i];

            if (useP3)
            {
                if (startIndex + MAX_PARAM_COUNT >= array.Length)
                    return null;

                triggerCondition.Parameters[MAX_PARAM_COUNT - 1] = array[startIndex + MAX_PARAM_COUNT];
            }

            if (triggerCondition.ConditionIndex < 0)
                return null;

            return triggerCondition;
        }
    }
}
