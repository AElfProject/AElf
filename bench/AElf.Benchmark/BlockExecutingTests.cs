using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using BenchmarkDotNet.Attributes;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class BlockExecutingTests : BenchmarkTestBase
    {
        private IBlockExecutingService _blockExecutingService;
        private IBlockchainService _blockchainService;
        private OSTestHelper _osTestHelper;

        private List<Transaction> _transactions;
        private Block _block;
        
        [Params(1, 10, 100, 1000, 3000, 5000)]
        public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            
            var chain = await _blockchainService.GetChainAsync();

            _block = new Block
            {
                Header = new BlockHeader
                {
                    ChainId = chain.Id,
                    Height = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash,
                    Time = Timestamp.FromDateTime(DateTime.UtcNow)
                },
                Body = new BlockBody()
            };

            _transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);
        }

        [Benchmark]
        public async Task ExecuteBlock()
        {
            await _blockExecutingService.ExecuteBlockAsync(_block.Header, _transactions);
        }
    }
}