using MapEditorLibrary;
using MapEditorLibrary.CCEngine;
using MapEditorLibrary.Models;
using MapEditorLibrary.Models.Enums;
using System.Globalization;

namespace MapEditorMCP.Scripting;

internal sealed class ScriptingParameterCodec
{
    public ScriptingParameterCodec(Map map)
    {
        this.map = map;
    }

    private readonly Map map;

    public int EncodeScriptArgument(
        ScriptActionInput input,
        ScriptAction definition,
        Func<ScriptingElementReference, ScriptingElementKind, string, object> resolveReference,
        string path)
    {
        if (input == null)
            throw new ScriptingValidationException(path, "A script action cannot be null.");

        if (definition == null)
        {
            if (input.Reference != null || !TryParseInt(input.Value, out int rawValue))
                throw new ScriptingValidationException(path, "An unknown script action requires its exact raw integer argument in value.");

            return rawValue;
        }

        if (input.Reference != null && !IsReferenceType(definition.ParamType))
            throw new ScriptingValidationException(path + ".reference",
                $"Parameter type {definition.ParamType} does not accept a scripting-element reference.");

        string semanticValue = ResolveScriptPresetValue(definition, input.Value);

        if (definition.ParamType == TriggerParamType.Speech)
            return ResolveScriptSpeechIndex(semanticValue, path + ".value");
        if (definition.ParamType == TriggerParamType.Sound)
            return ResolveScriptSoundIndex(semanticValue, path + ".value");
        if (definition.ParamType == TriggerParamType.Theme)
            return ResolveScriptThemeIndex(semanticValue, path + ".value");
        if (definition.ParamType is TriggerParamType.Text or TriggerParamType.Movie or TriggerParamType.StringTableEntry)
            return RequireInteger(semanticValue, path + ".value");

        string encodedValue = Encode(
            definition.ParamType,
            semanticValue,
            input.Reference,
            resolveReference,
            path + ".value");

        if (!int.TryParse(encodedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int argument))
        {
            throw new ScriptingValidationException(
                path,
                $"Script action {definition.ID} ({definition.Name}) parameter type {definition.ParamType} did not encode to an integer.");
        }

        return argument;
    }

    public string Encode(
        TriggerParamType parameterType,
        string value,
        ScriptingElementReference reference,
        Func<ScriptingElementReference, ScriptingElementKind, string, object> resolveReference,
        string path)
    {
        if (parameterType == TriggerParamType.Unused)
            throw new ScriptingValidationException(path, "This parameter is unused and cannot be supplied.");
        if (reference != null && !IsReferenceType(parameterType))
            throw new ScriptingValidationException(path + ".reference", $"Parameter type {parameterType} does not accept a scripting-element reference.");

        switch (parameterType)
        {
            case TriggerParamType.LocalVariable:
                return ResolveLocalVariable(value, reference, resolveReference, path).Index.ToString(CultureInfo.InvariantCulture);
            case TriggerParamType.TeamType:
                return ResolveByReference<TeamType>(value, reference, ScriptingElementKind.TeamType, resolveReference, path).ININame;
            case TriggerParamType.Trigger:
                return ResolveByReference<Trigger>(value, reference, ScriptingElementKind.Trigger, resolveReference, path).ID;
            case TriggerParamType.Tag:
                return ResolveByReference<Tag>(value, reference, ScriptingElementKind.Tag, resolveReference, path).ID;
            case TriggerParamType.Waypoint:
                return ResolveWaypoint(value, path).ToString(CultureInfo.InvariantCulture);
            case TriggerParamType.WaypointZZ:
                return Helpers.WaypointNumberToAlphabeticalString(ResolveWaypoint(value, path));
            case TriggerParamType.Float:
                if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue) || !float.IsFinite(floatValue))
                    throw new ScriptingValidationException(path, $"'{value}' is not a finite floating-point value.");
                return BitConverter.SingleToInt32Bits(floatValue).ToString(CultureInfo.InvariantCulture);
            case TriggerParamType.Cell:
                return EncodeCell(value, path);
            case TriggerParamType.Boolean:
                if (bool.TryParse(value, out bool booleanValue))
                    return booleanValue ? "1" : "0";
                int booleanInteger = RequireInteger(value, path);
                if (booleanInteger != 0 && booleanInteger != 1)
                    throw new ScriptingValidationException(path, "A Boolean parameter must be false/0 or true/1.");
                return booleanInteger.ToString(CultureInfo.InvariantCulture);
            case TriggerParamType.HouseType:
                return ResolveHouseTypeIndex(value, path).ToString(CultureInfo.InvariantCulture);
            case TriggerParamType.House:
                return ResolveHouseIndex(value, path).ToString(CultureInfo.InvariantCulture);
            case TriggerParamType.GlobalVariable:
            {
                int index = RequireInteger(value, path);
                if (!map.Rules.GlobalVariables.Exists(variable => variable.Index == index))
                    throw new ScriptingValidationException(path, $"Global variable index {index} does not exist in the loaded rules.");
                return index.ToString(CultureInfo.InvariantCulture);
            }
            case TriggerParamType.Techno:
                return ResolveTechnoName(value, path);
            case TriggerParamType.BuildingName:
                return ResolveTypeName(map.Rules.BuildingTypes.Select(type => type.ININame), value, "building", path);
            case TriggerParamType.Building:
                return ResolveTypeIndex(map.Rules.BuildingTypes.Select(type => type.ININame).ToArray(), value, "building", path);
            case TriggerParamType.Aircraft:
                return ResolveTypeIndex(map.Rules.AircraftTypes.Select(type => type.ININame).ToArray(), value, "aircraft", path);
            case TriggerParamType.Infantry:
                return ResolveTypeIndex(map.Rules.InfantryTypes.Select(type => type.ININame).ToArray(), value, "infantry", path);
            case TriggerParamType.Unit:
                return ResolveTypeIndex(map.Rules.UnitTypes.Select(type => type.ININame).ToArray(), value, "unit", path);
            case TriggerParamType.Animation:
                return ResolveTypeIndex(map.Rules.AnimTypes.Select(type => type.ININame).ToArray(), value, "animation", path);
            case TriggerParamType.ParticleSystem:
                return ResolveTypeIndex(map.Rules.ParticleSystemTypes.Select(type => type.ININame).ToArray(), value, "particle system", path);
            case TriggerParamType.SuperWeapon:
                return ResolveTypeIndex(map.Rules.SuperWeaponTypes.Select(type => type.ININame).ToArray(), value, "super weapon", path);
            case TriggerParamType.SuperWeaponName:
                return ResolveTypeName(map.Rules.SuperWeaponTypes.Select(type => type.ININame), value, "super weapon", path);
            case TriggerParamType.Weapon:
                return ResolveTypeIndex(map.Rules.Weapons.Select(type => type.ININame).ToArray(), value, "weapon", path);
            case TriggerParamType.Color:
                if (TryParseInt(value, out int colorIndex) && colorIndex == -1)
                    return "-1";
                return ResolveTypeIndex(map.Rules.Colors.Select(color => color.Name).ToArray(), value, "color", path);
            case TriggerParamType.BuildingWithProperty:
                return EncodeBuildingWithProperty(value, path);
            case TriggerParamType.Unknown:
            case TriggerParamType.Number:
            case TriggerParamType.Quarry:
            case TriggerParamType.SpotlightBehaviour:
            case TriggerParamType.RadarEvent:
            case TriggerParamType.VoxelAnim:
            case TriggerParamType.ComparisonType:
                return RequireInteger(value, path).ToString(CultureInfo.InvariantCulture);
            case TriggerParamType.Sound:
                return EncodeTriggerSound(value, path);
            case TriggerParamType.Speech:
                return EncodeTriggerSpeech(value, path);
            case TriggerParamType.Theme:
                return ResolveScriptThemeIndex(value, path).ToString(CultureInfo.InvariantCulture);
            case TriggerParamType.Text:
                return RequireInteger(value, path).ToString(CultureInfo.InvariantCulture);
            case TriggerParamType.Movie:
            case TriggerParamType.String:
            case TriggerParamType.StringTableEntry:
                return RequireCommaSafeValue(value, path);
            default:
                return RequireCommaSafeValue(value, path);
        }
    }

    public (string Value, ScriptingElementReference Reference) Decode(TriggerParamType parameterType, string rawValue)
    {
        switch (parameterType)
        {
            case TriggerParamType.LocalVariable:
                return (rawValue, int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int variableIndex)
                    ? new ScriptingElementReference { Kind = ScriptingElementKind.LocalVariable, Index = variableIndex }
                    : null);
            case TriggerParamType.TeamType:
                return (rawValue, new ScriptingElementReference
                {
                    Kind = ScriptingElementKind.TeamType,
                    Id = ResolveCanonicalReferenceId(ScriptingElementKind.TeamType, rawValue)
                });
            case TriggerParamType.Trigger:
                return (rawValue, new ScriptingElementReference
                {
                    Kind = ScriptingElementKind.Trigger,
                    Id = ResolveCanonicalReferenceId(ScriptingElementKind.Trigger, rawValue)
                });
            case TriggerParamType.Tag:
                return (rawValue, new ScriptingElementReference
                {
                    Kind = ScriptingElementKind.Tag,
                    Id = ResolveCanonicalReferenceId(ScriptingElementKind.Tag, rawValue)
                });
            case TriggerParamType.WaypointZZ:
                try
                {
                    return (Helpers.GetWaypointNumberFromAlphabeticalString(rawValue).ToString(CultureInfo.InvariantCulture), null);
                }
                catch (InvalidOperationException)
                {
                    return (rawValue, null);
                }
            case TriggerParamType.Float:
                if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int floatBits))
                    return (BitConverter.Int32BitsToSingle(floatBits).ToString("R", CultureInfo.InvariantCulture), null);
                return (rawValue, null);
            default:
                return (rawValue, null);
        }
    }

    private LocalVariable ResolveLocalVariable(
        string value,
        ScriptingElementReference reference,
        Func<ScriptingElementReference, ScriptingElementKind, string, object> resolveReference,
        string path)
    {
        if (reference == null)
        {
            int index = RequireInteger(value, path);
            reference = new ScriptingElementReference { Kind = ScriptingElementKind.LocalVariable, Index = index };
        }

        return (LocalVariable)resolveReference(reference, ScriptingElementKind.LocalVariable, path);
    }

    private static string ResolveScriptPresetValue(ScriptAction definition, string value)
    {
        if (string.IsNullOrWhiteSpace(value) || definition.PresetOptions.Count == 0)
            return value;

        ScriptActionPresetOption preset = definition.PresetOptions.Find(option =>
            string.Equals(option.Text, value.Trim(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(option.GetOptionText(), value.Trim(), StringComparison.OrdinalIgnoreCase));
        return preset == null ? value : preset.Value.ToString(CultureInfo.InvariantCulture);
    }

    private string ResolveCanonicalReferenceId(ScriptingElementKind kind, string rawValue)
    {
        IEnumerable<string> ids = kind switch
        {
            ScriptingElementKind.TeamType => map.TeamTypes.Select(teamType => teamType.ININame)
                .Concat(map.Rules.TeamTypes.Select(teamType => teamType.ININame)),
            ScriptingElementKind.Trigger => map.Triggers.Select(trigger => trigger.ID),
            ScriptingElementKind.Tag => map.Tags.Select(tag => tag.ID),
            _ => Enumerable.Empty<string>()
        };

        string exactId = ids.FirstOrDefault(id => string.Equals(id, rawValue, StringComparison.OrdinalIgnoreCase));
        if (exactId != null)
            return exactId;
        if (!int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rawNumber))
            return rawValue;

        return ids.FirstOrDefault(id =>
            int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idNumber) && idNumber == rawNumber) ?? rawValue;
    }

    private int ResolveScriptSpeechIndex(string value, string path)
    {
        EvaSpeeches speeches = Constants.IsRA2YR ? map.Rules.Speeches : map.EditorConfig.Speeches;
        if (TryParseInt(value, out int index))
        {
            if (speeches?.Get(index) != null)
                return index;
            throw new ScriptingValidationException(path, $"Speech index {index} does not exist.");
        }

        EvaSpeech speech = speeches?.List?.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return speech?.Index ?? throw new ScriptingValidationException(path, $"Speech '{value}' does not exist.");
    }

    private int ResolveScriptSoundIndex(string value, string path)
    {
        if (TryParseInt(value, out int index))
        {
            if (map.Rules.Sounds?.Get(index) != null)
                return index;
            throw new ScriptingValidationException(path, $"Sound index {index} does not exist.");
        }

        Sound sound = map.Rules.Sounds?.List?.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return sound?.Index ?? throw new ScriptingValidationException(path, $"Sound '{value}' does not exist.");
    }

    private int ResolveScriptThemeIndex(string value, string path)
    {
        if (TryParseInt(value, out int index))
        {
            if (map.Rules.Themes?.Get(index) != null)
                return index;
            throw new ScriptingValidationException(path, $"Theme index {index} does not exist.");
        }

        Theme theme = map.Rules.Themes?.List?.FirstOrDefault(candidate =>
            string.Equals(candidate.ININame, value?.Trim(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return theme?.Index ?? throw new ScriptingValidationException(path, $"Theme '{value}' does not exist.");
    }

    private string EncodeTriggerSpeech(string value, string path)
    {
        int index = ResolveScriptSpeechIndex(value, path);
        if (!Constants.IsRA2YR)
            return index.ToString(CultureInfo.InvariantCulture);
        return map.Rules.Speeches.Get(index).Name;
    }

    private string EncodeTriggerSound(string value, string path)
    {
        int index = ResolveScriptSoundIndex(value, path);
        if (!Constants.IsRA2YR)
            return index.ToString(CultureInfo.InvariantCulture);
        return map.Rules.Sounds.Get(index).Name;
    }

    private static T ResolveByReference<T>(
        string value,
        ScriptingElementReference reference,
        ScriptingElementKind kind,
        Func<ScriptingElementReference, ScriptingElementKind, string, object> resolveReference,
        string path)
        where T : class
    {
        reference ??= new ScriptingElementReference { Kind = kind, Id = RequireText(value, path) };
        return (T)resolveReference(reference, kind, path);
    }

    private int ResolveWaypoint(string value, string path)
    {
        int waypoint = RequireInteger(value, path);
        if (waypoint < 0 || waypoint >= Constants.MaxWaypoint)
            throw new ScriptingValidationException(path, $"Waypoint must be from 0 through {Constants.MaxWaypoint - 1}.");
        if (!map.Waypoints.Exists(existingWaypoint => existingWaypoint.Identifier == waypoint))
            throw new ScriptingValidationException(path, $"Waypoint {waypoint} does not exist on the map.");
        return waypoint;
    }

    private string EncodeCell(string value, string path)
    {
        string text = RequireText(value, path);
        string[] parts = text.Split(',');
        if (parts.Length == 1)
        {
            int cellNumber = RequireInteger(text, path);
            ValidateCell(cellNumber % 1000, cellNumber / 1000, path);
            return cellNumber.ToString(CultureInfo.InvariantCulture);
        }
        if (parts.Length != 2 ||
            !int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ||
            !int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
        {
            throw new ScriptingValidationException(path, "A cell must be a raw cell number or an 'x,y' coordinate.");
        }

        ValidateCell(x, y, path);
        return (y * 1000 + x).ToString(CultureInfo.InvariantCulture);
    }

    private void ValidateCell(int x, int y, string path)
    {
        if (x < 0 || y < 0 || x >= 1000 || y >= 1000 || map.GetTile(x, y) == null)
            throw new ScriptingValidationException(path, $"Cell ({x}, {y}) does not exist on the map.");
    }

    private int ResolveHouseTypeIndex(string value, string path)
    {
        if (TryParseInt(value, out int index))
        {
            if (index == -1 || map.GetHouseTypes().Exists(houseType => houseType.Index == index))
                return index;

            throw new ScriptingValidationException(path, $"HouseType index {index} does not exist.");
        }

        HouseType houseTypeByName = map.GetHouseTypes().Find(houseType =>
            string.Equals(houseType.ININame, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        return houseTypeByName?.Index ?? throw new ScriptingValidationException(path, $"HouseType '{value}' does not exist.");
    }

    private int ResolveHouseIndex(string value, string path)
    {
        if (TryParseInt(value, out int index))
        {
            if (index >= 0 && index < map.GetHouses().Count)
                return index;
            throw new ScriptingValidationException(path, $"House index {index} does not exist.");
        }

        int houseIndex = map.GetHouses().FindIndex(house =>
            string.Equals(house.ININame, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (houseIndex < 0)
            throw new ScriptingValidationException(path, $"House '{value}' does not exist.");
        return houseIndex;
    }

    private string ResolveTechnoName(string value, string path)
    {
        string name = RequireText(value, path);
        TechnoType technoType = map.GetAllTechnoTypes().Find(type =>
            string.Equals(type.ININame, name, StringComparison.OrdinalIgnoreCase));
        return technoType?.ININame ?? throw new ScriptingValidationException(path, $"Techno type '{value}' does not exist in the loaded rules.");
    }

    private static string ResolveTypeName(System.Collections.Generic.IEnumerable<string> names, string value, string typeName, string path)
    {
        string name = RequireText(value, path);
        string resolved = names.FirstOrDefault(candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));
        return resolved ?? throw new ScriptingValidationException(path, $"{typeName} '{value}' does not exist in the loaded rules.");
    }

    private static string ResolveTypeIndex(string[] names, string value, string typeName, string path)
    {
        if (TryParseInt(value, out int index))
        {
            if (index >= 0 && index < names.Length)
                return index.ToString(CultureInfo.InvariantCulture);
            throw new ScriptingValidationException(path, $"{typeName} index {index} does not exist in the loaded rules.");
        }

        int nameIndex = Array.FindIndex(names, name => string.Equals(name, value?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (nameIndex < 0)
            throw new ScriptingValidationException(path, $"{typeName} '{value}' does not exist in the loaded rules.");
        return nameIndex.ToString(CultureInfo.InvariantCulture);
    }

    private string EncodeBuildingWithProperty(string value, string path)
    {
        if (TryParseInt(value, out int rawValue))
        {
            int rawPropertyOffset = rawValue switch
            {
                < 0x10000 => 0x00000,
                < 0x20000 => 0x10000,
                < 0x30000 => 0x20000,
                < 0x40000 => 0x30000,
                _ => -1
            };
            int rawBuildingIndex = rawValue - rawPropertyOffset;
            if (rawPropertyOffset < 0 || rawBuildingIndex < 0 || rawBuildingIndex >= map.Rules.BuildingTypes.Count)
                throw new ScriptingValidationException(path,
                    $"BuildingWithProperty raw value {rawValue} does not identify a loaded building and supported property.");
            return rawValue.ToString(CultureInfo.InvariantCulture);
        }

        string text = RequireText(value, path);
        string[] parts = text.Split(':');
        if (parts.Length != 2)
            throw new ScriptingValidationException(path,
                "BuildingWithProperty must be a raw integer or 'BuildingININame:LeastThreat|HighestThreat|Nearest|Farthest'.");

        int buildingIndex = map.Rules.BuildingTypes.FindIndex(type =>
            string.Equals(type.ININame, parts[0].Trim(), StringComparison.OrdinalIgnoreCase));
        if (buildingIndex < 0)
            throw new ScriptingValidationException(path, $"Building '{parts[0].Trim()}' does not exist in the loaded rules.");

        int propertyOffset = parts[1].Trim().ToLowerInvariant() switch
        {
            "leastthreat" => 0x00000,
            "highestthreat" => 0x10000,
            "nearest" => 0x20000,
            "farthest" => 0x30000,
            _ => throw new ScriptingValidationException(path, $"Unknown building property '{parts[1].Trim()}'.")
        };

        return (buildingIndex + propertyOffset).ToString(CultureInfo.InvariantCulture);
    }

    private static string RequireCommaSafeValue(string value, string path)
    {
        string text = RequireText(value, path);
        if (text.Contains(','))
            throw new ScriptingValidationException(path, "Trigger parameter values cannot contain commas.");
        return text;
    }

    private static int RequireInteger(string value, string path)
    {
        if (!TryParseInt(value, out int parsedValue))
            throw new ScriptingValidationException(path, $"'{value}' is not an integer or an option beginning with an integer value.");
        return parsedValue;
    }

    private static bool TryParseInt(string value, out int parsedValue)
    {
        parsedValue = 0;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string text = value.Trim();
        int endIndex = text.Length > 0 && (text[0] == '-' || text[0] == '+') ? 1 : 0;
        int digitStartIndex = endIndex;
        while (endIndex < text.Length && char.IsDigit(text[endIndex]))
            endIndex++;
        if (endIndex == digitStartIndex)
            return false;

        string numberText = text.Substring(0, endIndex);
        return int.TryParse(numberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsedValue);
    }

    private static string RequireText(string value, string path)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ScriptingValidationException(path, "A value is required.");
        string text = value.Trim();
        if (text.IndexOfAny(new[] { '\r', '\n', '\0' }) >= 0)
            throw new ScriptingValidationException(path, "Parameter values must be single-line text and cannot contain NUL characters.");
        return text;
    }

    private static bool IsReferenceType(TriggerParamType parameterType)
    {
        return parameterType is TriggerParamType.LocalVariable or
            TriggerParamType.TeamType or
            TriggerParamType.Trigger or
            TriggerParamType.Tag;
    }
}

internal sealed class ScriptingValidationException : Exception
{
    public ScriptingValidationException(string path, string message)
        : base(message)
    {
        Path = path;
    }

    public string Path { get; }
}
