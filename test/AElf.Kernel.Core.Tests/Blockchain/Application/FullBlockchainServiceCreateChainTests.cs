using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class FullBlockchainServiceCreateChainTests: AElfKernelTestBase
    {
        private readonly FullBlockchainService _fullBlockchainService;
        private readonly KernelTestHelper _kernelTestHelper;

        public FullBlockchainServiceCreateChainTests()
        {
            _fullBlockchainService = GetRequiredService<FullBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }
        
        [Fact]
        public async Task Create_Chain_Success()
        {
            var transactions = new List<Transaction>
            {
                _kernelTestHelper.GenerateTransaction(),
                _kernelTestHelper.GenerateTransaction()
            };
                
            var block = _kernelTestHelper.GenerateBlock(0, Hash.Empty, transactions);

            var chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldBeNull();
            
            var existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.ShouldBeNull();

            var createChainResult = await _fullBlockchainService.CreateChainAsync(block, transactions);

            chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldNotBeNull();
            chain.ShouldBe(createChainResult);

            existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.GetHash().ShouldBe(block.GetHash());

            var existTransactions = await _fullBlockchainService.GetTransactionsAsync(transactions.Select(t => t.GetHash()));
            existTransactions.ShouldContain(transactions[0]);
            existTransactions.ShouldContain(transactions[1]);
        }
    }
}