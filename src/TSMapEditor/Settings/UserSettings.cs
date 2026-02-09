using Rampastring.Tools;
using System;
using System.IO;
using System.Threading.Tasks;

namespace TSMapEditor.Settings
{
    public class UserSettings
    {
        private const string General = "General";
        private const string Display = "Display";
        private const string MapView = "MapView";

        public UserSettings()
        {
            if (Instance != null)
                throw new InvalidOperationException("User settings can only be initialized once.");

            Instance = this;

            string path = Path.Combine(Environment.CurrentDirectory, "MapEditorSettings.ini");
            if (File.Exists(path))
            {
                UserSettingsIni = new IniFile(path);
            }
            else
            {
                UserSettingsIni = new IniFile(Path.Combine(Environment.CurrentDirectory, "Config", "DefaultSettings.ini"));
                UserSettingsIni.FileName = path;
            }

            settings = new IINILoadable[]
            {
                TargetFPS,
                GraphicsLevel,
                ResolutionWidth,
                ResolutionHeight,
                RenderScale,
                Borderless,
                FullscreenWindowed,
                ConserveVRAM,

                ScrollRate,
                MapWideOverlayOpacity,

                Theme,
                UseBoldFont,
                SmartScriptActionCloning,
                SmartScriptActionDefaultValues,
                QuickTriggerParameterSelection,
                AutoSaveInterval,
                SidebarWidth,

                MultithreadedTextureLoading,
                LogFileLoading,

                GameDirectory,
                LastScenarioPath,

                TextEditorPath,

                Language
            };

            foreach (var setting in settings)
                setting.LoadValue(UserSettingsIni);

            RecentFiles.ReadFromIniFile(UserSettingsIni);
        }

        public IniFile UserSettingsIni { get; }

        public void SaveSettings()
        {
            foreach (var setting in settings)
            {
                setting.WriteValue(UserSettingsIni, false);
            }

            RecentFiles.WriteToIniFile(UserSettingsIni);

            UserSettingsIni.WriteIniFile();
        }

        public async Task SaveSettingsAsync()
        {
            await Task.Factory.StartNew(SaveSettings);
        }

        public static UserSettings Instance { get; private set; }

        private readonly IINILoadable[] settings;

        public IntSetting TargetFPS = new IntSetting(Display, nameof(TargetFPS), 240);
        public IntSetting GraphicsLevel = new IntSetting(Display, nameof(GraphicsLevel), 1);
        public IntSetting ResolutionWidth = new IntSetting(Display, nameof(ResolutionWidth), -1);
        public IntSetting ResolutionHeight = new IntSetting(Display, nameof(ResolutionHeight), -1);
        public DoubleSetting RenderScale = new DoubleSetting(Display, nameof(RenderScale), 1.0);
        public BoolSetting Borderless = new BoolSetting(Display, nameof(Borderless), false);
        public BoolSetting FullscreenWindowed = new BoolSetting(Display, nameof(FullscreenWindowed), false);
        public BoolSetting ConserveVRAM = new BoolSetting(Display, nameof(ConserveVRAM), false);

        public IntSetting ScrollRate = new IntSetting(MapView, nameof(ScrollRate), 15);
        public IntSetting MapWideOverlayOpacity = new IntSetting(MapView, nameof(MapWideOverlayOpacity), 50);

        public StringSetting Theme = new StringSetting(General, nameof(Theme), "Default");
        public BoolSetting UseBoldFont = new BoolSetting(General, nameof(UseBoldFont), false);
        public BoolSetting SmartScriptActionCloning = new BoolSetting(General, nameof(SmartScriptActionCloning), true);
        public BoolSetting SmartScriptActionDefaultValues = new BoolSetting(General, nameof(SmartScriptActionDefaultValues), true);
        public BoolSetting QuickTriggerParameterSelection = new BoolSetting(General, nameof(QuickTriggerParameterSelection), true);
        public IntSetting AutoSaveInterval = new IntSetting(General, nameof(AutoSaveInterval), 300);
        public IntSetting SidebarWidth = new IntSetting(General, nameof(SidebarWidth), 250);
        public BoolSetting DoNotSuggestMPStartingWaypoints = new BoolSetting(General, nameof(DoNotSuggestMPStartingWaypoints), false);

        public BoolSetting MultithreadedTextureLoading = new BoolSetting(General, nameof(MultithreadedTextureLoading), true);
        public BoolSetting LogFileLoading = new BoolSetting(General, nameof(LogFileLoading), false);

        public StringSetting GameDirectory = new StringSetting(General, nameof(GameDirectory), string.Empty);
        public StringSetting LastScenarioPath = new StringSetting(General, nameof(LastScenarioPath), "Maps/Custom/");

        public StringSetting TextEditorPath = new StringSetting(General, nameof(TextEditorPath), string.Empty);

        public StringSetting Language = new StringSetting(General, nameof(Language), string.Empty);

        public RecentFiles RecentFiles = new RecentFiles();
    }
}
