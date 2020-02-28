using Google.Protobuf;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusExtraDataExtractor
    {
        ByteString ExtractConsensusExtraData(BlockHeader header);
    }
}