using System;
using System.IO;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Node.Infrastructure
{
    public class NodeEnvironmentProvider : INodeEnvironmentProvider, ISingletonDependency
    {
        private const string ApplicationFolderName = "aelf";
        private string _appDataPath;

        public NodeEnvironmentProvider()
        {
        }

        public string GetAppDataPath()
        {
            if (string.IsNullOrWhiteSpace(_appDataPath))
            {
                _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationFolderName);

                if (!Directory.Exists(_appDataPath))
                {
                    Directory.CreateDirectory(_appDataPath);
                }
            }

            return _appDataPath;
        }
    }
}