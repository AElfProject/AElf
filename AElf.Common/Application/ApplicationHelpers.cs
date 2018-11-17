using System;
using System.IO;

namespace AElf.Common.Application
{
    public static class ApplicationHelpers
    {
        private const string ApplicationFolderName = "aelf";

        private static string _configPath;

        static ApplicationHelpers()
        {
            _configPath= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationFolderName);
        }

        public static string GetDefaultConfigPath()
        {
            return _configPath;
        }

        public static void SetConfigPath(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
            {
                return;
            }

            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }

            _configPath = configPath;
        }
    }
}