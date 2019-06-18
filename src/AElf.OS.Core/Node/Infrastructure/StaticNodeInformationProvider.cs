using System;
using System.IO;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Node.Infrastructure
{
    public class StaticNodeInformationProvider : IStaticNodeInformationProvider, ISingletonDependency
    {
        private const string ApplicationFolderName = "aelf";
        public string AppDataPath { get; }

        public StaticNodeInformationProvider()
        {
            AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), ApplicationFolderName);
        }
    }
}