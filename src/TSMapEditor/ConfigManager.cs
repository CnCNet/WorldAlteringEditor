using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using TSMapEditor.Settings;

namespace TSMapEditor
{
    public static class ConfigManager
    {
        private static Dictionary<string, IniFile> configCache = new Dictionary<string, IniFile>();

        /// <summary>
        /// 清除所有已缓存的配置文件
        /// </summary>
        public static void ClearCache()
        {
            configCache.Clear();
            Rampastring.Tools.Logger.Log("配置缓存已清除");
        }

        /// <summary>
        /// 根据当前选择的游戏版本加载配置文件
        /// </summary>
        /// <param name="path">配置文件相对路径</param>
        /// <returns>加载的INI配置文件</returns>
        public static IniFile GetConfig(string path)
        {
            string cacheKey = path;
            
            // 检查缓存
            if (configCache.TryGetValue(cacheKey, out IniFile config))
                return config;
            
            // 获取当前游戏版本
            string gameVersion = "Yuri's Revenge";
            try
            {
                gameVersion = UserSettings.Instance?.GameVersion?.GetValue() ?? "Yuri's Revenge";
            }
            catch (Exception ex)
            {
                Rampastring.Tools.Logger.Log($"获取游戏版本时出错: {ex.Message}，使用默认值 Yuri's Revenge");
            }
            
            // 确定应该使用哪个配置目录
            string configDir = gameVersion == "MentalOmegaClient" ? "MO" : "yr";
            
            // 优先检查对应游戏版本的配置
            string versionSpecificPath = Path.Combine(Environment.CurrentDirectory, "Config", configDir, path);
            if (File.Exists(versionSpecificPath))
            {
                Rampastring.Tools.Logger.Log($"从 {configDir} 加载配置: {path}");
                config = new IniFile(versionSpecificPath);
                configCache[cacheKey] = config;
                return config;
            }
            
            // 其次检查用户自定义配置
            string customPath = Path.Combine(Environment.CurrentDirectory, "Config", path);
            if (File.Exists(customPath))
            {
                Rampastring.Tools.Logger.Log($"从用户目录加载配置: {path}");
                config = new IniFile(customPath);
                configCache[cacheKey] = config;
                return config;
            }
            
            // 最后使用默认配置
            string defaultPath = Path.Combine(Environment.CurrentDirectory, "Config", "Default", path);
            if (File.Exists(defaultPath))
            {
                Rampastring.Tools.Logger.Log($"从默认目录加载配置: {path}");
                config = new IniFile(defaultPath);
                configCache[cacheKey] = config;
                return config;
            }
            
            Rampastring.Tools.Logger.Log($"警告: 找不到配置文件 {path}");
            return new IniFile();
        }
    }
} 