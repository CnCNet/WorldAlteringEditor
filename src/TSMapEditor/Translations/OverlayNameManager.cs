using System;
using System.Collections.Generic;
using System.IO;
using Rampastring.Tools;

namespace TSMapEditor.Translations
{
    /// <summary>
    /// 管理覆盖物名称的翻译
    /// </summary>
    public static class OverlayNameManager
    {
        private static Dictionary<string, string> connectedOverlayNameMap = new Dictionary<string, string>();
        private static Dictionary<string, string> overlayCollectionNameMap = new Dictionary<string, string>();
        private static Dictionary<string, string> overlayNameMap = new Dictionary<string, string>();
        
        /// <summary>
        /// 初始化覆盖物名称翻译管理器
        /// </summary>
        public static void Initialize()
        {
            try
            {
                LoadTranslations();
                Console.WriteLine("[OverlayNameManager] 覆盖物名称翻译文件加载成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OverlayNameManager] 加载覆盖物名称翻译文件时出错: {ex.Message}");
                // 加载失败时使用空字典，不影响程序运行
                connectedOverlayNameMap = new Dictionary<string, string>();
                overlayCollectionNameMap = new Dictionary<string, string>();
                overlayNameMap = new Dictionary<string, string>();
            }
        }
        
        /// <summary>
        /// 加载翻译文件
        /// </summary>
        private static void LoadTranslations()
        {
            string translationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", "OverlayNames.ini");
            
            // 输出诊断信息
            Console.WriteLine($"[OverlayNameManager] 当前运行目录: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"[OverlayNameManager] 翻译文件路径: {translationsPath}");
            Console.WriteLine($"[OverlayNameManager] 翻译文件是否存在: {File.Exists(translationsPath)}");
            
            // 如果翻译文件不存在，使用空字典
            if (!File.Exists(translationsPath))
            {
                connectedOverlayNameMap = new Dictionary<string, string>();
                overlayCollectionNameMap = new Dictionary<string, string>();
                overlayNameMap = new Dictionary<string, string>();
                return;
            }
            
            // 读取INI文件
            IniFile iniFile = new IniFile(translationsPath);
            
            // 输出所有找到的节
            Console.WriteLine($"[OverlayNameManager] INI文件中的节: {string.Join(", ", iniFile.GetSections())}");
            
            // 读取连接型覆盖物名称翻译
            var connectedSection = iniFile.GetSection("ConnectedOverlayTranslations");
            if (connectedSection != null)
            {
                foreach (var kvp in connectedSection.Keys)
                {
                    connectedOverlayNameMap[kvp.Key] = kvp.Value;
                }
            }
            
            // 读取覆盖物集合名称翻译
            var collectionsSection = iniFile.GetSection("OverlayCollectionTranslations");
            if (collectionsSection != null)
            {
                foreach (var kvp in collectionsSection.Keys)
                {
                    overlayCollectionNameMap[kvp.Key] = kvp.Value;
                }
            }
            
            // 读取单个覆盖物名称翻译
            var overlaySection = iniFile.GetSection("OverlayTranslations");
            if (overlaySection != null)
            {
                foreach (var kvp in overlaySection.Keys)
                {
                    overlayNameMap[kvp.Key] = kvp.Value;
                }
            }
            
            Console.WriteLine($"[OverlayNameManager] 加载的连接型覆盖物翻译数量: {connectedOverlayNameMap.Count}");
            Console.WriteLine($"[OverlayNameManager] 加载的覆盖物集合翻译数量: {overlayCollectionNameMap.Count}");
            Console.WriteLine($"[OverlayNameManager] 加载的单个覆盖物翻译数量: {overlayNameMap.Count}");
        }
        
        /// <summary>
        /// 尝试获取连接型覆盖物的翻译
        /// </summary>
        /// <param name="overlayName">覆盖物名称</param>
        /// <param name="translation">输出的中文翻译</param>
        /// <returns>是否找到翻译</returns>
        public static bool TryGetConnectedOverlayTranslation(string overlayName, out string translation)
        {
            return connectedOverlayNameMap.TryGetValue(overlayName, out translation);
        }
        
        /// <summary>
        /// 尝试获取覆盖物集合的翻译
        /// </summary>
        /// <param name="collectionName">集合名称</param>
        /// <param name="translation">输出的中文翻译</param>
        /// <returns>是否找到翻译</returns>
        public static bool TryGetOverlayCollectionTranslation(string collectionName, out string translation)
        {
            return overlayCollectionNameMap.TryGetValue(collectionName, out translation);
        }
        
        /// <summary>
        /// 尝试获取单个覆盖物的翻译
        /// </summary>
        /// <param name="iniName">覆盖物INI名称</param>
        /// <param name="translation">输出的中文翻译</param>
        /// <returns>是否找到翻译</returns>
        public static bool TryGetOverlayTranslation(string iniName, out string translation)
        {
            return overlayNameMap.TryGetValue(iniName, out translation);
        }
    }
} 