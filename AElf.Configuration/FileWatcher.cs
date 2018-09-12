using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common.Application;

namespace AElf.Configuration
{
    public class FileWatcher
    {
        private static readonly HashSet<string> _fileNames;
        private static readonly HashSet<string> _pendingFiles;

        private static FileSystemWatcher _watcher;
        
        private static readonly ReaderWriterLockSlim _fileNameLock = new ReaderWriterLockSlim();
        private static readonly object _pendingFileLock = new object();

        public delegate void FileWatcherEventHandler(object sender, FileWatcherEventArgs e);
        public static event FileWatcherEventHandler FileChanged;

        private const int DelayTime = 5000;

        static FileWatcher()
        {
            _fileNames = new HashSet<string>();
            _pendingFiles = new HashSet<string>();
            var configPath = Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "config");
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
            InitWatcher(configPath);
            Console.WriteLine("Init FileWatcher");
        }

        private static void InitWatcher(string directory)
        {
            _watcher = new FileSystemWatcher
            {
                Path = directory,
                IncludeSubdirectories = false
            };

            _watcher.Changed += WatchFileChanged;
            _watcher.Renamed += WatchFileChanged;
            _watcher.EnableRaisingEvents = true;
        }
        
        private static void WatchFileChanged(object sender, FileSystemEventArgs e)
        {
            var fileName = e.Name.ToLower();
            Console.WriteLine("File Changed:" + fileName);
            if (ContainFileName(fileName))
            {
                lock (_pendingFileLock)
                {
                    if (_pendingFiles.Contains(fileName))
                    {
                        return;
                    }

                    _pendingFiles.Add(fileName);
                }
                Task.Delay(DelayTime).ContinueWith(DelayNotify, fileName);
            }
        }

        private static void DelayNotify(Task task, object o)
        {
            var fileName = (string) o;
            lock (_pendingFileLock)
            {
                _pendingFiles.Remove(fileName);
            }

            if (FileChanged != null)
            {
                FileChanged(null, new FileWatcherEventArgs(fileName));
            }
        }

        public static void AddWatch(string fileName)
        {
            _fileNameLock.EnterWriteLock();
            try
            {
                _fileNames.Add(fileName);
            }
            finally
            {
                _fileNameLock.ExitWriteLock();
            }
        }

        private static bool ContainFileName(string fileName)
        {
            _fileNameLock.EnterReadLock();
            try
            {
                return _fileNames.Contains(fileName);
            }
            finally
            {
                _fileNameLock.ExitReadLock();
            }
        }
    }
}