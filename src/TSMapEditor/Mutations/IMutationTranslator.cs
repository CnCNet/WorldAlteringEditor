using TSMapEditor.GameMath;
using TSMapEditor.Models;
using System.Text.RegularExpressions;
using TSMapEditor.UI.Sidebar;
using TSMapEditor.Translations;

namespace TSMapEditor.Mutations
{
    /// <summary>
    /// 提供Mutation操作描述的翻译功能
    /// </summary>
    public static class MutationTranslator
    {
        /// <summary>
        /// 将操作描述翻译为指定语言
        /// </summary>
        /// <param name="displayString">原始描述文本</param>
        /// <param name="isChinese">是否翻译为中文</param>
        /// <returns>翻译后的描述文本</returns>
        public static string Translate(string displayString, bool isChinese)
        {
            if (!isChinese)
                return displayString;
                
            // 匹配格式: Place 'UnitName' at X, Y
            // 这种格式用于放置单位、载具、建筑等
            var placeUnitRegex = new Regex(@"Place '([^']+)' at (\d+, \d+)");
            var match = placeUnitRegex.Match(displayString);
            if (match.Success)
            {
                string unitName = match.Groups[1].Value;
                string coords = match.Groups[2].Value;
                
                // 使用游戏现有的翻译系统获取单位中文名称
                string translatedUnitName = TranslateUnitName(unitName);
                return $"放置{translatedUnitName}，位置{coords}";
            }

            // 最常见的格式: Place terrain tile of TileSet.LAT Dark Grass at 20, 100 with a brush size of 1x1
            // 简化匹配，只使用开头和结尾作为锚点
            if (displayString.StartsWith("Place terrain tile of ") && displayString.Contains(" at ") && displayString.Contains(" with a brush size of "))
            {
                int atIndex = displayString.IndexOf(" at ");
                int withIndex = displayString.IndexOf(" with a brush size of ");
                
                if (atIndex > 0 && withIndex > atIndex)
                {
                    string tileType = displayString.Substring("Place terrain tile of ".Length, atIndex - "Place terrain tile of ".Length);
                    string position = displayString.Substring(atIndex + " at ".Length, withIndex - atIndex - " at ".Length);
                    string brushSize = displayString.Substring(withIndex + " with a brush size of ".Length);
                    
                    return $"放置地形图块，类型{tileType}，位置{position}，笔刷大小{brushSize}";
                }
            }

            // Place structure of type $Structure at $Position owned by $House
            var placeStructureRegex = new Regex(@"Place structure of type ([^ ]+) at (\d+, \d+) owned by (.+)");
            match = placeStructureRegex.Match(displayString);
            if (match.Success)
            {
                string type = match.Groups[1].Value;
                string coords = match.Groups[2].Value;
                string house = match.Groups[3].Value;
                
                // 使用游戏现有的翻译系统获取建筑中文名称
                string translatedType = TranslateUnitName(type);
                return $"放置建筑{translatedType}，位置{coords}，所属{house}";
            }

            // Place infantry of type $Type at $Position owned by $House
            var placeInfantryRegex = new Regex(@"Place infantry of type ([^ ]+) at (\d+, \d+) owned by (.+)");
            match = placeInfantryRegex.Match(displayString);
            if (match.Success)
            {
                string type = match.Groups[1].Value;
                string coords = match.Groups[2].Value;
                string house = match.Groups[3].Value;
                
                // 使用游戏现有的翻译系统获取步兵中文名称
                string translatedType = TranslateUnitName(type);
                return $"放置步兵{translatedType}，位置{coords}，所属{house}";
            }

            // Place vehicle of type $Type at $Position owned by $House
            var placeVehicleRegex = new Regex(@"Place vehicle of type ([^ ]+) at (\d+, \d+) owned by (.+)");
            match = placeVehicleRegex.Match(displayString);
            if (match.Success)
            {
                string type = match.Groups[1].Value;
                string coords = match.Groups[2].Value;
                string house = match.Groups[3].Value;
                
                // 使用游戏现有的翻译系统获取载具中文名称
                string translatedType = TranslateUnitName(type);
                return $"放置载具{translatedType}，位置{coords}，所属{house}";
            }

            // Place overlay of type $Type at $Position
            var placeOverlayRegex = new Regex(@"Place overlay of type ([^ ]+) at (\d+, \d+)");
            match = placeOverlayRegex.Match(displayString);
            if (match.Success)
            {
                string type = match.Groups[1].Value;
                string coords = match.Groups[2].Value;
                
                // 使用覆盖层名称翻译字典
                var connectedOverlayMap = OverlayListPanel.GetConnectedOverlayChineseNameMap();
                if (connectedOverlayMap.TryGetValue(type, out string translatedType))
                {
                    return $"放置覆盖层{translatedType}，位置{coords}";
                }
                return $"放置覆盖层{type}，位置{coords}";
            }

            // Place smudge of type $Type at $Position
            var placeSmudgeRegex = new Regex(@"Place smudge of type ([^ ]+) at (\d+, \d+)");
            match = placeSmudgeRegex.Match(displayString);
            if (match.Success)
            {
                string type = match.Groups[1].Value;
                string coords = match.Groups[2].Value;
                return $"放置污渍{type}，位置{coords}";
            }
            
            // Place smudge collection $Type at $Position
            var placeSmudgeCollectionRegex = new Regex(@"Place smudge collection ([^ ]+) at (\d+, \d+)");
            match = placeSmudgeCollectionRegex.Match(displayString);
            if (match.Success)
            {
                string type = match.Groups[1].Value;
                string coords = match.Groups[2].Value;
                
                // 特殊处理RandomCrater
                if (type == "Random Crater" || type == "RandomCrater")
                {
                    return $"放置污渍集合随机陨石坑，位置{coords}";
                }
                
                return $"放置污渍集合{type}，位置{coords}";
            }

            // Delete object of type $Type at $Position
            var deleteObjectRegex = new Regex(@"Delete object of type ([^ ]+) at (\d+, \d+)");
            match = deleteObjectRegex.Match(displayString);
            if (match.Success)
            {
                string type = match.Groups[1].Value;
                string coords = match.Groups[2].Value;
                
                // 使用游戏现有的翻译系统获取对象中文名称
                string translatedType = TranslateUnitName(type);
                return $"删除对象{translatedType}，位置{coords}";
            }

            // 如果没有匹配的模式，返回原文
            return displayString;
        }
        
        /// <summary>
        /// 尝试从游戏已有的翻译对照表中获取单位名称的中文翻译
        /// </summary>
        /// <param name="unitName">单位名称</param>
        /// <returns>翻译后的单位名称</returns>
        private static string TranslateUnitName(string unitName)
        {
            System.Console.WriteLine($"[MutationTranslator] 尝试翻译单位名称: {unitName}");
            
            // 尝试从ObjectListPanel中的TechnoChineseNameMap获取中文名称
            if (ObjectListPanel.TryGetTechnoChineseName(unitName, out string translatedName))
            {
                System.Console.WriteLine($"[MutationTranslator] 找到翻译: {translatedName}");
                return translatedName;
            }
            
            // 尝试从外部INI翻译文件中获取英文展示名到中文的直接映射
            if (IniTranslationManager.TryGetUnitDisplayNameTranslation(unitName, out translatedName))
            {
                System.Console.WriteLine($"[MutationTranslator] 从翻译文件中找到翻译: {translatedName}");
                return translatedName;
            }
            
            // 如果没有找到翻译，返回原名称
            System.Console.WriteLine($"[MutationTranslator] 未找到任何翻译，将使用原名: {unitName}");
            return unitName;
        }
    }
} 