using MapEditorLibrary.Models;
using Rampastring.XNAUI.Windowing;
using System;
using TSMapEditor.Rendering;
using TSMapEditor.UI.Notifications;
using TSMapEditor.UI.Windows.MainMenuWindows;
using TSMapEditor.UI.Windows.TerrainGenerator;

namespace TSMapEditor.UI.Windows;

public class WindowController : XNAWindowController
{
    public event EventHandler Initialized;
    public event EventHandler RenderResolutionChanged;

    public BasicSectionConfigWindow BasicSectionConfigWindow { get; private set; }
    public TaskforcesWindow TaskForcesWindow { get; private set; }
    public ScriptsWindow ScriptsWindow { get; private set; }
    public TeamTypesWindow TeamTypesWindow { get; private set; }
    public TriggersWindow TriggersWindow { get; private set; }
    public TagsWindow TagsWindow { get; private set; }
    public AITriggersWindow AITriggersWindow { get; private set; }
    public PlaceWaypointWindow PlaceWaypointWindow { get; private set; }
    public LocalVariablesWindow LocalVariablesWindow { get; private set; }
    public StructureOptionsWindow StructureOptionsWindow { get; private set; }
    public VehicleOptionsWindow VehicleOptionsWindow { get; private set; }
    public InfantryOptionsWindow InfantryOptionsWindow { get; private set; }
    public AircraftOptionsWindow AircraftOptionsWindow { get; private set; }
    public HousesWindow HousesWindow { get; private set; }
    public SaveMapAsWindow SaveMapAsWindow { get; private set; }
    public CreateNewMapWindow CreateNewMapWindow { get; private set; }
    public OpenMapWindow OpenMapWindow { get; private set; }
    public AutoApplyImpassableOverlayWindow AutoApplyImpassableOverlayWindow { get; private set; }
    public TerrainGeneratorConfigWindow TerrainGeneratorConfigWindow { get; private set; }
    public MegamapWindow MinimapWindow { get; private set; }
    public CopiedEntryTypesWindow CopiedEntryTypesWindow { get; private set; }
    public LightingSettingsWindow LightingSettingsWindow { get; private set; }
    public ApplyINICodeWindow ApplyINICodeWindow { get; private set; }
    public RunScriptWindow RunScriptWindow { get; private set; }
    public HotkeyConfigurationWindow HotkeyConfigurationWindow { get; private set; }
    public MapSizeWindow MapSizeWindow { get; private set; }
    public ExpandMapWindow ExpandMapWindow { get; private set; }
    public ChangeHeightWindow ChangeHeightWindow { get; private set; }
    public FindWaypointWindow FindWaypointWindow { get; private set; }
    public DeletionModeConfigurationWindow DeletionModeConfigurationWindow { get; private set; }
    public RenderedObjectsConfigurationWindow RenderedObjectsConfigurationWindow { get; private set; }
    public ConfigureAlliesWindow ConfigureAlliesWindow { get; private set; }
    public SelectConnectedTileWindow SelectConnectedTileWindow { get; private set; }
    public MegamapGenerationOptionsWindow MegamapGenerationOptionsWindow { get; private set; }
    public HistoryWindow HistoryWindow { get; private set; }
    public AboutWindow AboutWindow { get; private set; }

    public WindowController(Rampastring.XNAUI.Windowing.IWindowParentControl windowParentControl) : base(windowParentControl)
    {
    }

    public void Initialize(Map map, EditorState editorState, NotificationManager notificationManager, ICursorActionTarget cursorActionTarget)
    {
        BasicSectionConfigWindow = new BasicSectionConfigWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(BasicSectionConfigWindow);

        var referenceListWindow = new ReferenceListWindow(WindowParentControl.WindowManager,
            target => NavigateToReference(target, map, cursorActionTarget, notificationManager));
        RegisterWindow(referenceListWindow);

        TaskForcesWindow = new TaskforcesWindow(WindowParentControl.WindowManager, map, referenceListWindow);
        RegisterWindow(TaskForcesWindow);

        ScriptsWindow = new ScriptsWindow(WindowParentControl.WindowManager, map, editorState, notificationManager, cursorActionTarget, referenceListWindow);
        RegisterWindow(ScriptsWindow);

        TeamTypesWindow = new TeamTypesWindow(WindowParentControl.WindowManager, map, referenceListWindow);
        RegisterWindow(TeamTypesWindow);

        TriggersWindow = new TriggersWindow(WindowParentControl.WindowManager, map, editorState, cursorActionTarget, referenceListWindow);
        RegisterWindow(TriggersWindow);

        TagsWindow = new TagsWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(TagsWindow);

        AITriggersWindow = new AITriggersWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(AITriggersWindow);

        PlaceWaypointWindow = new PlaceWaypointWindow(WindowParentControl.WindowManager, map, cursorActionTarget.MutationManager, cursorActionTarget.MutationTarget);
        RegisterWindow(PlaceWaypointWindow);

        LocalVariablesWindow = new LocalVariablesWindow(WindowParentControl.WindowManager, map, referenceListWindow);
        RegisterWindow(LocalVariablesWindow);

        StructureOptionsWindow = new StructureOptionsWindow(WindowParentControl.WindowManager, map, editorState);
        RegisterWindow(StructureOptionsWindow);

        VehicleOptionsWindow = new VehicleOptionsWindow(WindowParentControl.WindowManager, map, editorState, cursorActionTarget);
        RegisterWindow(VehicleOptionsWindow);

        InfantryOptionsWindow = new InfantryOptionsWindow(WindowParentControl.WindowManager, map, cursorActionTarget);
        RegisterWindow(InfantryOptionsWindow);

        AircraftOptionsWindow = new AircraftOptionsWindow(WindowParentControl.WindowManager, map, cursorActionTarget);
        RegisterWindow(AircraftOptionsWindow);

        HousesWindow = new HousesWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(HousesWindow);

        SaveMapAsWindow = new SaveMapAsWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(SaveMapAsWindow);

        CreateNewMapWindow = new CreateNewMapWindow(WindowParentControl.WindowManager);
        RegisterWindow(CreateNewMapWindow);

        OpenMapWindow = new OpenMapWindow(WindowParentControl.WindowManager);
        RegisterWindow(OpenMapWindow);

        AutoApplyImpassableOverlayWindow = new AutoApplyImpassableOverlayWindow(WindowParentControl.WindowManager, map, cursorActionTarget.MutationTarget);
        RegisterWindow(AutoApplyImpassableOverlayWindow);

        TerrainGeneratorConfigWindow = new TerrainGeneratorConfigWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(TerrainGeneratorConfigWindow);

        MinimapWindow = new MegamapWindow(WindowParentControl.WindowManager, cursorActionTarget, true);
        RegisterWindow(MinimapWindow);

        CopiedEntryTypesWindow = new CopiedEntryTypesWindow(WindowParentControl.WindowManager);
        RegisterWindow(CopiedEntryTypesWindow);

        LightingSettingsWindow = new LightingSettingsWindow(WindowParentControl.WindowManager, map, editorState);
        RegisterWindow(LightingSettingsWindow);

        ApplyINICodeWindow = new ApplyINICodeWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(ApplyINICodeWindow);

        RunScriptWindow = new RunScriptWindow(WindowParentControl.WindowManager, new Scripts.ScriptDependencies(map, cursorActionTarget, editorState, WindowParentControl.WindowManager, this, map.FileManager));
        RegisterWindow(RunScriptWindow);

        HotkeyConfigurationWindow = new HotkeyConfigurationWindow(WindowParentControl.WindowManager);
        RegisterWindow(HotkeyConfigurationWindow);

        MapSizeWindow = new MapSizeWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(MapSizeWindow);
        MapSizeWindow.OnResizeMapButtonClicked += MapSizeWindow_OnResizeMapButtonClicked;

        ExpandMapWindow = new ExpandMapWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(ExpandMapWindow);

        ChangeHeightWindow = new ChangeHeightWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(ChangeHeightWindow);

        FindWaypointWindow = new FindWaypointWindow(WindowParentControl.WindowManager, map, cursorActionTarget);
        RegisterWindow(FindWaypointWindow);

        DeletionModeConfigurationWindow = new DeletionModeConfigurationWindow(WindowParentControl.WindowManager, editorState);
        RegisterWindow(DeletionModeConfigurationWindow);

        RenderedObjectsConfigurationWindow = new RenderedObjectsConfigurationWindow(WindowParentControl.WindowManager, editorState);
        RegisterWindow(RenderedObjectsConfigurationWindow);

        ConfigureAlliesWindow = new ConfigureAlliesWindow(WindowParentControl.WindowManager, map);
        RegisterWindow(ConfigureAlliesWindow);

        SelectConnectedTileWindow = new SelectConnectedTileWindow(WindowParentControl.WindowManager, map);
        // TODO add a way for WindowController windows to use DarkeningPanels
        // DarkeningPanel.InitializeAndAddToParentControlWithChild(WindowParentControl.WindowManager, windowParentControl, SelectConnectedTileWindow);
        RegisterWindow(SelectConnectedTileWindow);

        MegamapGenerationOptionsWindow = new MegamapGenerationOptionsWindow(WindowParentControl.WindowManager);
        RegisterWindow(MegamapGenerationOptionsWindow);

        HistoryWindow = new HistoryWindow(WindowParentControl.WindowManager, cursorActionTarget.MutationManager);
        RegisterWindow(HistoryWindow);

        AboutWindow = new AboutWindow(WindowParentControl.WindowManager);
        RegisterWindow(AboutWindow);

        TeamTypesWindow.TaskForceOpened += TeamTypesWindow_TaskForceOpened;
        TeamTypesWindow.ScriptOpened += TeamTypesWindow_ScriptOpened;
        TeamTypesWindow.TagOpened += Window_TagOpened;
        AITriggersWindow.TeamTypeOpened += AITriggersWindow_TeamTypeOpened;
        TriggersWindow.TeamTypeOpened += TriggersWindow_TeamTypeOpened;
        StructureOptionsWindow.TagOpened += Window_TagOpened;
        VehicleOptionsWindow.TagOpened += Window_TagOpened;
        InfantryOptionsWindow.TagOpened += Window_TagOpened;
        AircraftOptionsWindow.TagOpened += Window_TagOpened;

        Initialized?.Invoke(this, EventArgs.Empty);

        WindowParentControl.RenderResolutionChanged += (s, e) => RenderResolutionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void NavigateToReference(object target, Map map, IMapView mapView, NotificationManager notificationManager)
    {
        switch (target)
        {
            case Trigger trigger when map.Triggers.Contains(trigger):
                TriggersWindow.Open();
                TriggersWindow.SelectTrigger(trigger, reveal: true);
                break;

            case TeamType teamType when map.TeamTypes.Contains(teamType):
                TeamTypesWindow.Open();
                TeamTypesWindow.SelectTeamType(teamType, reveal: true);
                break;

            case Script script when map.Scripts.Contains(script):
                ScriptsWindow.Open();
                ScriptsWindow.SelectScript(script, reveal: true);
                break;

            case AITriggerType aiTrigger when map.AITriggerTypes.Contains(aiTrigger):
                AITriggersWindow.Open();
                AITriggersWindow.SelectAITrigger(aiTrigger, reveal: true);
                break;

            case TechnoBase techno when map.GetTile(techno.Position)?.ContainsObject(techno) == true:
                mapView.Camera.CenterOnCell(techno.Position);
                break;

            case CellTag cellTag when map.GetTile(cellTag.Position)?.CellTag == cellTag:
                mapView.Camera.CenterOnCell(cellTag.Position);
                break;

            default:
                notificationManager.AddNotification(Translate(this, "ReferenceTargetMissing", "The referenced object no longer exists."));
                break;
        }
    }

    private void Window_TagOpened(object sender, TagEventArgs e)
    {
        if (e.Tag.Trigger == null)
        {
            EditorMessageBox.Show(WindowParentControl.WindowManager,
                Translate(this, "NoTriggerAttached.Title", "No trigger attached"),
                Translate(this, "NoTriggerAttached.Description", "The specified Tag has no attached Trigger!"),
                MessageBoxButtons.OK);

            return;
        }

        TriggersWindow.Open();
        TriggersWindow.SelectTrigger(e.Tag.Trigger);
    }

    private void MapSizeWindow_OnResizeMapButtonClicked(object sender, EventArgs e)
    {
        ExpandMapWindow.Open();
    }

    private void TeamTypesWindow_TaskForceOpened(object sender, TaskForceEventArgs e)
    {
        TaskForcesWindow.Open();
        TaskForcesWindow.SelectTaskForce(e.TaskForce);
    }

    private void TeamTypesWindow_ScriptOpened(object sender, ScriptEventArgs e)
    {
        ScriptsWindow.Open();
        ScriptsWindow.SelectScript(e.Script);
    }

    private void AITriggersWindow_TeamTypeOpened(object sender, TeamTypeEventArgs e)
    {
        TeamTypesWindow.Open();
        TeamTypesWindow.SelectTeamType(e.TeamType);
    }

    private void TriggersWindow_TeamTypeOpened(object sender, TeamTypeEventArgs e) => AITriggersWindow_TeamTypeOpened(sender, e);

    public override void Clear()
    {
        TeamTypesWindow.TaskForceOpened -= TeamTypesWindow_TaskForceOpened;
        TeamTypesWindow.ScriptOpened -= TeamTypesWindow_ScriptOpened;
        TeamTypesWindow.TagOpened -= Window_TagOpened;
        AITriggersWindow.TeamTypeOpened -= AITriggersWindow_TeamTypeOpened;
        TriggersWindow.TeamTypeOpened -= TriggersWindow_TeamTypeOpened;
        StructureOptionsWindow.TagOpened -= Window_TagOpened;
        VehicleOptionsWindow.TagOpened -= Window_TagOpened;
        InfantryOptionsWindow.TagOpened -= Window_TagOpened;
        MapSizeWindow.OnResizeMapButtonClicked -= MapSizeWindow_OnResizeMapButtonClicked;

        base.Clear();
    }
}
