using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class TxHubTransactionsReceiveTests: BenchmarkTestBase
    {
        private readonly ITxHub _txHub;
        private readonly OSTestHelper _osTestHelper;
        
        private List<Transaction> _transactions;
        
        public TxHubTransactionsReceiveTests()
        {
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _txHub = GetRequiredService<ITxHub>();
        }
        
        [Params(1, 10, 100, 1000, 3000, 5000)] 
        public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
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
    }
}