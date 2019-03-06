using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.SmartContractExecution.Scheduling;
using AElf.Modularity;
using AElf.TestBase;
using Castle.DynamicProxy.Generators;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(
        typeof(SmartContractExecutionTestAElfModule)
    )]
    public class SmartContractExecutionExecutingTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient<ITransactionExecutingService>(p =>
            {
                var mockService = new Mock<ITransactionExecutingService>();
                mockService.Setup(m => m.ExecuteAsync(It.IsAny<BlockHeader>(), It.IsAny<List<Transaction>>(),
                        It.IsAny<CancellationToken>()))
                    .Returns<BlockHeader, List<Transaction>,
                        CancellationToken>((blockHeader, transactions, cancellationToken) =>
                    {
                        var returnSets = new List<ExecutionReturnSet>();

                        var count = 0;
                        foreach (var tx in transactions)
                        {
                            if (cancellationToken.IsCancellationRequested && count >= 3)
                            {
                                break;
                            }

                            var returnSet = new ExecutionReturnSet
                            {
                                TransactionId = tx.GetHash()
                            };
                            returnSet.StateChanges.Add(tx.GetHash().ToHex(), tx.ToByteString());
                            returnSets.Add(returnSet);
                            count++;
                        }

                        return Task.FromResult(returnSets);
                    });

                return mockService.Object;
            });
        }
    }
}