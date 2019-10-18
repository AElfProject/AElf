using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class AEDPoSExtraDataExtractorTests : AEDPoSTestBase
    {
        private readonly IConsensusExtraDataExtractor _consensusExtraDataExtractor;
        private readonly IBlockchainService _blockchainService;

        public AEDPoSExtraDataExtractorTests()
        {
            _consensusExtraDataExtractor = GetRequiredService<IConsensusExtraDataExtractor>();
            _blockchainService = GetRequiredService<IBlockchainService>();
        }
        
        [Fact]
        public async Task ExtractConsensusExtraData_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var height = chain.BestChainHeight;
            var hash = chain.BestChainHash;

            var header = new BlockHeader
            {
                PreviousBlockHash = hash,
                Height = height,
                SignerPubkey = ByteString.CopyFromUtf8("fake-pubkey")
            };
            var result = _consensusExtraDataExtractor.ExtractConsensusExtraData(header);
            result.ShouldBeNull();
            
            header.SignerPubkey = ByteString.CopyFromUtf8("real-pubkey");
            result = _consensusExtraDataExtractor.ExtractConsensusExtraData(header);
            result.ShouldNotBeNull();
        }
    }
}