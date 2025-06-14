using Rampastring.Tools;

namespace TSMapEditor.Models
{
    /// <summary>
    /// A trigger tag. Tags are responsible for activating map triggers.
    /// </summary>
    public class Tag : IIDContainer
    {
        public const int REPEAT_TYPE_MAX = 2;

        public string GetInternalID() => ID;
        public void SetInternalID(string id) => ID = id;

        public string ID { get; set; }
        public int Repeating { get; set; }
        public string Name { get; set; }
        public Trigger Trigger { get; set; }

        public void WriteToIniSection(IniSection iniSection)
        {
            iniSection.SetStringValue(ID, $"{Repeating},{Name},{Trigger.ID}");
        }

        public string GetDisplayString() => Name + " (" + ID + ")";
    }
}
