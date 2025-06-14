using Rampastring.Tools;
using System.Globalization;

namespace TSMapEditor.Settings
{
    public class IntSetting : SettingBase<int>
    {
        public IntSetting(string section, string key, int defaultValue) : base(section, key, defaultValue)
        {
        }

        protected override int GetValueFromString(string iniValue)
        {
            return Conversions.IntFromString(iniValue, DefaultValue);
        }

        protected override string GetValueString(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
