using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Contexts;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Sdk.CSharp.Tests
{
    [DependsOn(
        typeof(DatabaseAElfModule),
        typeof(TestBaseAElfModule),
        typeof(SmartContractAElfModule))]
    public class TestSdkCSharpAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            context.Services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            context.Services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddTransient<IStateProviderFactory, StateProviderFactory>();

            services.AddTransient<ISmartContractService>(p =>
            {
                var mockService = new Mock<ISmartContractService>();
                mockService
                    .Setup(m => m.DeployContractAsync(It.IsAny<int>(), It.IsAny<Address>(),
                        It.IsAny<SmartContractRegistration>(),
                        It.IsAny<bool>()))
                    .Returns(Task.CompletedTask);
                mockService
                    .Setup(m => m.UpdateContractAsync(It.IsAny<int>(), It.IsAny<Address>(),
                        It.IsAny<SmartContractRegistration>(),
                        It.IsAny<bool>()))
                    .Returns(Task.CompletedTask);
                return mockService.Object;
            });
        }
    }
}