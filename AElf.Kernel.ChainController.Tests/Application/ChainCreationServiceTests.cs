using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
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
        public async Task Create_NewChain_Success()
        {
            var chainId = 1;

            var chain = await _blockchainService.GetChainAsync();
            chain.ShouldBeNull();

            chain = await _chainCreationService.CreateNewChainAsync(new List<Transaction>());
            chain = await _blockchainService.GetChainAsync();
            chain.ShouldNotBeNull();

            var block = await _blockchainService.GetBlockByHashAsync(chain.BestChainHash);
            block.Header.Height.ShouldBe(ChainConsts.GenesisBlockHeight);
            block.Header.PreviousBlockHash.ShouldBe(Hash.Empty);
            block.Header.ChainId.ShouldBe(chain.Id);
        }
    }
}