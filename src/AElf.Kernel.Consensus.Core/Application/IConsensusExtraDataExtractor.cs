using AElf.Kernel.Blockchain.Application;
using Google.Protobuf;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusExtraDataExtractor
    {
        ByteString ExtractConsensusExtraData(BlockHeader header);
    }

    public abstract class ConsensusExtraDataExtractorBase : IConsensusExtraDataExtractor
    {
        private readonly IBlockExtraDataService _blockExtraDataService;
        private readonly IConsensusExtraDataKeyProvider _consensusExtraDataKeyProvider;

        protected ConsensusExtraDataExtractorBase(IBlockExtraDataService blockExtraDataService,
            IConsensusExtraDataKeyProvider consensusExtraDataKeyProvider)
        {
            _blockExtraDataService = blockExtraDataService;
            _consensusExtraDataKeyProvider = consensusExtraDataKeyProvider;
        }

        public ByteString ExtractConsensusExtraData(BlockHeader header)
        {
            var consensusExtraData =
                _blockExtraDataService.GetExtraDataFromBlockHeader(
                    _consensusExtraDataKeyProvider.BlockHeaderExtraDataKey, header);
            return ValidateConsensusExtraData(header, consensusExtraData) ? consensusExtraData : null;
        }

        protected abstract bool ValidateConsensusExtraData(BlockHeader header, ByteString consensusExtraData);
    }

    public class DefaultConsensusExtraDataExtractor : ConsensusExtraDataExtractorBase
    {
        public DefaultConsensusExtraDataExtractor(IBlockExtraDataService blockExtraDataService,
            IConsensusExtraDataKeyProvider consensusExtraDataKeyProvider) : base(blockExtraDataService,
            consensusExtraDataKeyProvider)
        {
        }

        protected override bool ValidateConsensusExtraData(BlockHeader header, ByteString consensusExtraData)
        {
            return consensusExtraData != null;
        }
    }
}