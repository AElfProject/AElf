using Google.Protobuf;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusExtraDataExtractor
    {
        ByteString ExtractConsensusExtraData(BlockHeader header);
        
        long GetForkBlockHeight();
    }

    public interface ITestForkService
    {
        ByteString ExtractConsensusExtraData(BlockHeader header);
    }
}