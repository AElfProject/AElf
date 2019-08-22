using System.Threading.Tasks;
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

        public AEDPoSExtraDataExtractor(IBlockExtraDataService blockExtraDataService)
        {
            _blockExtraDataService = blockExtraDataService;
        }

        public ByteString ExtractConsensusExtraData(BlockHeader header)
        {
            var consensusExtraData = _blockExtraDataService.GetExtraDataFromBlockHeader("Consensus", header);
            var headerInformation = AElfConsensusHeaderInformation.Parser.ParseFrom(consensusExtraData);
            // Validate header information
            if (headerInformation.SenderPubkey != header.SignerPubkey)
            {
                return null;
            }

            return consensusExtraData;
        }
    }
}