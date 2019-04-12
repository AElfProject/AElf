using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.EventMessages;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class TxHubTransactionsReceiveTests : BenchmarkTestBase
    {
        private IBlockchainService _blockchainService;
        private ITxHub _txHub;
        private OSTestHelper _osTestHelper;

        private Chain _chain;
        private List<Transaction> _transactions;

        [Params(1, 10, 100, 1000, 3000, 5000)] public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _txHub = GetRequiredService<ITxHub>();

            _chain = await _blockchainService.GetChainAsync();
            _transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);
        }

        [Benchmark]
        public async Task HandleTransactionsReceivedTest()
        {
            await _txHub.HandleTransactionsReceivedAsync(new TransactionsReceivedEvent
            {
                Transactions = _transactions
            });
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _txHub.HandleUnexecutableTransactionsFoundAsync(new UnexecutableTransactionsFoundEvent
                (null, _transactions.Select(t => t.GetHash()).ToList()));

            await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
            {
                BlockHash = _chain.BestChainHash,
                BlockHeight = _chain.BestChainHeight
            });
        }
    }
}