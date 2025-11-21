using System;
using System.Globalization;
using System.IO;

namespace TSMapEditor.Models
{
    public class TriggerAction : ICloneable
    {
        public const int PARAM_COUNT = 7;
        public const int INI_VALUE_COUNT = PARAM_COUNT + 1;

        public TriggerAction()
        {
            for (int i = 0; i < Parameters.Length - 1; i++)
                Parameters[i] = "0";

            Parameters[PARAM_COUNT - 1] = "A";
        }

        public int ActionIndex { get; set; }
        public string[] Parameters { get; private set; } = new string[PARAM_COUNT];

        public string ParamToString(int index)
        {
            if (string.IsNullOrWhiteSpace(Parameters[index]))
                return "0";

            return Parameters[index];
        }

        public object Clone() => DoClone();

        public TriggerAction DoClone()
        {
            TriggerAction clone = (TriggerAction)MemberwiseClone();
            clone.Parameters = new string[Parameters.Length];
            Array.Copy(Parameters, clone.Parameters, Parameters.Length);

            return clone;
        }

        public static TriggerAction ParseFromArray(string[] array, int startIndex)
        {
            if (startIndex + INI_VALUE_COUNT > array.Length)
                return null;

            int actionIndex = int.Parse(array[startIndex], CultureInfo.InvariantCulture);

            var triggerAction = new TriggerAction();
            triggerAction.ActionIndex = actionIndex;
            for (int i = 0; i < PARAM_COUNT; i++)
                triggerAction.Parameters[i] = array[startIndex + 1 + i];

            return triggerAction;
        }

        public void Serialize(MemoryStream memoryStream)
        {
            StreamHelpers.WriteInt(memoryStream, ActionIndex);

            foreach (var parameter in Parameters)
            {
                StreamHelpers.WriteUnicodeString(memoryStream, parameter);
            }
        }

        public void Deserialize(MemoryStream memoryStream)
        {   
            ActionIndex = StreamHelpers.ReadInt(memoryStream);

            for (int i = 0; i < Parameters.Length; i++)
            {
                Parameters[i] = StreamHelpers.ReadUnicodeString(memoryStream);
            }
        }
    }
}
