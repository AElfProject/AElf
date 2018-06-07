using System;

namespace AElf.Configuration
{
    public class ConfigBase<T> where T : new()
    {
        private static T _instance = ConfigManager.GetConfigInstance<T>();
        public static T Instance
        {
            get
            {
                return _instance;
            }
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
    }
}