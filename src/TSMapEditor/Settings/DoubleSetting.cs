using Rampastring.Tools;
using System.Globalization;

namespace TSMapEditor.Settings
{
    public class DoubleSetting : SettingBase<double>
    {
        public DoubleSetting(string section, string key, double defaultValue) : base(section, key, defaultValue)
        {
        }

        protected override double GetValueFromString(string iniValue)
        {
            return Conversions.DoubleFromString(iniValue, DefaultValue);
        }

        protected override string GetValueString(double value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
