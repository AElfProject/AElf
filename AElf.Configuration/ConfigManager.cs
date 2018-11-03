using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using AElf.Common.Application;
using NLog;

[assembly: InternalsVisibleTo("AElf.Configuration.Tests")]

namespace AElf.Configuration
{
    internal class ConfigManager
    {
        private static readonly ILogger _logger;
        
        public static List<string> ConfigFilePaths = new List<string>
        {
            Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "config"),
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "config")
        };

        private static readonly Dictionary<string, ConfigInfo> _configInfos = new Dictionary<string, ConfigInfo>();
        private static readonly ReaderWriterLockSlim _configLock = new ReaderWriterLockSlim();

        static ConfigManager()
        {
            FileWatcher.FileChanged += ConfigChanged;
            _logger = LogManager.GetLogger("Configuration");
        }

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

        private static bool GetIsWatch<T>()
        {
            var t = typeof(T);
            var attrs = t.GetCustomAttributes(typeof(ConfigFileAttribute), false);
            return attrs.Length > 0 && ((ConfigFileAttribute) attrs[0]).IsWatch;
        }

        private static T GetConfigInstance<T>(string name)
        {
            var configName = name.ToLower();
            var config = GetConfigInfo(configName);
            if (config == null)
            {
                _configLock.EnterWriteLock();
                try
                {
                    if (_configInfos.TryGetValue(configName, out config))
                        return (T) config.Value;
                    var configContent = GetFromLocalFile(configName);
                    config = new ConfigInfo(configName, typeof(T), configContent);
                    _configInfos.Add(configName, config);
                }
                finally
                {
                    _configLock.ExitWriteLock();
                }

                if (GetIsWatch<T>())
                {
                    FileWatcher.AddWatch(name);
                }
            }

            return (T) config.Value;
        }

        private static ConfigInfo GetConfigInfo(string configName)
        {
            configName = configName.ToLower();
            ConfigInfo configInfo;
            _configLock.EnterReadLock();
            try
            {
                _configInfos.TryGetValue(configName, out configInfo);
            }
            finally
            {
                _configLock.ExitReadLock();
            }

            return configInfo;
        }

        private static string GetFromLocalFile(string name)
        {
            foreach (var path in ConfigFilePaths)
            {
                var filePath = Path.Combine(path, name);
                if (File.Exists(filePath))
                {
                    return File.ReadAllText(filePath);
                }
            }

            return null;
        }

        private static void ConfigChanged(object sender, FileWatcherEventArgs e)
        {
            var fileName = e.FileName.ToLower();
            var configInfo = GetConfigInfo(fileName);
            if (configInfo != null)
            {
                try
                {
                    var configContent = GetFromLocalFile(fileName);
                    var newConfig = JsonSerializer.Instance.Deserialize(configContent, configInfo.Type);
                    SetConfigInstance(newConfig, configInfo.Value);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Exception while handle config changed.");
                }
            }
        }
        
        private static void SetConfigInstance(object srcObject, object targetObject)
        {
            var type = targetObject.GetType();
            var propInstance = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.SetProperty | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy);
            propInstance.SetValue(null, srcObject, null);
        }
    }
}