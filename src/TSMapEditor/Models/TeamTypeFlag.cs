namespace TSMapEditor.Models
{
    public class TeamTypeFlag
    {
        public TeamTypeFlag(string name, string uiName, bool defaultValue)
        {
            Name = name;
            UIName = uiName;
            DefaultValue = defaultValue;
        }

        public string Name { get; }
        public string UIName { get; }
        public bool DefaultValue { get; }
    }
}
