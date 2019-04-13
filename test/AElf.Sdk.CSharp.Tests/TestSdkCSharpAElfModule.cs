using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Sdk.CSharp.Tests
{
    [DependsOn(
        typeof(SmartContractAElfModule),
        typeof(TestBaseKernelAElfModule))]
    public class TestSdkCSharpAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            
            services.AddTransient(p =>
            {
                var mockExecutive = new Mock<IExecutive>();
                
                var mockSmartContractRunner = new Mock<ISmartContractRunner>();
                mockSmartContractRunner.SetupGet(m => m.Category).Returns(0);
                mockSmartContractRunner.Setup(m => m.CodeCheck(It.IsAny<byte[]>(), It.IsAny<bool>()));
                mockSmartContractRunner.Setup(m => m.RunAsync(It.IsAny<SmartContractRegistration>()))
                    .Returns(Task.FromResult(mockExecutive.Object));
                return mockSmartContractRunner.Object;
            });
            
            services.AddSingleton(p => Mock.Of<IAccountService>());
        }
    }
}