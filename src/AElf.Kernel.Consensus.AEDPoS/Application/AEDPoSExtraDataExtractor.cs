using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSExtraDataExtractor : ConsensusExtraDataExtractorBase
    {
        public AEDPoSExtraDataExtractor(IBlockExtraDataService blockExtraDataService,
            IConsensusExtraDataKeyProvider consensusExtraDataKeyProvider) : base(blockExtraDataService,
            consensusExtraDataKeyProvider)
        {
        }

        protected override bool ValidateConsensusExtraData(BlockHeader header, ByteString consensusExtraData)
        {
            var headerInformation = AElfConsensusHeaderInformation.Parser.ParseFrom(consensusExtraData);
            return headerInformation.SenderPubkey == header.SignerPubkey;
        }
    }
}