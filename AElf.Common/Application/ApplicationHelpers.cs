using System;
using System.IO;
using NLog.Targets.Wrappers;

namespace AElf.Common.Application
{
    public static class ApplicationHelpers
    {
        private const string ApplicationFolderName = "aelf";

        private static string _configPath;
        
        private static string _logPath;

        public static string ConfigPath
        {
            get => _configPath;
            set
            {
                if (CheckPath(value))
                    _configPath = value;
            }
        }
        
        public static string LogPath
        {
            get => _logPath;
            set
            {
                if (CheckPath(value))
                    _logPath = value;
            }
        }

        static ApplicationHelpers()
        {
            _configPath= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationFolderName);
            _logPath = "logs";
        }

        private static bool CheckPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return true;
        }
    }
}