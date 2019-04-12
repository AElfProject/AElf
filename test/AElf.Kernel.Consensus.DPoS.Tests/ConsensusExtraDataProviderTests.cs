using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS
{
    public class ConsensusExtraDataProviderTests: DPoSConsensusTestBase
    {
        private readonly IBlockExtraDataProvider _blockExtraDataProvider;

        public ConsensusExtraDataProviderTests()
        {
            _blockExtraDataProvider = GetRequiredService<IBlockExtraDataProvider>();
        }

        [Fact]
        public async Task GetExtraDataForFillingBlockHeaderAsync_Test()
        {
            var blockHeader = new BlockHeader()
            {
                Height = 1
            };
            var result = await _blockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(blockHeader);
            result.ShouldBeNull();
            
            var blockHeader1 = new BlockHeader()
            {
                Height = 10,
                BlockExtraDatas =
                {
                    ByteString.CopyFromUtf8("test1")
                }
            };
            var result1 = await _blockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(blockHeader1);
            result1.ShouldBeNull();
            
            var blockHeader2 = new BlockHeader()
            {
                Height = 2,
                PreviousBlockHash = Hash.Generate()
            };
            var result2 = await _blockExtraDataProvider.GetExtraDataForFillingBlockHeaderAsync(blockHeader2);
            result2.ShouldBe(ByteString.Empty);
        }
    }
}