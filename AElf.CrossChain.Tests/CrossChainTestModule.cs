using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Tests;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(
        typeof(CrossChainAElfModule), typeof(KernelCoreTestAElfModule))]
    public class CrossChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //context.Services.AddTransient<IBlockValidationProvider, CrossChainValidationProvider>();
            context.Services.AddSingleton<CrossChainTestHelper>();
            context.Services.AddTransient(provider =>
            {
                var mockTransactionReadOnlyExecutionService = new Mock<ITransactionReadOnlyExecutionService>();
                mockTransactionReadOnlyExecutionService
                .Setup(m => m.ExecuteAsync(It.IsAny<IChainContext>(), It.IsAny<Transaction>(), It.IsAny<DateTime>()))
                .Returns<IChainContext, Transaction, DateTime>((chainContext, transaction, dateTime) =>
                {
                    var crossChainTestHelper = context.Services.GetRequiredServiceLazy<CrossChainTestHelper>().Value;                   
                    return Task.FromResult(crossChainTestHelper.CreateFakeTransactionTrace(transaction));
                });
                return mockTransactionReadOnlyExecutionService.Object;
            });
            context.Services.AddTransient(provider =>
            {
                var mockSmartContractAddressService = new Mock<ISmartContractAddressService>();
                mockSmartContractAddressService.Setup(m => m.GetAddressByContractName(It.IsAny<Hash>()))
                    .Returns(Address.Generate);
                return mockSmartContractAddressService.Object;
            });
        }
    }
}