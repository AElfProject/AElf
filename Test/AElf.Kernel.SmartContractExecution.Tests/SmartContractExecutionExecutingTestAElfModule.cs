using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(
        typeof(SmartContractExecutionTestAElfModule),
        typeof(KernelCoreWithChainTestAElfModule)
    )]
    public class SmartContractExecutionExecutingTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            // TODO: No need mock ITransactionExecutingService, just mock Executive.
            services.AddTransient<ITransactionExecutingService>(p =>
            {
                var mockService = new Mock<ITransactionExecutingService>();
                mockService.Setup(m => m.ExecuteAsync(It.IsAny<BlockHeader>(), It.IsAny<List<Transaction>>(),
                        It.IsAny<CancellationToken>(), It.IsAny<bool>()))
                    .Returns<BlockHeader, List<Transaction>,
                        CancellationToken,bool>((blockHeader, transactions, cancellationToken, throwException) =>
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

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var kernelTestHelper = context.ServiceProvider.GetService<KernelTestHelper>();
            for (var i = 0; i < 4; i++)
            {
                AsyncHelper.RunSync(() => kernelTestHelper.AttachBlockToBestChain());
            }
        }
    }

    [DependsOn(
        typeof(SmartContractExecutionExecutingTestAElfModule)
    )]
    public class ExecuteFailedTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient<IBlockValidationService>(p =>
            {
                var mockProvider = new Mock<IBlockValidationService>();
                mockProvider.Setup(m => m.ValidateBlockBeforeExecuteAsync(It.IsAny<IBlock>()))
                    .ReturnsAsync(true);

                mockProvider.Setup(m => m.ValidateBlockAfterExecuteAsync(It.IsAny<IBlock>()))
                    .ReturnsAsync(true);

                return mockProvider.Object;
            });

            services.AddTransient<IBlockExecutingService>(p =>
            {
                var mockService = new Mock<IBlockExecutingService>();
                mockService.Setup(m =>
                        m.ExecuteBlockAsync(It.IsAny<BlockHeader>(), It.IsAny<IEnumerable<Transaction>>()))
                    .Returns<BlockHeader, IEnumerable<Transaction>>((blockHeader, transactions) =>
                    {
                        Block result;
                        if (blockHeader.Height == KernelConstants.GenesisBlockHeight)
                        {
                            result = new Block {Header = blockHeader};
                        }
                        else
                        {
                            result = new Block
                                {Header = new BlockHeader {Time = Timestamp.FromDateTime(DateTime.UtcNow)}};
                        }

                        return Task.FromResult(result);

                    });

                return mockService.Object;
            });
        }
    }

    [DependsOn(
        typeof(SmartContractExecutionExecutingTestAElfModule)
    )]
    public class ValidateAfterFailedTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddTransient<IBlockValidationService>(p =>
            {
                var mockProvider = new Mock<IBlockValidationService>();
                mockProvider.Setup(m => m.ValidateBlockBeforeExecuteAsync(It.IsAny<IBlock>()))
                    .ReturnsAsync(true);
                mockProvider.Setup(m => m.ValidateBlockAfterExecuteAsync(It.IsAny<IBlock>()))
                    .Returns<IBlock>((block) => Task.FromResult(block.Height == KernelConstants.GenesisBlockHeight));

                return mockProvider.Object;
            });
        }
    }
}