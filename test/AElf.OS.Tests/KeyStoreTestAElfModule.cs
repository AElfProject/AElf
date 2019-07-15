using System;
using System.IO;
using AElf.Modularity;
using AElf.OS.Account.Infrastructure;
using AElf.OS.Node.Application;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(
        typeof(OSCoreTestAElfModule)
    )]
    public class KeyStoreTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<INodeEnvironmentService>(o =>
            {
                var service = new Mock<INodeEnvironmentService>();

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