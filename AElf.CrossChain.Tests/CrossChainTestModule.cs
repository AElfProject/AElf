using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Tests;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(
        typeof(ContractTestAElfModule),
        typeof(CrossChainAElfModule),
        typeof(KernelTestAElfModule))]
    public class CrossChainTestModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var keyPair = CryptoHelpers.GenerateKeyPair();
            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(m => m.GetPublicKeyAsync()).Returns(Task.FromResult(keyPair.PublicKey));
            mockAccountService.Setup(m => m.GetAccountAsync())
                .Returns(Task.FromResult(Address.FromPublicKey(keyPair.PublicKey)));
            context.Services.AddTransient(provider =>  mockAccountService.Object);
            
//            var extraDataOrderInformation = context.Services.GetRequiredServiceLazy<IBlockExtraDataOrderService>().Value;
//            var blockExtraDataProviders = context.Services.GetRequiredServiceLazy<IEnumerable<IBlockExtraDataProvider>>().Value;
//            foreach (var blockExtraDataProvider in blockExtraDataProviders)
//            {
//                extraDataOrderInformation.AddExtraDataProvider(blockExtraDataProvider.GetType());
//            }

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
        }
    }
}