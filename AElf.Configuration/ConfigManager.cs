using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AElf.Common.Application;

[assembly: InternalsVisibleTo("AElf.Configuration.Tests")]

namespace AElf.Configuration
{
    internal class ConfigManager
    {
        public static readonly List<string> ConfigFilePaths = new List<string>
        {
            Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "config"),
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "config")
        };

        private static readonly Dictionary<string, ConfigInfo> ConfigInfos = new Dictionary<string, ConfigInfo>();
        private static readonly object ConfigLock = new object();

        internal static T GetConfigInstance<T>()
        {
            var configName = GetConfigName<T>();
            return GetConfigInstance<T>(configName);
        }

        private static string GetConfigName<T>()
        {
            var t = typeof(T);
            var attrs = t.GetCustomAttributes(typeof(ConfigFileAttribute), false);
            return attrs.Length > 0 ? ((ConfigFileAttribute) attrs[0]).FileName : t.Name;
        }

        private static T GetConfigInstance<T>(string name)
        {
            var configName = name.ToLower();
            var config = GetConfigInfo(configName);
            if (config != null) return (T) config.Value;
            lock (ConfigLock)
            {
                if (ConfigInfos.TryGetValue(configName, out config)) return (T) config.Value;
                var configContent = GetFromLocalFile(configName);
                config = new ConfigInfo(configName, typeof(T), configContent);
                ConfigInfos.Add(configName, config);
            }

            return (T) config.Value;
        }

        private static ConfigInfo GetConfigInfo(string configName)
        {
            configName = configName.ToLower();
            ConfigInfo configInfo;
            lock (ConfigLock)
            {
                ConfigInfos.TryGetValue(configName, out configInfo);
            }

            return configInfo;
        }

        private static string GetFromLocalFile(string name)
        {
            return (from configFilePath in ConfigFilePaths
                select Path.Combine(configFilePath, name)
                into filePath
                where File.Exists(filePath)
                select File.ReadAllText(filePath)).FirstOrDefault();
        }
    }
}