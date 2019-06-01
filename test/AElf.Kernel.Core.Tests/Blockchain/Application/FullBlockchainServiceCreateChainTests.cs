using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Application
{
    public class FullBlockchainServiceCreateChainTests: AElfKernelTestBase
    {
        private readonly FullBlockchainService _fullBlockchainService;

        public FullBlockchainServiceCreateChainTests()
        {
            _fullBlockchainService = GetRequiredService<FullBlockchainService>();
        }
        
        [Fact]
        public async Task Create_Chain_Success()
        {
            var block = new Block
            {
                Header = new BlockHeader
                {
                    Height = Constants.GenesisBlockHeight,
                    PreviousBlockHash = Hash.Empty,
                    Time = TimestampHelper.GetUtcNow()
                },
                Body = new BlockBody()
            };

            var chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldBeNull();
            
            var existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.ShouldBeNull();

            var createChainResult = await _fullBlockchainService.CreateChainAsync(block, new List<Transaction>());

            chain = await _fullBlockchainService.GetChainAsync();
            chain.ShouldNotBeNull();
            chain.ShouldBe(createChainResult);

            existBlock = await _fullBlockchainService.GetBlockByHashAsync(block.GetHash());
            existBlock.GetHash().ShouldBe(block.GetHash());
        }
    }
}