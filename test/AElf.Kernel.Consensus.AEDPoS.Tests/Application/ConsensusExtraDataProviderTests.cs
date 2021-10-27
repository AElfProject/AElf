using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class ConsensusExtraDataProviderTests : AEDPoSTestBase
    {
        private readonly IBlockExtraDataProvider _blockExtraDataProvider;
        private readonly IBlockchainService _blockchainService;

        public ConsensusExtraDataProviderTests()
        {
            _blockExtraDataProvider = GetRequiredService<IBlockExtraDataProvider>();
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task GetExtraDataForFillingBlockHeaderAsync_Test()
        {
            //null situation
            var blockHeader = new BlockHeader
            {
                Height = 1
            };
            var result = await _blockExtraDataProvider.GetBlockHeaderExtraDataAsync(blockHeader);
            result.ShouldBeNull();
            
            //with data
            var chain = await _blockchainService.GetChainAsync();
            var height = chain.BestChainHeight;
            var hash = chain.BestChainHash;

            var result1 = await _blockExtraDataProvider.GetBlockHeaderExtraDataAsync(new BlockHeader
            {
                PreviousBlockHash = hash,
                Height = height
            });
            result1.ShouldNotBeNull();
        }
    }
}