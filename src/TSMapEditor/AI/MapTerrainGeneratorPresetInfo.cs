using System.Collections.Generic;
using System.ComponentModel;
using TSMapEditor.Mutations;

namespace TSMapEditor.AI;

public sealed class MapTerrainGeneratorPresetSummary
{
    public MapTerrainGeneratorPresetSummary(
        string presetId,
        string name,
        string theater,
        bool isUserPreset,
        bool isUsable,
        int terrainTypeGroupCount,
        int tileGroupCount,
        int overlayGroupCount,
        int smudgeGroupCount)
    {
        PresetId = presetId;
        Name = name;
        Theater = theater;
        IsUserPreset = isUserPreset;
        IsUsable = isUsable;
        TerrainTypeGroupCount = terrainTypeGroupCount;
        TileGroupCount = tileGroupCount;
        OverlayGroupCount = overlayGroupCount;
        SmudgeGroupCount = smudgeGroupCount;
    }

    [Description("Stable, unambiguous preset identifier accepted by get_terrain_generator_preset and generate_terrain.")]
    public string PresetId { get; }

    [Description("Human-readable preset name shown by the editor.")]
    public string Name { get; }

    [Description("Theater named by the preset. An empty value means the built-in preset is available in all theaters.")]
    public string Theater { get; }

    [Description("Whether this is a user-saved preset rather than a built-in mod preset.")]
    public bool IsUserPreset { get; }

    [Description("Whether the effective preset configuration can currently be generated safely. Inspect unusable presets with get_terrain_generator_preset for validation errors.")]
    public bool IsUsable { get; }

    public int TerrainTypeGroupCount { get; }
    public int TileGroupCount { get; }
    public int OverlayGroupCount { get; }
    public int SmudgeGroupCount { get; }
}

public class MapTerrainGeneratorChanceGroupInfo
{
    public MapTerrainGeneratorChanceGroupInfo(double openCellChance, double occupiedCellChance)
    {
        OpenCellChance = openCellChance;
        OccupiedCellChance = occupiedCellChance;
    }

    [Description("Independent probability from 0.0 through 1.0 of placing an entry on an open candidate cell.")]
    public double OpenCellChance { get; }

    [Description("Independent probability from 0.0 through 1.0 of placing an entry on a candidate cell occupied by an earlier generator placement.")]
    public double OccupiedCellChance { get; }
}

public sealed class MapTerrainGeneratorTerrainTypeGroupInfo : MapTerrainGeneratorChanceGroupInfo
{
    public MapTerrainGeneratorTerrainTypeGroupInfo(double openCellChance, double occupiedCellChance, List<string> terrainTypeNames)
        : base(openCellChance, occupiedCellChance)
    {
        TerrainTypeNames = terrainTypeNames;
    }

    [Description("Terrain-object INI names from which one entry is chosen randomly when this group places an object.")]
    public List<string> TerrainTypeNames { get; }
}

public sealed class MapTerrainGeneratorTileGroupInfo : MapTerrainGeneratorChanceGroupInfo
{
    public MapTerrainGeneratorTileGroupInfo(
        double openCellChance,
        double occupiedCellChance,
        string tileSetName,
        bool usesAllTilesInSet,
        List<int> tileIndicesInSet)
        : base(openCellChance, occupiedCellChance)
    {
        TileSetName = tileSetName;
        UsesAllTilesInSet = usesAllTilesInSet;
        TileIndicesInSet = tileIndicesInSet;
    }

    [Description("Internal tile-set name.")]
    public string TileSetName { get; }

    [Description("Whether the generator randomly chooses from every tile entry in the tile set.")]
    public bool UsesAllTilesInSet { get; }

    [Description("Zero-based tile-set-relative indices used when usesAllTilesInSet is false; otherwise empty.")]
    public List<int> TileIndicesInSet { get; }
}

public sealed class MapTerrainGeneratorOverlayGroupInfo : MapTerrainGeneratorChanceGroupInfo
{
    public MapTerrainGeneratorOverlayGroupInfo(
        double openCellChance,
        double occupiedCellChance,
        string overlayTypeName,
        bool usesAllFrames,
        List<int> frameIndices)
        : base(openCellChance, occupiedCellChance)
    {
        OverlayTypeName = overlayTypeName;
        UsesAllFrames = usesAllFrames;
        FrameIndices = frameIndices;
    }

    [Description("Overlay-type INI name.")]
    public string OverlayTypeName { get; }

    [Description("Whether the generator randomly chooses from every loaded frame of the overlay.")]
    public bool UsesAllFrames { get; }

    [Description("Zero-based overlay frame indices used when usesAllFrames is false; otherwise empty.")]
    public List<int> FrameIndices { get; }
}

public sealed class MapTerrainGeneratorSmudgeGroupInfo : MapTerrainGeneratorChanceGroupInfo
{
    public MapTerrainGeneratorSmudgeGroupInfo(double openCellChance, double occupiedCellChance, List<string> smudgeTypeNames)
        : base(openCellChance, occupiedCellChance)
    {
        SmudgeTypeNames = smudgeTypeNames;
    }

    [Description("Smudge-type INI names from which one entry is chosen randomly when this group places a smudge.")]
    public List<string> SmudgeTypeNames { get; }
}

public sealed class MapTerrainGeneratorPresetInfo
{
    public MapTerrainGeneratorPresetInfo(
        string presetId,
        string name,
        string theater,
        bool isUserPreset,
        List<string> validationErrors,
        List<MapTerrainGeneratorTerrainTypeGroupInfo> terrainTypeGroups,
        List<MapTerrainGeneratorTileGroupInfo> tileGroups,
        List<MapTerrainGeneratorOverlayGroupInfo> overlayGroups,
        List<MapTerrainGeneratorSmudgeGroupInfo> smudgeGroups)
    {
        PresetId = presetId;
        Name = name;
        Theater = theater;
        IsUserPreset = isUserPreset;
        ValidationErrors = validationErrors;
        TerrainTypeGroups = terrainTypeGroups;
        TileGroups = tileGroups;
        OverlayGroups = overlayGroups;
        SmudgeGroups = smudgeGroups;
    }

    [Description("Stable, unambiguous preset identifier accepted by generate_terrain.")]
    public string PresetId { get; }

    [Description("Human-readable preset name shown by the editor.")]
    public string Name { get; }

    [Description("Theater named by the preset. An empty value means the built-in preset is available in all theaters.")]
    public string Theater { get; }

    [Description("Whether this is a user-saved preset rather than a built-in mod preset.")]
    public bool IsUserPreset { get; }

    [Description("Whether the effective preset configuration can currently be generated safely.")]
    public bool IsUsable => ValidationErrors.Count == 0;

    [Description("Configuration problems that prevent generation; empty when the preset is usable.")]
    public List<string> ValidationErrors { get; }

    public List<MapTerrainGeneratorTerrainTypeGroupInfo> TerrainTypeGroups { get; }
    public List<MapTerrainGeneratorTileGroupInfo> TileGroups { get; }
    public List<MapTerrainGeneratorOverlayGroupInfo> OverlayGroups { get; }
    public List<MapTerrainGeneratorSmudgeGroupInfo> SmudgeGroups { get; }
}

public sealed class MapTerrainGenerationResult
{
    public MapTerrainGenerationResult(
        int revision,
        string presetId,
        int candidateCellCount,
        int terrainCellWriteCount,
        int placedTerrainObjectCount,
        int placedOverlayCount,
        int placedSmudgeCount,
        bool autoLATApplied,
        MutationAffectedCells potentialAffectedCells)
    {
        Revision = revision;
        PresetId = presetId;
        CandidateCellCount = candidateCellCount;
        TerrainCellWriteCount = terrainCellWriteCount;
        PlacedTerrainObjectCount = placedTerrainObjectCount;
        PlacedOverlayCount = placedOverlayCount;
        PlacedSmudgeCount = placedSmudgeCount;
        AutoLATApplied = autoLATApplied;
        PotentialAffectedCells = potentialAffectedCells;
    }

    [Description("Map revision after generation.")]
    public int Revision { get; }

    [Description("Exact preset ID used for generation.")]
    public string PresetId { get; }

    [Description("Number of valid map cells from the requested rectangle considered by every generator group.")]
    public int CandidateCellCount { get; }

    [Description("Number of distinct map cells directly written by generated full terrain tiles, before AutoLAT transitions.")]
    public int TerrainCellWriteCount { get; }

    [Description("Number of terrain objects placed by the generator.")]
    public int PlacedTerrainObjectCount { get; }

    [Description("Number of overlays placed by the generator.")]
    public int PlacedOverlayCount { get; }

    [Description("Number of smudges placed by the generator.")]
    public int PlacedSmudgeCount { get; }

    [Description("Whether AutoLAT was applied after generation.")]
    public bool AutoLATApplied { get; }

    [Description("Summary of the cells that could have changed, including full-tile footprints and possible AutoLAT neighbors. Individual random placements can affect fewer cells.")]
    public MutationAffectedCells PotentialAffectedCells { get; }
}
