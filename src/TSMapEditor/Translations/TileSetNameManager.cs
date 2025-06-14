using System;
using System.Collections.Generic;
using System.IO;
using Rampastring.Tools;

namespace TSMapEditor.Translations
{
    /// <summary>
    /// 管理地形集名称的翻译
    /// </summary>
    public static class TileSetNameManager
    {
        private static Dictionary<string, string> tileSetNameMap = new Dictionary<string, string>();
        
        /// <summary>
        /// 初始化地形集名称翻译管理器
        /// </summary>
        public static void Initialize()
        {
            try
            {
                LoadTranslations();
                Console.WriteLine("[TileSetNameManager] 地形集名称翻译文件加载成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TileSetNameManager] 加载地形集名称翻译文件时出错: {ex.Message}");
                // 加载失败时使用空字典，不影响程序运行
                tileSetNameMap = new Dictionary<string, string>();
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
                Console.WriteLine($"[TileSetNameManager] 获取游戏版本时出错: {ex.Message}，使用默认值 Yuri's Revenge");
            }
            
            // 确定应该使用哪个翻译目录
            string translationDir = gameVersion == "MentalOmegaClient" ? "MO" : "YR";
            
            // 尝试加载游戏版本特定的翻译文件
            string versionSpecificPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", translationDir, "TileSetNames.ini");
            
            // 如果版本特定的翻译文件不存在，则使用通用翻译文件
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", "TileSetNames.ini");
            
            string translationsPath = File.Exists(versionSpecificPath) ? versionSpecificPath : defaultPath;
            
            // 输出诊断信息
            Console.WriteLine($"[TileSetNameManager] 当前游戏版本: {gameVersion}");
            Console.WriteLine($"[TileSetNameManager] 当前运行目录: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"[TileSetNameManager] 翻译文件路径: {translationsPath}");
            Console.WriteLine($"[TileSetNameManager] 翻译文件是否存在: {File.Exists(translationsPath)}");
            
            // 如果翻译文件不存在，使用空字典
            if (!File.Exists(translationsPath))
            {
                tileSetNameMap = new Dictionary<string, string>();
                return;
            }
            
            // 读取INI文件
            IniFile iniFile = new IniFile(translationsPath);
            
            // 输出所有找到的节
            Console.WriteLine($"[TileSetNameManager] INI文件中的节: {string.Join(", ", iniFile.GetSections())}");
            
            // 读取地形集名称翻译
            var section = iniFile.GetSection("TileSetTranslations");
            if (section != null)
            {
                foreach (var kvp in section.Keys)
                {
                    tileSetNameMap[kvp.Key] = kvp.Value;
                }
            }
            
            Console.WriteLine($"[TileSetNameManager] 加载的地形集翻译数量: {tileSetNameMap.Count}");
        }
        
        /// <summary>
        /// 尝试获取地形集名称的翻译
        /// </summary>
        /// <param name="tileSetName">地形集名称</param>
        /// <param name="translation">输出的中文翻译</param>
        /// <returns>是否找到翻译</returns>
        public static bool TryGetTileSetNameTranslation(string tileSetName, out string translation)
        {
            return tileSetNameMap.TryGetValue(tileSetName, out translation);
        }
    }
} 