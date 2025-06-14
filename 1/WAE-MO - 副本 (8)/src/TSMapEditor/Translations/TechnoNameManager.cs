using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rampastring.Tools;

namespace TSMapEditor.Translations
{
    /// <summary>
    /// 管理所有游戏对象（建筑、单位、载具等）的名称翻译
    /// </summary>
    public static class TechnoNameManager
    {
        // 各种对象类型的翻译字典
        private static Dictionary<string, string> buildingNameMap = new Dictionary<string, string>();
        private static Dictionary<string, string> vehicleNameMap = new Dictionary<string, string>();
        private static Dictionary<string, string> infantryNameMap = new Dictionary<string, string>();
        private static Dictionary<string, string> terrainNameMap = new Dictionary<string, string>();
        private static Dictionary<string, string> smudgeNameMap = new Dictionary<string, string>();
        private static Dictionary<string, string> overlayNameMap = new Dictionary<string, string>();
        
        /// <summary>
        /// 初始化名称翻译管理器
        /// </summary>
        public static void Initialize()
        {
            try
            {
                LoadTranslations();
                Console.WriteLine("[TechnoNameManager] 技术对象名称翻译文件加载成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TechnoNameManager] 加载技术对象名称翻译文件时出错: {ex.Message}");
                // 出错时使用空字典，不影响程序运行
                InitEmptyDictionaries();
            }
        }
        
        /// <summary>
        /// 初始化空字典
        /// </summary>
        private static void InitEmptyDictionaries()
        {
            buildingNameMap = new Dictionary<string, string>();
            vehicleNameMap = new Dictionary<string, string>();
            infantryNameMap = new Dictionary<string, string>();
            terrainNameMap = new Dictionary<string, string>();
            smudgeNameMap = new Dictionary<string, string>();
            overlayNameMap = new Dictionary<string, string>();
        }
        
        /// <summary>
        /// 加载翻译文件
        /// </summary>
        private static void LoadTranslations()
        {
            // 获取当前游戏版本
            string gameVersion = "Yuri's Revenge";
            try
            {
                gameVersion = TSMapEditor.Settings.UserSettings.Instance?.GameVersion?.GetValue() ?? "Yuri's Revenge";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TechnoNameManager] 获取游戏版本时出错: {ex.Message}，使用默认值 Yuri's Revenge");
            }
            
            // 确定应该使用哪个翻译目录
            string translationDir = gameVersion == "MentalOmegaClient" ? "MO" : "YR";
            
            // 尝试加载游戏版本特定的翻译文件
            string versionSpecificPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", translationDir, "TechnoNames.ini");
            
            // 如果版本特定的翻译文件不存在，则使用通用翻译文件
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", "TechnoNames.ini");
            
            string translationsPath = File.Exists(versionSpecificPath) ? versionSpecificPath : defaultPath;
            
            // 输出诊断信息
            Console.WriteLine($"[TechnoNameManager] 当前游戏版本: {gameVersion}");
            Console.WriteLine($"[TechnoNameManager] 当前运行目录: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"[TechnoNameManager] 翻译文件路径: {translationsPath}");
            Console.WriteLine($"[TechnoNameManager] 翻译文件是否存在: {File.Exists(translationsPath)}");
            
            // 如果翻译文件不存在，使用空字典
            if (!File.Exists(translationsPath))
            {
                InitEmptyDictionaries();
                return;
            }
            
            // 读取INI文件
            IniFile iniFile = new IniFile(translationsPath);
            
            // 输出所有找到的节
            Console.WriteLine($"[TechnoNameManager] INI文件中的节: {string.Join(", ", iniFile.GetSections().Select(s => s))}");
            
            // 读取建筑名称
            LoadSectionIntoDict(iniFile, "Buildings", buildingNameMap);
            Console.WriteLine($"[TechnoNameManager] 加载的建筑翻译数量: {buildingNameMap.Count}");
            
            // 读取载具名称
            LoadSectionIntoDict(iniFile, "Vehicles", vehicleNameMap);
            Console.WriteLine($"[TechnoNameManager] 加载的载具翻译数量: {vehicleNameMap.Count}");
            
            // 读取步兵名称
            LoadSectionIntoDict(iniFile, "Infantry", infantryNameMap);
            Console.WriteLine($"[TechnoNameManager] 加载的步兵翻译数量: {infantryNameMap.Count}");
            
            // 读取地形物品名称
            LoadSectionIntoDict(iniFile, "Terrains", terrainNameMap);
            
            // 读取污渍名称
            LoadSectionIntoDict(iniFile, "Smudges", smudgeNameMap);
            
            // 读取覆盖层名称
            LoadSectionIntoDict(iniFile, "Overlays", overlayNameMap);
        }
        
        /// <summary>
        /// 将INI节中的键值对加载到字典中
        /// </summary>
        private static void LoadSectionIntoDict(IniFile iniFile, string sectionName, Dictionary<string, string> dict)
        {
            var section = iniFile.GetSection(sectionName);
            if (section != null)
            {
                foreach (var kvp in section.Keys)
                {
                    dict[kvp.Key] = kvp.Value;
                }
            }
        }
        
        /// <summary>
        /// 尝试获取建筑名称的翻译
        /// </summary>
        public static bool TryGetBuildingName(string iniName, out string translation)
        {
            return buildingNameMap.TryGetValue(iniName, out translation);
        }
        
        /// <summary>
        /// 尝试获取载具名称的翻译
        /// </summary>
        public static bool TryGetVehicleName(string iniName, out string translation)
        {
            return vehicleNameMap.TryGetValue(iniName, out translation);
        }
        
        /// <summary>
        /// 尝试获取步兵名称的翻译
        /// </summary>
        public static bool TryGetInfantryName(string iniName, out string translation)
        {
            return infantryNameMap.TryGetValue(iniName, out translation);
        }
        
        /// <summary>
        /// 尝试获取地形物品名称的翻译
        /// </summary>
        public static bool TryGetTerrainName(string iniName, out string translation)
        {
            return terrainNameMap.TryGetValue(iniName, out translation);
        }
        
        /// <summary>
        /// 尝试获取污渍名称的翻译
        /// </summary>
        public static bool TryGetSmudgeName(string iniName, out string translation)
        {
            return smudgeNameMap.TryGetValue(iniName, out translation);
        }
        
        /// <summary>
        /// 尝试获取覆盖层名称的翻译
        /// </summary>
        public static bool TryGetOverlayName(string iniName, out string translation)
        {
            return overlayNameMap.TryGetValue(iniName, out translation);
        }
        
        /// <summary>
        /// 获取单位（建筑、载具、步兵）名称的翻译，自动根据INI名称前缀判断类型
        /// </summary>
        public static bool TryGetTechnoName(string iniName, out string translation)
        {
            // 尝试作为建筑查询
            if (TryGetBuildingName(iniName, out translation))
                return true;
                
            // 尝试作为载具查询
            if (TryGetVehicleName(iniName, out translation))
                return true;
                
            // 尝试作为步兵查询
            if (TryGetInfantryName(iniName, out translation))
                return true;
                
            // 未找到翻译
            translation = null;
            return false;
        }
        
        /// <summary>
        /// 添加技术对象名称翻译
        /// </summary>
        public static void AddTechnoName(string sectionName, string iniName, string translation)
        {
            if (string.IsNullOrEmpty(iniName) || string.IsNullOrEmpty(translation))
                return;
                
            // 根据section类型更新相应的字典
            switch (sectionName.ToLower())
            {
                case "buildings":
                    buildingNameMap[iniName] = translation;
                    break;
                case "vehicles":
                    vehicleNameMap[iniName] = translation;
                    break;
                case "infantry":
                    infantryNameMap[iniName] = translation;
                    break;
                case "terrains":
                    terrainNameMap[iniName] = translation;
                    break;
                case "smudges":
                    smudgeNameMap[iniName] = translation;
                    break;
                case "overlays":
                    overlayNameMap[iniName] = translation;
                    break;
                default:
                    return; // 未知的section类型
            }
            
            // 获取当前游戏版本
            string gameVersion = "Yuri's Revenge";
            try
            {
                gameVersion = TSMapEditor.Settings.UserSettings.Instance?.GameVersion?.GetValue() ?? "Yuri's Revenge";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TechnoNameManager] 获取游戏版本时出错: {ex.Message}，使用默认值 Yuri's Revenge");
            }
            
            // 确定应该使用哪个翻译目录
            string translationDir = gameVersion == "MentalOmegaClient" ? "MO" : "YR";
            
            // 构建版本特定的翻译文件路径
            string translationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", translationDir, "TechnoNames.ini");
            
            // 如果版本特定目录不存在，创建它
            Directory.CreateDirectory(Path.GetDirectoryName(translationsPath));
            
            // 如果文件不存在，可能需要从通用文件复制过来或创建新文件
            if (!File.Exists(translationsPath))
            {
                string generalPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", "TechnoNames.ini");
                if (File.Exists(generalPath))
                {
                    File.Copy(generalPath, translationsPath);
                }
            }
            
            // 现在更新INI文件
            IniFile iniFile = new IniFile(translationsPath);
            
            var section = iniFile.GetSection(sectionName);
            if (section == null)
            {
                iniFile.AddSection(sectionName);
                section = iniFile.GetSection(sectionName);
            }
            
            section.SetStringValue(iniName, translation);
            iniFile.WriteIniFile();
            
            Console.WriteLine($"[TechnoNameManager] 已将翻译 '{iniName}={translation}' 添加到文件: {translationsPath}");
        }
    }
} 