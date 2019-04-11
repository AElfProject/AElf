using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class BlockAttachTests : BenchmarkTestBase
    {
        private readonly IBlockchainService _blockchainService;
        private readonly IBlockAttachService _blockAttachService;
        private readonly OSTestHelper _osTestHelper;

        private Block _block;

        public BlockAttachTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
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
        }

        [Benchmark]
        public async Task AttachBlockTest()
        {
            await _blockAttachService.AttachBlockAsync(_block);
        }
    }
}