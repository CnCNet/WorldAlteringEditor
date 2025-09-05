using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TSMapEditor.Misc
{
    public class Translation
    {
        private const string UIStringTableSection = "UIStringTable";

        private static Dictionary<string, string> UIStringTable = new Dictionary<string, string>();
        private static Dictionary<string, string> MissingValues = new Dictionary<string, string>();

        private static Dictionary<string, string> ObjectNames = new Dictionary<string, string>();

        public Translation(string internalName, string directoryName, string uiName, int index)
        {
            if (string.IsNullOrWhiteSpace(internalName))
                throw new ArgumentException("Translation internal name cannot be empty.");

            if (string.IsNullOrWhiteSpace(directoryName))
                throw new ArgumentException("Translation directory name cannot be empty.");

            InternalName = internalName;
            DirectoryName = directoryName;
            UIName = uiName;
            Index = index;
        }

        public Translation(IniSection definitionSection, int index)
        {
            if (definitionSection == null)
                throw new ArgumentNullException("Missing INI section in translation definition!");

            InternalName = definitionSection.SectionName;

            DirectoryName = definitionSection.GetStringValue("Directory", null);
            if (string.IsNullOrWhiteSpace(DirectoryName))
                throw new InvalidOperationException("DirectoryName cannot be empty for a translation. Translation section: " + definitionSection.SectionName);

            UIName = definitionSection.GetStringValue("UIName", null);
            if (string.IsNullOrWhiteSpace(UIName))
                throw new InvalidOperationException("UIName cannot be empty for a translation. Translation section: " + definitionSection.SectionName);

            if (definitionSection.KeyExists(nameof(IniFileEncoding)))
            {
                IniFileEncoding = Encoding.GetEncoding(definitionSection.GetIntValue(nameof(IniFileEncoding), 0));
            }

            Index = index;
        }

        public string InternalName { get; }

        public string DirectoryName { get; }

        public Encoding IniFileEncoding { get; } = Encoding.UTF8;

        public string UIName { get; }

        public int Index { get; }

        private string ProcessEscapes(string str)
        {
            return str.Replace("\\s", " ").Replace("@", Environment.NewLine);
        }

        private string EscapeString(string str)
        {
            while (str.StartsWith(" "))
            {
                str = "\\s" + str.Substring(1);
            }

            for (int i = str.Length - 1; i >= 0; i--)
            {
                if (str[i] == ' ')
                    str = str.Substring(0, i) + "\\s" + str.Substring(i + 1);
                else
                    break;
            }

            return str.Replace(Environment.NewLine, "@");
        }

        public void Load()
        {
            UIStringTable.Clear();
            ObjectNames.Clear();

            var uiStringsIniFile = new IniFile(Path.Combine(Environment.CurrentDirectory, "Config", "Translations", DirectoryName, "Translation_" + DirectoryName + ".ini"), IniFileEncoding);

            if (uiStringsIniFile.SectionExists(UIStringTableSection))
            {
                var section = uiStringsIniFile.GetSection(UIStringTableSection);

                foreach (var kvp in section.Keys)
                {
                    UIStringTable.Add(kvp.Key, ProcessEscapes(kvp.Value));
                }
            }

            var objectStringsIniFile = new IniFile(Path.Combine(Environment.CurrentDirectory, "Config", "Translations", DirectoryName, "ObjectNames_" + DirectoryName + ".ini"), IniFileEncoding);

            if (objectStringsIniFile.SectionExists("ObjectNames"))
            {
                var section = objectStringsIniFile.GetSection("ObjectNames");

                foreach (var kvp in section.Keys)
                {
                    ObjectNames.Add(kvp.Key, ProcessEscapes(kvp.Value));
                }
            }
        }

        public string Translate(string identifier, string defaultString)
        {
            if (UIStringTable.TryGetValue(identifier, out var result))
                return result;

            if (defaultString != null && !MissingValues.ContainsKey(identifier))
                MissingValues[identifier] = defaultString;

            return defaultString;
        }

        public string TranslateObject(string iniName)
        {
            if (ObjectNames.TryGetValue(iniName, out var result))
                return result;

            return null;
        }

        public void DumpMissingValues()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "MissingTranslationValues.ini");
            File.Delete(path);
            var iniFile = new IniFile(path);

            foreach (var kvp in MissingValues)
            {
                iniFile.SetStringValue(UIStringTableSection, kvp.Key, EscapeString(kvp.Value));
            }

            iniFile.WriteIniFile();
        }
    }

    public static class TranslatorSetup
    {
        public static Translation ActiveTranslation;
        public static List<Translation> Translations;

        public static string Translate(string identifier, string defaultString)
        {
            if (ActiveTranslation == null)
                return defaultString;

            return ActiveTranslation.Translate(identifier, defaultString);
        }

        public static string Translate(object contextObject, string identifier, string defaultString)
        {
            if (ActiveTranslation == null)
                return defaultString;

            return ActiveTranslation.Translate(contextObject.GetType().Name + "." + identifier, defaultString);
        }

        public static string TranslateObject(string iniName, string uiName)
        {
            if (ActiveTranslation == null)
                return uiName;

            string translation = ActiveTranslation.TranslateObject(iniName);
            if (translation == null)
                return uiName;

            return translation;
        }

        public static string ActiveTranslationDirectory()
        {
            if (ActiveTranslation == null)
                return null;

            return ActiveTranslation.DirectoryName;
        }

        private static void AddTranslation(Translation translation)
        {
            if (Translations.Exists(tr => tr.DirectoryName == translation.DirectoryName))
                throw new InvalidOperationException("Multiple translations cannot share the same directory name.");

            Translations.Add(translation);
        }

        public static void LoadTranslations()
        {
            if (Translations != null)
                throw new InvalidOperationException("Translations have already been loaded!");

            Translations = new List<Translation>();

            var iniFile = new IniFile(Path.Combine(Environment.CurrentDirectory, "Config", "Translations.ini"));

            if (iniFile != null)
            {
                var translationsSection = iniFile.GetSectionKeys("Translations");
                foreach (var key in translationsSection)
                {
                    var translationSectionName = iniFile.GetStringValue("Translations", key, string.Empty);
                    if (string.IsNullOrWhiteSpace(translationSectionName))
                        continue;

                    var iniSection = iniFile.GetSection(translationSectionName);

                    var translation = new Translation(iniSection, Translations.Count);
                    AddTranslation(translation);
                }
            }
            else
            {
                var englishTranslation = new Translation("English", "en", "English", Translations.Count);
                AddTranslation(englishTranslation);
            }
        }

        public static void SetActiveTranslation(string internalName)
        {
            if (Translations == null)
                throw new InvalidOperationException("Attempted to set active translations before translations have been initialized!");

            if (Translations.Count == 0)
                throw new InvalidOperationException("No translations exist!");

            var translation = Translations.Find(tr => tr.InternalName == internalName);
            ActiveTranslation = translation ?? Translations[0];
            ActiveTranslation.Load();
        }

        public static void DumpMissingValues()
        {
            if (ActiveTranslation != null)
                ActiveTranslation.DumpMissingValues();
        }
    }

    public static class Translator
    {
        public static string Translate(string identifier, string defaultString) => TranslatorSetup.Translate(identifier, defaultString);

        public static string Translate(object contextObject, string identifier, string defaultString) => TranslatorSetup.Translate(contextObject, identifier, defaultString);

        public static string TranslateObject(string iniName, string uiName) => TranslatorSetup.TranslateObject(iniName, uiName);
    }
}
