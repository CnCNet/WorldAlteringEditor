namespace TSMapEditor.Settings
{
    public class StringSetting : SettingBase<string>
    {
        public StringSetting(string section, string key, string defaultValue) : base(section, key, defaultValue)
        {
        }

        protected override string GetValueFromString(string iniValue) => iniValue;

        protected override string GetValueString(string value) => value;
    }
}
