using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Database;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.TestBase;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(
        typeof(SmartContractExecutionAElfModule),
        typeof(TestBaseAElfModule))]
    public class SmartContractExecutionTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(o => o.UseInMemoryDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(o => o.UseInMemoryDatabase());

            services.AddTransient<ITransactionExecutingService>(p =>
            {
                var mockService = new Mock<ITransactionExecutingService>();
                mockService.Setup(m => m.ExecuteAsync(It.IsAny<IChainContext>(), It.IsAny<List<Transaction>>(), It
                        .IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                    .Returns<IChainContext, List<Transaction>, DateTime,
                        CancellationToken>((chainContext, transactions, currentBlockTime, cancellationToken) =>
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