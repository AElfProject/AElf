using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.Database;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.CrossChain
{
    [DependsOn(typeof(AbpEventBusModule),
        typeof(ContractTestAElfModule),
        typeof(CrossChainAElfModule))]
    public class CrossChainTestModule : AElfModule
    {

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());
            
            // todo: Remove this
            context.Services.AddTransient<ITransactionResultQueryService, NoBranchTransactionResultService>();
            context.Services.AddTransient<ITransactionResultService, NoBranchTransactionResultService>();
            
            var keyPair = CrossChainTestHelper.EcKeyPair;
            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(m => m.GetPublicKeyAsync()).Returns(Task.FromResult(keyPair.PublicKey));
            mockAccountService.Setup(m => m.GetAccountAsync())
                .Returns(Task.FromResult(Address.FromPublicKey(keyPair.PublicKey)));
            context.Services.AddTransient(provider =>  mockAccountService.Object);
            
            var mockTransactionReadOnlyExecutionService = new Mock<ITransactionReadOnlyExecutionService>();
            mockTransactionReadOnlyExecutionService
                .Setup(m => m.ExecuteAsync(It.IsAny<IChainContext>(), It.IsAny<Transaction>(), It.IsAny<DateTime>()))
                .Returns<IChainContext, Transaction, DateTime>((chainContext, transaction, dateTime) =>
                {
                    string methodName = transaction.MethodName;
                    var trace = new TransactionTrace
                    {
                        TransactionId = transaction.GetHash(),
                        ExecutionStatus = ExecutionStatus.ExecutedButNotCommitted,
                        RetVal = new RetVal()
                    };
                    if (methodName == CrossChainConsts.GetParentChainIdMethodName)
                    {
                        var parentChainId = CrossChainTestHelper.ParentChainIdHeight.Keys.FirstOrDefault();
                        if(parentChainId == 0)
                            trace.ExecutionStatus = ExecutionStatus.ContractError;
                        else
                            trace.RetVal.Data = parentChainId.ToPbMessage().ToByteString();
                    }
                    else if (methodName == CrossChainConsts.GetParentChainHeightMethodName)
                    {
                        trace.RetVal.Data = CrossChainTestHelper.ParentChainIdHeight.Values.First().ToPbMessage()
                            .ToByteString();
                    }
                    else if (methodName == CrossChainConsts.GetSideChainHeightMethodName)
                    {
                        int sideChainId =
                            (int) ParamsPacker.Unpack(transaction.Params.ToByteArray(), new[] {typeof(int)})[0];
                        var exist = CrossChainTestHelper.SideChainIdHeights.TryGetValue(sideChainId, out var sideChainHeight);
                        if (!exist)
                            trace.ExecutionStatus = ExecutionStatus.ContractError;
                        else
                            trace.RetVal.Data = sideChainHeight.ToPbMessage().ToByteString();
                    }
                    else if (methodName == CrossChainConsts.GetAllChainsIdAndHeightMethodName)
                    {
                        var dict = new SideChainIdAndHeightDict();
                        dict.IdHeighDict.Add(CrossChainTestHelper.SideChainIdHeights);
                        dict.IdHeighDict.Add(CrossChainTestHelper.ParentChainIdHeight);
                        trace.RetVal.Data = dict.ToByteString();
                    }
                    else if (methodName == CrossChainConsts.GetSideChainIdAndHeightMethodName)
                    {
                        var dict = new SideChainIdAndHeightDict();
                        dict = new SideChainIdAndHeightDict();
                        dict.IdHeighDict.Add(CrossChainTestHelper.SideChainIdHeights);
                        trace.RetVal.Data = dict.ToByteString();
                    }

                    return Task.FromResult(trace);
                });
            context.Services.AddTransient(provider => mockTransactionReadOnlyExecutionService.Object);
        }
    }
}