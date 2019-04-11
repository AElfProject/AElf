using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.OS;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class BlockchainStateMergingTests : BenchmarkTestBase
    {
        private readonly IBlockchainStateMergingService _blockchainStateMergingService;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainManager _chainManager;
        private readonly OSTestHelper _osTestHelper;

        public BlockchainStateMergingTests()
        {
            _blockchainStateMergingService = GetRequiredService<IBlockchainStateMergingService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _chainManager = GetRequiredService<IChainManager>();
        }

        [Params(1, 10, 100, 1000, 3000, 5000)]
        public int TransactionCount;

        [Params(1, 10, 50)] 
        public int BlockCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            var transactionCount = TransactionCount;
            for (var i = 0; i < BlockCount; i++)
            {
                var transactions = await _osTestHelper.GenerateTransferTransactions(transactionCount);
                await _osTestHelper.BroadcastTransactions(transactions);
                await _osTestHelper.MinedOneBlock();
            }

            var chain = await _blockchainService.GetChainAsync();
            await _chainManager.SetIrreversibleBlockAsync(chain, chain.BestChainHash);
        }

        [Benchmark]
        public async Task MergeBlockStateTest()
        {
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainStateMergingService.MergeBlockStateAsync(chain.BestChainHeight, chain.BestChainHash);
        }
    }
}