using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Serialization;

namespace AElf.Configuration
{
    public class ConfigManager
    {
        private static readonly string _configFilePath = "/etc/aelfconfig/";
        private static Dictionary<string, ConfigInfo> ConfigInfos = new Dictionary<string, ConfigInfo>();

        private static readonly object _configLock = new object();

        public static string GetConfigName<T>()
        {
            Type t = typeof(T);
            object[] attrs = t.GetCustomAttributes(typeof(XmlRootAttribute), false);
            if (attrs.Length > 0)
            {
                return ((ConfigFileAttribute) attrs[0]).FileName;
            }

            return t.Name;
        }

        public static T GetConfigInstance<T>()
        {
            var configName = GetConfigName<T>();
            return GetConfigInstance<T>(configName);
        }

        public static T GetConfigInstance<T>(string name)
        {
            var configName = name.ToLower();
            var config = GetConfigInfo(configName);
            if (config == null)
            {
                lock (_configLock)
                {
                    if (!ConfigInfos.TryGetValue(configName, out config))
                    {
                        var configContent = GetFromLocalFile(configName);
                        config = new ConfigInfo(configName, typeof(T), configContent);
                        ConfigInfos.Add(configName, config);
                    }
                }
            }

            return (T) config.Value;
        }

        private static ConfigInfo GetConfigInfo(string configName)
        {
            configName = configName.ToLower();
            ConfigInfo entry;
            lock (_configLock)
            {
                ConfigInfos.TryGetValue(configName, out entry);
            }

            return entry;
        }

        private static string GetFromLocalFile(string name)
        {
            var filePath = _configFilePath + name;
            if (!File.Exists(filePath))
            {
                return null;
            }

            var text = File.ReadAllText(_configFilePath + name);
            return text;
        }
    }
}