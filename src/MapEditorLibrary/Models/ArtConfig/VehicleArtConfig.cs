using Rampastring.Tools;

namespace MapEditorLibrary.Models.ArtConfig;

public class VehicleArtConfig : IArtConfig
{
    public bool Voxel { get; set; }
    public int TurretOffset { get; set; }
    public bool Remapable => true;
    public int StartStandFrame { get; set; } = -1;
    public int StandingFrames { get; set; } = -1;
    public int StartWalkFrame { get; set; }

    // Hackity hackity hack hack
    // DTA patches the unit type constructor to default WalkFrames to 1
    public int WalkFrames { get; set; } = Constants.ExpectedClientExecutableNames.FirstOrDefault() == "DTA.exe" ? 1 : 12;
    public int Facings { get; set; } = -1;

    public int FiringFrames { get; set; }

    /// <summary>
    /// Vinifera addition, override for start turret frame.
    /// If not specified, it's negative.
    /// </summary>
    public int StartTurretFrame { get; set; } = -1;

    public int TurretFacings { get; set; } = Constants.TurretFrameCount;

    /// <summary>
    /// Palette override introduced in Red Alert 2.
    /// Not actually used by game for vehicles without Phobos.
    /// </summary>
    public string Palette { get; set; }

    public void ReadFromIniSection(IniSection iniSection)
    {
        if (iniSection == null)
            return;

        Voxel = iniSection.GetBooleanValue(nameof(Voxel), Voxel);
        TurretOffset = iniSection.GetIntValue(nameof(TurretOffset), TurretOffset);
        StartStandFrame = iniSection.GetIntValue(nameof(StartStandFrame), StartStandFrame);
        StandingFrames = iniSection.GetIntValue(nameof(StandingFrames), StandingFrames);
        StartWalkFrame = iniSection.GetIntValue(nameof(StartWalkFrame), StartWalkFrame);
        WalkFrames = iniSection.GetIntValue(nameof(WalkFrames), WalkFrames);
        FiringFrames = iniSection.GetIntValue(nameof(FiringFrames), FiringFrames);
        Facings = iniSection.GetIntValue(nameof(Facings), Facings);
        StartTurretFrame = iniSection.GetIntValue(nameof(StartTurretFrame), StartTurretFrame);
        TurretFacings = iniSection.GetIntValue(nameof(TurretFacings), TurretFacings);

        Palette = iniSection.GetStringValue(nameof(Palette), Palette);
    }
}
