using System.Linq;
using AElf.ContractTestKit;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;
using SmartContractConstants = AElf.Sdk.CSharp.SmartContractConstants;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    [DependsOn(typeof(ContractTestAEDPoSExtensionModule),
        typeof(AEDPoSExtensionDemoModule))]
    public class MockCrossChainContractAddressTestAElfModule : ContractTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddAssemblyOf<MockCrossChainContractAddressTestAElfModule>();

            context.Services.AddSingleton(o =>
            {
                var bridgeContext = new Mock<ISmartContractBridgeContext>();

                bridgeContext.Setup(c =>
                        c.GetContractAddressByName(It.Is<string>(a =>
                            a == SmartContractConstants.CrossChainContractSystemName)))
                    .Returns(SampleAccount.Accounts.Last().Address);

                return bridgeContext.Object;
            });
        }
    }
}