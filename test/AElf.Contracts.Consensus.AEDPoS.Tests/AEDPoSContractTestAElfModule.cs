using System.Linq;
using AElf.Contracts.Economic.TestBase;
using AElf.ContractTestKit;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using NSubstitute;
using Volo.Abp.Modularity;
using SmartContractConstants = AElf.Sdk.CSharp.SmartContractConstants;

namespace AElf.Contracts.Consensus.AEDPoS
{
    [DependsOn(typeof(EconomicContractsTestModule))]
    public class AEDPoSContractTestAElfModule : EconomicContractsTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.AddSingleton<IResetBlockTimeProvider, ResetBlockTimeProvider>();
        }
    }

    [DependsOn(typeof(EconomicContractsTestModule))]
    public class AEDPoSContractMockCrossChainContractAddressTestAElfModule : EconomicContractsTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.RemoveAll<IPreExecutionPlugin>();
            context.Services.AddSingleton<IResetBlockTimeProvider, ResetBlockTimeProvider>();

            context.Services.AddSingleton<ISmartContractBridgeContext>(o =>
            {
                var bridgeContext = new Mock<ISmartContractBridgeContext>();

                bridgeContext.Setup(c =>
                    c.GetContractAddressByName(It.Is<string>(a =>
                            a == SmartContractConstants.CrossChainContractSystemName))
                        .Returns(SampleAccount.Accounts.Last().Address));

                return bridgeContext.Object;
            });
        }
    }
}