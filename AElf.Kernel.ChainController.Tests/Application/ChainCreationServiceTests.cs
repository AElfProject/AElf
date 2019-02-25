using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.ChainController.Application
{
    public class ChainCreationServiceTests: ChainControllerTestBase
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
            var chain = await _chainCreationService.CreateNewChainAsync(1, new List<Transaction>());
            chain.Id.ShouldBe(1);

            var block = await _blockchainService.GetBlockByHashAsync(chain.Id, chain.BestChainHash);
            block.Header.Height.ShouldBe(GlobalConfig.GenesisBlockHeight);
            block.Header.PreviousBlockHash.ShouldBe(Hash.Genesis);
            block.Header.ChainId.ShouldBe(chain.Id);
        }
    }
}