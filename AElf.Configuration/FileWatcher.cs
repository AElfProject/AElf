using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AElf.Common.Application;

namespace AElf.Configuration
{
    public class FileWatcher
    {
        private static readonly HashSet<string> _fileNames;
        private static readonly List<string> _pendingFiles;
        
        public static event FileSystemEventHandler FileChanged; 

        static FileWatcher()
        {
            _fileNames = new HashSet<string>();
            InitWatcher(Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "config"));
        }
        
        private static void InitWatcher(string directory)
        {
            var watcher = new FileSystemWatcher
            {
                Path = directory,
                IncludeSubdirectories = false
            };

            watcher.Changed += WatchFileChanged;
            watcher.Renamed += WatchFileChanged;
            watcher.EnableRaisingEvents = true;
        }

        public static void AddWatch(string fileName)
        {
            _fileNames.Add(fileName);
        }

        private static void WatchFileChanged(object sender, FileSystemEventArgs e)
        {
            var fileName = e.Name.ToLower();
            _pendingFiles.Add(fileName);
            Thread.Sleep(5000);

            _pendingFiles.Remove(fileName);
            if (FileChanged != null)
            {
                FileChanged(sender, e);
            }
        }
    }
}