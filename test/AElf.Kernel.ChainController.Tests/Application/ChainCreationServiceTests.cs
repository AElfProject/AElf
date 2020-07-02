using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.ChainController.Application
{
    public class ChainCreationServiceTests : ChainControllerTestBase
    {
        private readonly ChainCreationService _chainCreationService;
        private readonly IBlockchainService _blockchainService;

        public ChainCreationServiceTests()
        {
            _chainCreationService = GetRequiredService<ChainCreationService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task Create_NewChain_Success_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            chain.ShouldBeNull();

            await _chainCreationService.CreateNewChainAsync(new List<Transaction>());
            chain = await _blockchainService.GetChainAsync();
            chain.ShouldNotBeNull();

            var block = await _blockchainService.GetBlockByHashAsync(chain.BestChainHash);
            block.Header.Height.ShouldBe(AElfConstants.GenesisBlockHeight);
            block.Header.PreviousBlockHash.ShouldBe(Hash.Empty);
            block.Header.ChainId.ShouldBe(chain.Id);
        }
        [Fact]
        public async Task Create_NewChain_Failed_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            chain.ShouldBeNull();
            var transactions= new List<Transaction>();
            transactions.Add(new Transaction());
            try
            { 
                await _chainCreationService.CreateNewChainAsync(transactions);
            }
            catch (Exception e)
            {
                e.Message.ShouldBe("Invalid transaction: { }");
            }
        }
    }
}