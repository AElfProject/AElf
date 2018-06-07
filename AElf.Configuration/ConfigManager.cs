using System.Collections.Generic;
using System.Globalization;

namespace AElf.Configuration
{
    public class ConfigManager
    {
        private static Dictionary<string,ConfigInfo> ConfigInfos = new Dictionary<string, ConfigInfo>();

        private static readonly object _configLock = new object();
        
        public static T GetConfigInstance<T>()
        {
            return default(T);
        }
        
        public static T GetConfigInstance<T>(string name)
        {
            string configName = name.ToLower();
            ConfigInfo config = GetConfigInfo(configName);            
            if (config == null)
            {                
                lock (_configLock)
                {                    
                    if (!ConfigInfos.TryGetValue(configName, out config))
                    {
                        //config = new ConfigInfo(configName, typeof(T));
                        ConfigInfos.Add(configName, config);
                                            }                    
                }
            }
            return (T)config.Value;
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
    }
}