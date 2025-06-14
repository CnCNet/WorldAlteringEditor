using System;
using System.Collections.Generic;
using System.IO;
using Rampastring.Tools;

namespace TSMapEditor.Translations
{
    /// <summary>
    /// 管理地形对象名称的翻译
    /// </summary>
    public static class TerrainObjectNameManager
    {
        private static Dictionary<string, string> terrainObjectNameMap = new Dictionary<string, string>();
        private static Dictionary<string, string> terrainObjectCollectionNameMap = new Dictionary<string, string>();

        /// <summary>
        /// 初始化地形对象名称翻译管理器
        /// </summary>
        public static void Initialize()
        {
            try
            {
                LoadTranslations();
                Console.WriteLine("[TerrainObjectNameManager] 地形对象名称翻译文件加载成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TerrainObjectNameManager] 加载地形对象名称翻译文件时出错: {ex.Message}");
                // 加载失败时使用空字典，不影响程序运行
                terrainObjectNameMap = new Dictionary<string, string>();
                terrainObjectCollectionNameMap = new Dictionary<string, string>();
            }
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
                Console.WriteLine($"[TerrainObjectNameManager] 获取游戏版本时出错: {ex.Message}，使用默认值 Yuri's Revenge");
            }
            
            // 确定应该使用哪个翻译目录
            string translationDir = gameVersion == "MentalOmegaClient" ? "MO" : "YR";
            
            // 尝试加载游戏版本特定的翻译文件
            string versionSpecificPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", translationDir, "TerrainObjectNames.ini");
            
            // 如果版本特定的翻译文件不存在，则使用通用翻译文件
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", "TerrainObjectNames.ini");
            
            string translationsPath = File.Exists(versionSpecificPath) ? versionSpecificPath : defaultPath;
            
            // 输出诊断信息
            Console.WriteLine($"[TerrainObjectNameManager] 当前游戏版本: {gameVersion}");
            Console.WriteLine($"[TerrainObjectNameManager] 当前运行目录: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"[TerrainObjectNameManager] 翻译文件路径: {translationsPath}");
            Console.WriteLine($"[TerrainObjectNameManager] 翻译文件是否存在: {File.Exists(translationsPath)}");
            
            // 如果翻译文件不存在，使用空字典
            if (!File.Exists(translationsPath))
            {
                terrainObjectNameMap = new Dictionary<string, string>();
                terrainObjectCollectionNameMap = new Dictionary<string, string>();
                return;
            }
            
            // 读取INI文件
            IniFile iniFile = new IniFile(translationsPath);
            
            // 输出所有找到的节
            Console.WriteLine($"[TerrainObjectNameManager] INI文件中的节: {string.Join(", ", iniFile.GetSections())}");
            
            // 读取地形对象名称翻译
            var objectsSection = iniFile.GetSection("TerrainObjectTranslations");
            if (objectsSection != null)
            {
                foreach (var kvp in objectsSection.Keys)
                {
                    terrainObjectNameMap[kvp.Key] = kvp.Value;
                }
            }
            
            // 读取地形对象集合名称翻译
            var collectionsSection = iniFile.GetSection("TerrainObjectCollectionTranslations");
            if (collectionsSection != null)
            {
                foreach (var kvp in collectionsSection.Keys)
                {
                    terrainObjectCollectionNameMap[kvp.Key] = kvp.Value;
                }
            }
            
            Console.WriteLine($"[TerrainObjectNameManager] 加载的地形对象翻译数量: {terrainObjectNameMap.Count}");
            Console.WriteLine($"[TerrainObjectNameManager] 加载的地形对象集合翻译数量: {terrainObjectCollectionNameMap.Count}");
        }

        /// <summary>
        /// 尝试获取地形对象的翻译
        /// </summary>
        /// <param name="objectName">地形对象名称</param>
        /// <param name="translation">输出的中文翻译</param>
        /// <returns>是否找到翻译</returns>
        public static bool TryGetTerrainObjectTranslation(string objectName, out string translation)
        {
            return terrainObjectNameMap.TryGetValue(objectName, out translation);
        }
        
        /// <summary>
        /// 尝试获取地形对象集合的翻译
        /// </summary>
        /// <param name="collectionName">集合名称</param>
        /// <param name="translation">输出的中文翻译</param>
        /// <returns>是否找到翻译</returns>
        public static bool TryGetTerrainObjectCollectionTranslation(string collectionName, out string translation)
        {
            return terrainObjectCollectionNameMap.TryGetValue(collectionName, out translation);
        }
    }
} 