using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class TxHubHandleBestChainFoundTests : BenchmarkTestBase
    {
        private readonly ITxHub _txHub;
        private readonly IBlockchainService _blockchainService;
        private readonly OSTestHelper _osTestHelper;

        public TxHubHandleBestChainFoundTests()
        {
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _txHub = GetRequiredService<ITxHub>();
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        [Params(1, 10, 100, 1000, 3000, 5000)] 
        public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            var transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);

            await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
            {
                Transactions = transactions
            });

            await _osTestHelper.MinedOneBlock();
        }

        [Benchmark]
        public async Task HandleBestChainFoundTest()
        {
            var chain = await _blockchainService.GetChainAsync();
            await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight
            });
        }
    }
}