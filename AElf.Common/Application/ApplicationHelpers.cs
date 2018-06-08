using System;
using System.IO;

namespace AElf.Common.Application
{
    public static class ApplicationHelpers
    {
        public const string ApplicationFolderName = "aelf";
        
        public static string GetDefaultDataDir()
        {
            try
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                    ApplicationFolderName);
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }
}