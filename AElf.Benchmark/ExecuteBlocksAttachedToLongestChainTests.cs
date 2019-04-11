using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class ExecuteBlocksAttachedToLongestChainTests : BenchmarkTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockchainExecutingService _blockchainExecutingService;
        private readonly OSTestHelper _osTestHelper;

        private Block _block;

        public ExecuteBlocksAttachedToLongestChainTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockchainExecutingService = GetRequiredService<IBlockchainExecutingService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }
        
        [Params(1, 10, 100, 1000, 3000, 5000)]
        public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            var chain = await _blockchainService.GetChainAsync();

            var transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);

            _block = _osTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);

            await _blockchainService.AddBlockAsync(_block);
            chain = await _blockchainService.GetChainAsync();
            await _blockchainService.AttachBlockToChainAsync(chain, _block);
        }

        [Benchmark]
        public async Task ExecuteBlocksAttachedToLongestChainTest()
        {
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain,
                BlockAttachOperationStatus.LongestChainFound);
        }
    }
}