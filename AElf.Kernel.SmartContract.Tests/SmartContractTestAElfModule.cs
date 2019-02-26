using System.Collections.Generic;
using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract
{
    [DependsOn(
        typeof(SmartContractAElfModule),
        typeof(TestBaseAElfModule))]
    public class SmartContractTestAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
            
            services.AddTransient<ISmartContractRunner>(p =>
            {
                var mockSmartContractRunner = new Mock<ISmartContractRunner>();
                mockSmartContractRunner.SetupGet(v => v.Category).Returns(2);
                mockSmartContractRunner.Setup(m => m.CodeCheck(It.IsAny<byte[]>(), It.IsAny<bool>()));
                return mockSmartContractRunner.Object;
            });
        }
    }
}