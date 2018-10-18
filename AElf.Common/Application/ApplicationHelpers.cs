using System;
using System.IO;

namespace AElf.Common.Application
{
    public static class ApplicationHelpers
    {
        private const string ApplicationFolderName = "aelf";

        private static string _dataDir;

        static ApplicationHelpers()
        {
            _dataDir= Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationFolderName);
        }

        public static string GetDefaultDataDir()
        {
            return _dataDir;
        }

        public static void SetDataDir(string dataDir)
        {
            if (string.IsNullOrWhiteSpace(dataDir))
            {
                return;
            }

            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            _dataDir = dataDir;
        }
    }
}