using System;
using System.IO;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Modularity;
using AElf.OS.Account.Infrastructure;
using AElf.OS.Node.Application;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(OSCoreTestAElfModule)
    )]
    // ReSharper disable once InconsistentNaming
    public class KeyStoreTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<INodeInformationService>(o =>
            {
                var service = new Mock<INodeInformationService>();

                service.Setup(s => s.GetAppDataPath())
                    .Returns(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "aelftest"));

                return service.Object;
            });

            context.Services.AddSingleton<AElfKeyStore>();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
        }
    }
}