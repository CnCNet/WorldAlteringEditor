using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Rampastring.Tools;
using Rampastring.XNAUI.XNAControls;

namespace TSMapEditor.I18N
{
    /// <summary>
    /// Handles translation and localization for the map editor.
    /// </summary>
    public class Translation
    {
        private static Translation _instance;
        public static Translation Instance => _instance ??= new Translation();

        private readonly Dictionary<string, string> _values = new();
        private readonly HashSet<string> _missingKeys = new();
        private readonly Dictionary<string, string> _defaultValues = new();

        /// <summary>
        /// Used for INI values.
        /// </summary>
        public const string INI_PREFIX = "INI";

        /// <summary>
        /// Used for INI-defined control values.
        /// </summary>
        public const string CONTROLS_PREFIX = "Controls";

        /// <summary>
        /// Used for parent-agnostic INI values.
        /// </summary>
        public const string GLOBAL_PREFIX = "Global";

        public string Name { get; private set; } = "English";
        public string Author { get; private set; } = "Unknown";
        public string LocaleCode { get; private set; } = "en";
        public CultureInfo UICulture { get; private set; } = CultureInfo.InvariantCulture;

        /// <summary>
        /// Gets all translation values.
        /// </summary>
        public IReadOnlyDictionary<string, string> Values => _values;

        /// <summary>
        /// Gets all missing translation keys.
        /// </summary>
        public IReadOnlySet<string> MissingKeys => _missingKeys;

        /// <summary>
        /// Gets all default values for missing keys.
        /// </summary>
        public IReadOnlyDictionary<string, string> DefaultValues => _defaultValues;

        /// <summary>
        /// Loads translation from an INI file.
        /// </summary>
        /// <param name="filePath">Path to the translation INI file.</param>
        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            var iniFile = new IniFile(filePath);
            
            // Load metadata
            Name = iniFile.GetStringValue("General", "Name", "English");
            Author = iniFile.GetStringValue("General", "Author", "Unknown");
            LocaleCode = iniFile.GetStringValue("General", "LocaleCode", "en");
            
            try
            {
                UICulture = new CultureInfo(LocaleCode);
            }
            catch
            {
                UICulture = CultureInfo.InvariantCulture;
            }

            // Load translation values
            var valuesSection = iniFile.GetSection("Values");
            if (valuesSection != null)
            {
                List<string> keys = iniFile.GetSectionKeys("Values");
                foreach (var key in keys)
                {
                    var value = valuesSection.GetStringValue(key, string.Empty);
                    if (!string.IsNullOrEmpty(value))
                    {
                        // Handle format: "TranslatedText;OriginalText"
                        var parts = value.Split(';');
                        _values[key] = parts[0];
                    }
                }
            }
        }

        /// <summary>
        /// Looks up a translation value by key.
        /// </summary>
        /// <param name="key">The translation key.</param>
        /// <param name="defaultValue">Default value if key is not found.</param>
        /// <param name="notify">Whether to notify about missing keys.</param>
        /// <returns>The translated text or default value.</returns>
        public string LookUp(string key, string defaultValue = null, bool notify = true)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue ?? string.Empty;

            if (_values.TryGetValue(key, out string value))
                return value;

            if (notify)
            {
                HandleMissing(key, defaultValue);
            }

            return defaultValue ?? key;
        }

        /// <summary>
        /// Looks up the translated value that corresponds to the given INI-defined control attribute.
        /// </summary>
        /// <param name="control">The control to look up the attribute value for.</param>
        /// <param name="attributeName">The attribute name as written in the INI.</param>
        /// <param name="defaultValue">The value to fall back to in case there's no translated value.</param>
        /// <param name="notify">Whether to add this key and value to the list of missing key-values.</param>
        /// <returns>The translated value or a default value.</returns>
        public string LookUp(XNAControl control, string attributeName, string defaultValue, bool notify = true)
        {
            string key = $"{INI_PREFIX}:{CONTROLS_PREFIX}:{control.Parent?.Name ?? GLOBAL_PREFIX}:{control.Name}:{attributeName}";
            string globalKey = $"{INI_PREFIX}:{CONTROLS_PREFIX}:{GLOBAL_PREFIX}:{control.Name}:{attributeName}";

            // Debug output to see what keys are being generated
            System.Diagnostics.Debug.WriteLine($"Translation LookUp: key='{key}', defaultValue='{defaultValue}'");
            System.Diagnostics.Debug.WriteLine($"Translation LookUp: control.Parent?.Name='{control.Parent?.Name}', control.Name='{control.Name}'");

            string result;
            if (_values.TryGetValue(key, out string value))
            {
                result = value;
                System.Diagnostics.Debug.WriteLine($"Translation LookUp: Found translation for key '{key}': '{result}'");
            }
            else if (key != globalKey && _values.TryGetValue(globalKey, out string globalValue))
            {
                result = globalValue;
                System.Diagnostics.Debug.WriteLine($"Translation LookUp: Found global translation for key '{globalKey}': '{result}'");
            }
            else
            {
                result = defaultValue;
                System.Diagnostics.Debug.WriteLine($"Translation LookUp: No translation found for key '{key}', using default: '{result}'");

                if (notify)
                {
                    HandleMissing(key, defaultValue);
                }
            }

            return result;
        }

        /// <summary>
        /// Handles missing translation keys.
        /// </summary>
        /// <param name="key">The missing key.</param>
        /// <param name="defaultValue">The default value.</param>
        private void HandleMissing(string key, string defaultValue)
        {
            if (_missingKeys.Add(key) && !string.IsNullOrEmpty(defaultValue))
            {
                _defaultValues[key] = defaultValue;
            }
        }

        /// <summary>
        /// Exports missing translations to an INI file.
        /// </summary>
        /// <param name="filePath">Path to export the INI file.</param>
        public void ExportMissingTranslations(string filePath)
        {
            var iniFile = new IniFile();
            
            iniFile.SetStringValue("General", "Name", "New Translation");
            iniFile.SetStringValue("General", "Author", "Unknown");
            iniFile.SetStringValue("General", "LocaleCode", "en");

            foreach (var key in _missingKeys)
            {
                var defaultValue = _defaultValues.TryGetValue(key, out string value) ? value : key;
                iniFile.SetStringValue("Values", key, $"{defaultValue};{defaultValue}");
            }

            iniFile.WriteIniFile(filePath);
        }

        /// <summary>
        /// Clears all loaded translations.
        /// </summary>
        public void Clear()
        {
            _values.Clear();
            _missingKeys.Clear();
            _defaultValues.Clear();
            Name = "English";
            Author = "Unknown";
            LocaleCode = "en";
            UICulture = CultureInfo.InvariantCulture;
        }
    }
}