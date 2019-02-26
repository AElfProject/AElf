using System;
using System.IO;

namespace AElf.Common.Application
{
    public static class ApplicationHelpers
    {
        private const string ApplicationFolderName = "aelf";
        private static string _appDataPath;

        public static string AppDataPath
        {
            get => _appDataPath;
            set
            {
                if (CheckPath(value))
                    _appDataPath = value;
            }
        }

        static ApplicationHelpers()
        {
            _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationFolderName);
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