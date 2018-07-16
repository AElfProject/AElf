using System;
using AElf.Common.Attributes;
using NLog;

namespace AElf.Configuration
{
    [LoggerName("Configuration")]
    public class ConfigBase<T> where T : new()
    {
        internal readonly ILogger _logger;
        private static T _instance = ConfigManager.GetConfigInstance<T>();

        public static T Instance
        {
            get { return _instance; }
            set
            {
                _instance = value;
                OnConfigChanged();
            }
        }

        public static event EventHandler ConfigChanged;

        public static void OnConfigChanged()
        {
            if (ConfigChanged != null)
            {
                ConfigChanged(Instance, EventArgs.Empty);
            }
        }

        public ConfigBase()
        {
            _logger = LogManager.GetLogger("Configuration");
        }
    }
}