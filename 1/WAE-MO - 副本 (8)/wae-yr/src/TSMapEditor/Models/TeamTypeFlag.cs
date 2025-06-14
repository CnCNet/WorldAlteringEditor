namespace TSMapEditor.Models
{
    public class TeamTypeFlag
    {
        public TeamTypeFlag(string name, bool defaultValue)
        {
            Name = name;
            DefaultValue = defaultValue;
        }

        public string Name { get; }
        public bool DefaultValue { get; }
    }
}
