using System;
using System.IO;

namespace AElf.Cli.Core
{
    public class AElfCliPaths
    {
        public static string Log => Path.Combine(AElfRootPath, "cli", "logs");

        private static readonly string AElfRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aelf");

    }
}