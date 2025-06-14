using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;

namespace TSMapEditor.Models
{
    /// <summary>
    /// House type. For most use cases, there is one house type related to each house.
    /// However, in Yuri's Revenge, this is not necessarily the case, and multiple
    /// Houses can use one HouseType, even if it results in limited trigger functionality
    /// as most triggers refer to HouseTypes rather than Houses.
    /// </summary>
    public class HouseType : AbstractObject, INIDefined
    {
        public const double MultiplierDefaultValue = 1.0;

        public HouseType(string iniName)
        {
            ININame = iniName;
        }

        public override RTTIType WhatAmI() => RTTIType.HouseType;

        [INI(false)]
        public string ININame { get; set; }

        public string ParentCountry { get; set; }
        public string Suffix { get; set; }
        public string Prefix { get; set; }
        public string Color { get; set; }
        public string Side { get; set; }

        public bool? SmartAI { get; set; }
        public bool? Multiplay { get; set; }
        public bool? MultiplayPassive { get; set; }
        public bool? WallOwner { get; set; }

        public double? Airspeed { get; set; }
        public double? Armor { get; set; }
        public double? Cost { get; set; }
        public double? Firepower { get; set; }
        public double? Groundspeed { get; set; }
        public double? ROF { get; set; }
        public double? BuildTime { get; set; }

        public float? ArmorInfantryMult { get; set; }
        public float? ArmorUnitsMult { get; set; }
        public float? ArmorAircraftMult { get; set; }
        public float? ArmorBuildingsMult { get; set; }
        public float? ArmorDefensesMult { get; set; }

        public float? CostInfantryMult { get; set; }
        public float? CostUnitsMult { get; set; }
        public float? CostAircraftMult { get; set; }
        public float? CostBuildingsMult { get; set; }
        public float? CostDefensesMult { get; set; }

        public float? SpeedInfantryMult { get; set; }
        public float? SpeedUnitsMult { get; set; }
        public float? SpeedAircraftMult { get; set; }

        public float? BuildTimeInfantryMult { get; set; }
        public float? BuildTimeUnitsMult { get; set; }
        public float? BuildTimeAircraftMult { get; set; }
        public float? BuildTimeBuildingsMult { get; set; }
        public float? BuildTimeDefensesMult { get; set; }

        public float? IncomeMult { get; set; }

        [INI(false)] public Color XNAColor { get; set; } = Microsoft.Xna.Framework.Color.White;

        [INI(false)]
        public int Index { get; set; }

        /// <summary>
        /// If set, this is a standard HouseType that has been somehow modified in the map.
        /// </summary>
        [INI(false)]
        public bool ModifiedInMap { get; set; }

        public void ReadFromIniSection(IniSection iniSection)
        {
            ReadPropertiesFromIniSection(iniSection);
        }

        public void WriteToIniSection(IniSection iniSection)
        {
            foreach (var property in GetType().GetProperties())
            {
                if (property.PropertyType == typeof(float?))
                {
                    if ((float?)property.GetValue(this) == (float)MultiplierDefaultValue)
                        property.SetValue(this, null);
                }
                else if (property.PropertyType == typeof(double?))
                {
                    if ((double?)property.GetValue(this) == MultiplierDefaultValue)
                        property.SetValue(this, null);
                }
            }

            WritePropertiesToIniSection(iniSection);
        }

        public void EraseFromIniFile(IniFile iniFile)
        {
            if (string.IsNullOrWhiteSpace(ININame))
                return;

            ArgumentNullException.ThrowIfNull(iniFile);

            var section = iniFile.GetSection(ININame);
            if (section == null)
                return;

            ErasePropertiesFromIniSection(section);

            if (section.Keys.Count == 0)
                iniFile.RemoveSection(ININame);
        }

        public void CopyBasicPropertiesFrom(HouseType other)
        {
            Color = other.Color;
            XNAColor = other.XNAColor;
            Prefix = other.Prefix;
            Suffix = other.Suffix;
            Side = other.Side;
            Multiplay = other.Multiplay;
            MultiplayPassive = other.MultiplayPassive;
            SmartAI = other.SmartAI;
            WallOwner = other.WallOwner;
        }
    }
}
