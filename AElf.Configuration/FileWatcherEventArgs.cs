using System;

namespace AElf.Configuration
{
    public class FileWatcherEventArgs:EventArgs
    {
        public string FileName { get; }

        public FileWatcherEventArgs(string fileName)
        {
            FileName = fileName;
        }
    }
}