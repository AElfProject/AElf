using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract
{
    [DependsOn(
        typeof(SmartContractTestAElfModule))]
    public class SmartContractRunnerTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient<ISmartContractRunner>(p =>
            {
                var mockExecutive = new Mock<IExecutive>();

                var mockSmartContractRunner = new Mock<ISmartContractRunner>();
                mockSmartContractRunner.SetupGet(m => m.Category).Returns(KernelConstants.DefaultRunnerCategory);
                mockSmartContractRunner.Setup(m => m.CodeCheck(It.IsAny<byte[]>(), It.IsAny<bool>()));
                mockSmartContractRunner.Setup(m => m.RunAsync(It.IsAny<SmartContractRegistration>()))
                    .Returns(Task.FromResult(mockExecutive.Object));
                return mockSmartContractRunner.Object;
            });

            services.AddSingleton(p => Mock.Of<IAccountService>());
        }
    }
}