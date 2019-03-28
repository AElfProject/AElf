﻿using System;
using System.IO;

namespace AElf.Common.Application
{
    public static class ApplicationHelper
    {
        private const string ApplicationFolderName = "aelf";
        private static string _appDataPath;

        static ApplicationHelper()
        {
            _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ApplicationFolderName);
        }

        public static string AppDataPath
        {
            get => _appDataPath;
            set
            {
                if (CheckPath(value))
                    _appDataPath = value;
            }
        }

        private static bool CheckPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            return true;
        }
    }
}