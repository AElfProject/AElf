using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSExtraDataExtractor : IConsensusExtraDataExtractor
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IConsensusExtraDataProvider _consensusExtraDataProvider;

        public AEDPoSExtraDataExtractor(IBlockExtraDataService blockExtraDataService, 
            IConsensusExtraDataProvider consensusExtraDataProvider)
        {
            _blockExtraDataService = blockExtraDataService;
            _consensusExtraDataProvider = consensusExtraDataProvider;
        }

        public ByteString ExtractConsensusExtraData(BlockHeader header)
        {
            var consensusExtraData =
                _blockExtraDataService.GetExtraDataFromBlockHeader(_consensusExtraDataProvider.BlockHeaderExtraDataKey, header);
            if (consensusExtraData == null)
                return null;

            var headerInformation = AElfConsensusHeaderInformation.Parser.ParseFrom(consensusExtraData);

            // Validate header information
            return headerInformation.SenderPubkey != header.SignerPubkey ? null : consensusExtraData;
        }
    }
}