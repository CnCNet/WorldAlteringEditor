using Rampastring.Tools;

namespace TSMapEditor.Settings
{
    public interface IINILoadable
    {
        void LoadValue(IniFile iniFile);
        void WriteValue(IniFile iniFile, bool force);
    }

    public interface ISetting<T> : IINILoadable
    {
        T GetValue();
    }

    /// <summary>
    /// Abstract base class for INI settings of all types.
    /// </summary>
    /// <typeparam name="T">The type of the setting.</typeparam>
    public abstract class SettingBase<T> : ISetting<T>
    {
        public SettingBase(string section, string key, T defaultValue)
        {
            Section = section;
            Key = key;
            DefaultValue = defaultValue;
        }

        public string Section { get; }
        public string Key { get; }
        public T DefaultValue { get; }
        
        public bool HasUserDefinedValue { get; private set; }
        private T _userDefinedValue;
        public T UserDefinedValue 
        {
            get => _userDefinedValue;
            set
            {
                _userDefinedValue = value;
                HasUserDefinedValue = true;
            }
        }

        public T GetValue()
        {
            if (HasUserDefinedValue)
                return UserDefinedValue;

            return DefaultValue;
        }

        public static implicit operator T(SettingBase<T> setting) => setting.GetValue();

        public void LoadValue(IniFile iniFile)
        {
            if (!iniFile.KeyExists(Section, Key))
                return;

            UserDefinedValue = GetValueFromString(iniFile.GetStringValue(Section, Key, string.Empty));
            HasUserDefinedValue = true;
        }

        protected abstract T GetValueFromString(string iniValue);

        public void WriteValue(IniFile iniFile, bool force)
        {
            if (!force && !HasUserDefinedValue)
                return;

            iniFile.SetStringValue(Section, Key, GetValueString(HasUserDefinedValue ? UserDefinedValue : DefaultValue));
        }

        protected abstract string GetValueString(T value);
    }
}
