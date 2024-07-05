using AElf.Kernel.Consensus.Application;
using Google.Protobuf;

namespace AElf.Kernel.Consensus.PoA.Application;

public class PoAExtraDataExtractor : IConsensusExtraDataExtractor
{
    public ByteString ExtractConsensusExtraData(BlockHeader header)
    {
        return ByteString.CopyFromUtf8("Pretending be a PoA consensus extra data.");
    }
}