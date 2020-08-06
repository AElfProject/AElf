using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Consensus.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Node
{
    [DependsOn(typeof(NodeAElfModule), typeof(KernelCoreTestAElfModule))]
    public class NodeTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<ChainOptions>(o => { o.ChainId = 1234; });

            var mockConsensusService = new Mock<IConsensusService>();
            var kernelNodeTestContext = new KernelNodeTestContext()
            {
                MockConsensusService = mockConsensusService
            };

            context.Services.AddSingleton(kernelNodeTestContext);
            context.Services.AddSingleton(mockConsensusService.Object);

            context.Services.AddTransient(p =>
            {
                var mockService = new Mock<IBlockExecutingService>();
                mockService.Setup(m =>
                        m.ExecuteBlockAsync(It.IsAny<BlockHeader>(), It.IsAny<List<Transaction>>()))
                    .Returns<BlockHeader, IEnumerable<Transaction>>((blockHeader, transactions) =>
                    {
                        var block = new Block
                        {
                            Header = new BlockHeader(blockHeader)
                            {
                                MerkleTreeRootOfTransactions = HashHelper.ComputeFrom("MerkleTreeRootOfTransactions"),
                                MerkleTreeRootOfWorldState = HashHelper.ComputeFrom("MerkleTreeRootOfWorldState"),
                                MerkleTreeRootOfTransactionStatus =
                                    HashHelper.ComputeFrom("MerkleTreeRootOfTransactionStatus")
                            },
                            Body = new BlockBody()
                        };
                        block.Body.AddTransactions(transactions.Select(x => x.GetHash()));
                        return Task.FromResult(new BlockExecutedSet() {Block = block});
                    });
                return mockService.Object;
            });
        }
    }
    
    public class KernelNodeTestContext
    {
        public Mock<IConsensusService> MockConsensusService { get; set; }
    }
}