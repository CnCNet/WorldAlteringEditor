using Rampastring.Tools;

namespace TSMapEditor.Settings
{
    public class BoolSetting : SettingBase<bool>
    {
        public BoolSetting(string section, string key, bool defaultValue) : base(section, key, defaultValue)
        {
        }

        protected override bool GetValueFromString(string iniValue)
        {
            return Conversions.BooleanFromString(iniValue, DefaultValue);
        }

        protected override string GetValueString(bool value)
        {
            return Conversions.BooleanToString(value, BooleanStringStyle.TRUEFALSE_LOWERCASE);
        }
    }
}
