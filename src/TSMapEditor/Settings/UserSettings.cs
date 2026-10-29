using Rampastring.Tools;
using Rampastring.Tools.IniSettings;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TSMapEditor.Settings;

public class UserSettings : IniSettings
{
    private const string General = "General";
    private const string Display = "Display";
    private const string MapView = "MapView";
    private const string MCPServer = "MCPServer";

    public UserSettings() : base()
    {
        if (Instance != null)
            throw new InvalidOperationException("User settings can only be initialized once.");

        Instance = this;

        string path = Path.Combine(Environment.CurrentDirectory, "MapEditorSettings.ini");
        SettingsIni = new IniFile(path);

        PopulateWithReflection();

        if (!File.Exists(path))
        {
            var defaultSettingsIni = new IniFile(Path.Combine(Environment.CurrentDirectory, "Config", "DefaultSettings.ini"));
            LoadDefaultValueOverrides(defaultSettingsIni);
        }

        LoadAll();
        RecentFiles.ReadFromIniFile(SettingsIni);
        PinnedTileSets.ReadFromIniFile(SettingsIni);
    }

    public void SaveSettings()
    {
        WriteAll();
        RecentFiles.WriteToIniFile(SettingsIni);
        PinnedTileSets.WriteToIniFile(SettingsIni);
        SaveSettingsIni();
    }

    private readonly SemaphoreSlim saveSemaphore = new(1, 1);

    public async Task SaveSettingsAsync()
    {
        await saveSemaphore.WaitAsync().ConfigureAwait(false);

        try
        {
            await Task.Run(SaveSettings).ConfigureAwait(false);
        }
        finally
        {
            saveSemaphore.Release();
        }
    }

    public static UserSettings Instance { get; private set; }

    public IntSetting TargetFPS { get; } = new IntSetting(Display, nameof(TargetFPS), 240);
    public IntSetting GraphicsLevel { get; } = new IntSetting(Display, nameof(GraphicsLevel), 1);
    public IntSetting ResolutionWidth { get; } = new IntSetting(Display, nameof(ResolutionWidth), -1);
    public IntSetting ResolutionHeight { get; } = new IntSetting(Display, nameof(ResolutionHeight), -1);
    public DoubleSetting RenderScale { get; } = new DoubleSetting(Display, nameof(RenderScale), 1.0);
    public BoolSetting Borderless { get; } = new BoolSetting(Display, nameof(Borderless), false);
    public BoolSetting FullscreenWindowed { get; } = new BoolSetting(Display, nameof(FullscreenWindowed), false);
    public BoolSetting ConserveVRAM { get; } = new BoolSetting(Display, nameof(ConserveVRAM), false);
    public BoolSetting EnableWindowGlassEffect { get; } = new BoolSetting(Display, nameof(EnableWindowGlassEffect), true);
    public BoolSetting EnableWindowDropShadowEffect { get; } = new BoolSetting(Display, nameof(EnableWindowDropShadowEffect), true);

    public IntSetting ScrollRate { get; } = new IntSetting(MapView, nameof(ScrollRate), 15);
    public IntSetting MapWideOverlayOpacity { get; } = new IntSetting(MapView, nameof(MapWideOverlayOpacity), 50);
    public BoolSetting DrawExtraGraphicsIn2DMode { get; } = new BoolSetting(MapView, nameof(DrawExtraGraphicsIn2DMode), true);

    public StringSetting Theme { get; } = new StringSetting(General, nameof(Theme), "Default");
    public BoolSetting UseBoldFont { get; } = new BoolSetting(General, nameof(UseBoldFont), false);
    public BoolSetting UseSpriteFonts { get; } = new BoolSetting(General, nameof(UseSpriteFonts), false);
    public BoolSetting SmartScriptActionCloning { get; } = new BoolSetting(General, nameof(SmartScriptActionCloning), true);
    public BoolSetting SmartScriptActionDefaultValues { get; } = new BoolSetting(General, nameof(SmartScriptActionDefaultValues), true);
    public BoolSetting QuickTriggerParameterSelection { get; } = new BoolSetting(General, nameof(QuickTriggerParameterSelection), true);
    public IntSetting AutoSaveInterval { get; } = new IntSetting(General, nameof(AutoSaveInterval), 300);
    public IntSetting SidebarWidth { get; } = new IntSetting(General, nameof(SidebarWidth), 284);
    public IntSetting TileInfoDisplayWidth { get; } = new IntSetting(General, nameof(TileInfoDisplayWidth), -1);
    public BoolSetting DoNotSuggestMPStartingWaypoints { get; } = new BoolSetting(General, nameof(DoNotSuggestMPStartingWaypoints), false);

    public BoolSetting EnableMCP { get; } = new BoolSetting(MCPServer, nameof(EnableMCP), false);
    public IntSetting MCPServerPort { get; } = new IntSetting(MCPServer, nameof(MCPServerPort), 7743);

    public BoolSetting MultithreadedTextureLoading { get; } = new BoolSetting(General, nameof(MultithreadedTextureLoading), true);
    public BoolSetting LogFileLoading { get; } = new BoolSetting(General, nameof(LogFileLoading), false);

    public StringSetting GameDirectory { get; } = new StringSetting(General, nameof(GameDirectory), string.Empty);
    public StringSetting LastScenarioPath { get; } = new StringSetting(General, nameof(LastScenarioPath), "Maps/Custom/");

    public StringSetting TextEditorPath { get; } = new StringSetting(General, nameof(TextEditorPath), string.Empty);

    public StringSetting Language { get; } = new StringSetting(General, nameof(Language), string.Empty);

    public RecentFiles RecentFiles { get; } = new RecentFiles();
    public PinnedTileSets PinnedTileSets { get; } = new PinnedTileSets();
}
