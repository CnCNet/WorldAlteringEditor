using System;
using System.Collections.Generic;
using System.IO;
using Rampastring.Tools;

namespace TSMapEditor.Translations
{
    /// <summary>
    /// 使用INI文件格式管理翻译内容
    /// </summary>
    public static class IniTranslationManager
    {
        private static Dictionary<string, string> unitDisplayNameTranslations = new Dictionary<string, string>();
        
        /// <summary>
        /// 初始化翻译管理器，加载翻译文件
        /// </summary>
        public static void Initialize()
        {
            try
            {
                LoadTranslations();
                Console.WriteLine("[IniTranslationManager] 翻译文件加载成功");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IniTranslationManager] 加载翻译文件时出错: {ex.Message}");
                // 加载失败时使用空字典，不影响程序运行
                unitDisplayNameTranslations = new Dictionary<string, string>();
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
                Console.WriteLine($"[IniTranslationManager] 获取游戏版本时出错: {ex.Message}，使用默认值 Yuri's Revenge");
            }
            
            // 确定应该使用哪个翻译目录
            string translationDir = gameVersion == "MentalOmegaClient" ? "MO" : "YR";
            
            // 尝试加载游戏版本特定的翻译文件
            string versionSpecificPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", translationDir, "UnitDisplayNames.ini");
            
            // 如果版本特定的翻译文件不存在，则使用通用翻译文件
            string defaultPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", "UnitDisplayNames.ini");
            
            string translationsPath = File.Exists(versionSpecificPath) ? versionSpecificPath : defaultPath;
            
            // 输出诊断信息
            Console.WriteLine($"[IniTranslationManager] 当前游戏版本: {gameVersion}");
            Console.WriteLine($"[IniTranslationManager] 当前运行目录: {AppDomain.CurrentDomain.BaseDirectory}");
            Console.WriteLine($"[IniTranslationManager] 翻译文件路径: {translationsPath}");
            Console.WriteLine($"[IniTranslationManager] 翻译文件是否存在: {File.Exists(translationsPath)}");
            
            // 如果翻译文件不存在，创建一个初始的翻译文件
            if (!File.Exists(translationsPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(translationsPath));
                
                // 创建一个简单的INI文件
                using (StreamWriter writer = new StreamWriter(translationsPath))
                {
                    writer.WriteLine("[UnitDisplayNameTranslations]");
                    writer.WriteLine("Allied Air Force Command Headquarters=盟军空军指挥部");
                    writer.WriteLine("Field Medic=军医");
                    writer.WriteLine("Civilian Male White=白衣男性平民A");
                    writer.WriteLine("Civilian Snow Female=雪地女性平民A");
                    writer.WriteLine("Flak Trooper=防空步兵");
                    writer.WriteLine("Random Crater=随机陨石坑");
                    writer.WriteLine("Stratofortress=平流层堡垒");
                    writer.WriteLine("Norio=诺里奥");
                }
            }
            
            // 读取INI文件
            IniFile iniFile = new IniFile(translationsPath);
            
            // 读取单位名称翻译
            var section = iniFile.GetSection("UnitDisplayNameTranslations");
            if (section != null)
            {
                foreach (var kvp in section.Keys)
                {
                    unitDisplayNameTranslations[kvp.Key] = kvp.Value;
                }
            }
            
            Console.WriteLine($"[IniTranslationManager] 加载的单位翻译数量: {unitDisplayNameTranslations.Count}");
        }
        
        /// <summary>
        /// 尝试获取单位展示名称的翻译
        /// </summary>
        /// <param name="displayName">单位的英文展示名称</param>
        /// <param name="translation">输出的中文翻译</param>
        /// <returns>是否找到翻译</returns>
        public static bool TryGetUnitDisplayNameTranslation(string displayName, out string translation)
        {
            return unitDisplayNameTranslations.TryGetValue(displayName, out translation);
        }
        
        /// <summary>
        /// 向翻译文件添加新的翻译条目
        /// </summary>
        /// <param name="englishName">英文名称</param>
        /// <param name="chineseName">中文名称</param>
        public static void AddTranslation(string englishName, string chineseName)
        {
            if (string.IsNullOrEmpty(englishName) || string.IsNullOrEmpty(chineseName))
                return;
                
            unitDisplayNameTranslations[englishName] = chineseName;
            
            // 获取当前游戏版本
            string gameVersion = "Yuri's Revenge";
            try
            {
                gameVersion = TSMapEditor.Settings.UserSettings.Instance?.GameVersion?.GetValue() ?? "Yuri's Revenge";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IniTranslationManager] 获取游戏版本时出错: {ex.Message}，使用默认值 Yuri's Revenge");
            }
            
            // 确定应该使用哪个翻译目录
            string translationDir = gameVersion == "MentalOmegaClient" ? "MO" : "YR";
            
            // 尝试加载游戏版本特定的翻译文件
            string translationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", translationDir, "UnitDisplayNames.ini");
            
            // 如果版本特定的翻译文件不存在，使用通用翻译文件
            if (!File.Exists(translationsPath))
            {
                translationsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Translations", "UnitDisplayNames.ini");
            }
            
            // 更新INI文件
            IniFile iniFile = new IniFile(translationsPath);
            
            var section = iniFile.GetSection("UnitDisplayNameTranslations");
            if (section == null)
            {
                // AddSection方法不返回IniSection对象，所以需要先添加section，然后再获取它
                iniFile.AddSection("UnitDisplayNameTranslations");
                section = iniFile.GetSection("UnitDisplayNameTranslations");
            }
            
            section.SetStringValue(englishName, chineseName);
            iniFile.WriteIniFile();
        }
    }
} 