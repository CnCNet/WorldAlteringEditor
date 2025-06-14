using TSMapEditor.Models.Enums;
using TSMapEditor.UI;

namespace TSMapEditor.Extensions
{
    public static class TriggerParamTypeExtensions
    {
        public static string GetChineseName(this TriggerParamType paramType)
        {
            if (!MainMenu.IsChinese)
                return paramType.ToString();
                
            return paramType switch
            {
                TriggerParamType.Unused => "未使用",
                TriggerParamType.Unknown => "未知",
                TriggerParamType.Number => "数量",
                TriggerParamType.Float => "浮点数",
                TriggerParamType.Cell => "单元格",
                TriggerParamType.LocalVariable => "局部变量",
                TriggerParamType.TeamType => "小队类型",
                TriggerParamType.Techno => "科技",
                TriggerParamType.Building => "建筑",
                TriggerParamType.BuildingName => "建筑名称",
                TriggerParamType.BuildingWithProperty => "带属性的建筑",
                TriggerParamType.Aircraft => "飞行器",
                TriggerParamType.Infantry => "步兵",
                TriggerParamType.Unit => "单位",
                TriggerParamType.Movie => "电影",
                TriggerParamType.Text => "文本",
                TriggerParamType.Tag => "标签",
                TriggerParamType.Trigger => "触发器",
                TriggerParamType.Boolean => "布尔值",
                TriggerParamType.Sound => "声音",
                TriggerParamType.Theme => "主题",
                TriggerParamType.Speech => "语音",
                TriggerParamType.SuperWeapon => "超级武器",
                TriggerParamType.Animation => "动画",
                TriggerParamType.ParticleSystem => "粒子系统",
                TriggerParamType.Waypoint => "路径点",
                TriggerParamType.WaypointZZ => "路径点ZZ",
                TriggerParamType.String => "字符串",
                TriggerParamType.GlobalVariable => "全局变量",
                TriggerParamType.HouseType => "势力类型",
                TriggerParamType.House => "势力",
                TriggerParamType.Quarry => "采矿场",
                TriggerParamType.Weapon => "武器",
                TriggerParamType.SpotlightBehaviour => "聚光灯行为",
                TriggerParamType.RadarEvent => "雷达事件",
                TriggerParamType.VoxelAnim => "体素动画",
                TriggerParamType.StringTableEntry => "字符串表项",
               
                _ => paramType.ToString(),
            };
        }
        
        /// <summary>
        /// 获取参数类型的本地化名称，如果是NameOverride参数，也提供翻译
        /// </summary>
        public static string GetLocalizedParamName(string nameOverride, TriggerParamType paramType)
        {
            // 如果不是中文模式，直接使用原始名称
            if (!MainMenu.IsChinese)
            {
                return !string.IsNullOrEmpty(nameOverride) ? nameOverride : paramType.ToString();
            }
            
            // 如果有nameOverride，优先处理特定命名的参数
            if (!string.IsNullOrEmpty(nameOverride))
            {
                // 翻译特定的参数名称
                switch (nameOverride)
                {
                    case "Left": return "左侧";
                    case "Top": return "顶部";
                    case "Width": return "宽度";
                    case "Height": return "高度";
                    case "Speech": return "语音";
                    case "Number": return "数量";
                    default: return nameOverride;
                }
            }
            
            // 否则使用枚举值的翻译
            return paramType.GetChineseName();
        }
    }
} 