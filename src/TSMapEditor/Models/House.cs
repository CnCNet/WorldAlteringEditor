using Microsoft.Xna.Framework;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using TSMapEditor.GameMath;

namespace TSMapEditor.Models
{
    public class BaseNode : IPositioned
    {
        public BaseNode()
        {
        }

        public BaseNode(string structureTypeName, Point2D location)
        {
            StructureTypeName = structureTypeName;
            Position = location;
        }

        public string StructureTypeName { get; set; }
        public Point2D Position { get; set; }

        public bool IsOnBridge() => false;

        public static BaseNode FromIniString(string iniString)
        {
            if (string.IsNullOrWhiteSpace(iniString))
            {
                Logger.Log($"{nameof(BaseNode)}.{nameof(FromIniString)}: null string or whitespace given as parameter");
                return null;
            }
                
            string[] parts = iniString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                Logger.Log($"{nameof(BaseNode)}.{nameof(FromIniString)}: invalid string " + iniString);
                return null;
            }

            int x = Conversions.IntFromString(parts[1], -1);
            int y = Conversions.IntFromString(parts[2], -1);
            if (x < 0 || y < 0)
            {
                Logger.Log($"{nameof(BaseNode)}.{nameof(FromIniString)}: invalid coordinates given in string " + iniString);
                return null;
            }

            return new BaseNode(parts[0], new Point2D(x, y));
        }
    }

    public class House : AbstractObject
    {
        private const int MaxBaseNodeCount = 1000;

        public override RTTIType WhatAmI() => RTTIType.House;

        public House(string iniName)
        {
            ININame = iniName;
        }

        public House(string iniName, HouseType houseType) : this(iniName)
        {
            HouseType = houseType;
        }

        [INI(false)]
        public string ININame { get; set; }
        public HouseType HouseType { get; set; }

        public int IQ { get; set; }
        public string Edge { get; set; }
        public string Color { get; set; } = "White";

        [INI(false)]
        public List<House> Allies { get; set; } = [];
        public int Credits { get; set; }

        /// <summary>
        /// The country of the house in Red Alert 2. Unused in Tiberian Sun.
        ///
        /// NOTE: This should only be used for saving and loading the map. Otherwise, use <see cref="HouseType"/>
        /// to refer to the country that the house is using.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Which HouseType this house "acts like" in Tiberian Sun. Unused in Red Alert 2.
        /// </summary>
        public int? ActsLike { get; set; }
        public int TechLevel { get; set; }
        public int PercentBuilt { get; set; }
        public bool PlayerControl { get; set; }
        public bool DefaultRepairableStructures { get; set; } = false;

        [INI(false)]
        public int ID { get; set; }

        [INI(false)]
        public Color XNAColor { get; set; } = Microsoft.Xna.Framework.Color.White;

        public List<BaseNode> BaseNodes { get; private set; } = new List<BaseNode>();

        public void ReadFromIniSection(IniSection iniSection)
        {
            ReadPropertiesFromIniSection(iniSection);

            // Read base nodes
            for (int i = 0; i < MaxBaseNodeCount; i++)
            {
                string nodeInfo = iniSection.GetStringValue(i.ToString("D3"), null);
                if (nodeInfo == null)
                    return;

                var baseNode = BaseNode.FromIniString(nodeInfo);
                if (baseNode != null)
                    BaseNodes.Add(baseNode);
            }
        }

        public void WriteToIniSection(IniSection iniSection)
        {
            WritePropertiesToIniSection(iniSection);
            iniSection.SetListValue("Allies", Allies.Select(alliedHouse => alliedHouse.ININame).ToList(), ',');

            // Write base nodes
            // Format: Index=BuildingTypeName,X,Y
            // Index is from 000 to 999

            iniSection.SetIntValue("NodeCount", BaseNodes.Count);
            for (int i = 0; i < BaseNodes.Count; i++)
            {
                var node = BaseNodes[i];

                iniSection.SetStringValue(i.ToString("D3"), $"{node.StructureTypeName},{node.Position.X},{node.Position.Y}");
            }

            // Erase potential removed nodes
            for (int i = BaseNodes.Count; i < MaxBaseNodeCount; i++)
            {
                iniSection.RemoveKey(i.ToString("D3"));
            }
        }

        /// <summary>
        /// Sometimes mappers might, unfortunately, give houses the same IDs
        /// as other heap object types, like unit types.
        /// Simply removing the house's section would then also erase the unit type's INI data.
        ///
        /// This function removes only the house's data from the INI file.
        /// The relevant section is removed only if it has no keys left after house data has been erased.
        /// </summary>
        /// <param name="iniFile">The INI file to remove this house's data from.</param>
        public void EraseFromIniFile(IniFile iniFile)
        {
            if (string.IsNullOrWhiteSpace(ININame))
                return;

            ArgumentNullException.ThrowIfNull(iniFile);

            var section = iniFile.GetSection(ININame);
            if (section == null)
                return;

            section.RemoveKey("NodeCount");
            for (int i = 0; i < MaxBaseNodeCount; i++)
            {
                section.RemoveKey(i.ToString("D3"));
            }

            ErasePropertiesFromIniSection(section);

            if (section.Keys.Count == 0)
                iniFile.RemoveSection(ININame);
        }

        public override AbstractObject Clone()
        {
            var clone = (House)base.Clone();
            clone.Allies = new List<House>(Allies);
            clone.Allies.Insert(0, clone);
            clone.BaseNodes = new List<BaseNode>();
            return clone;
        }
    }
}
