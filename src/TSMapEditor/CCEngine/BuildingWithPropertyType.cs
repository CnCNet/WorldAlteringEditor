namespace TSMapEditor.CCEngine
{
    public enum BuildingWithPropertyType
    {
        LeastThreat = 0x0,
        HighestThreat = 0x10000,
        Nearest = 0x20000,
        Farthest = 0x30000,
        Invalid = 0x40000
    }

    public static class BuildingWithPropertyTypeExtension
    {
        public static string ToDescription(this BuildingWithPropertyType value)
        {
            return value switch
            {
                BuildingWithPropertyType.LeastThreat => Translate("BuildingWithPropertyTypeExtension.LeastThreat", "Least threat"),
                BuildingWithPropertyType.HighestThreat => Translate("BuildingWithPropertyTypeExtension.HighestThreat", "Highest threat"),
                BuildingWithPropertyType.Nearest => Translate("BuildingWithPropertyTypeExtension.Nearest", "Nearest"),
                BuildingWithPropertyType.Farthest => Translate("BuildingWithPropertyTypeExtension.Farthest", "Farthest"),
                _ => string.Empty,
            };
        }
    }
}
