using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus;

public class ConsensusRequestMiningEventData
{
    public ConsensusRequestMiningEventData(Hash previousBlockHash, long previousBlockHeight, Timestamp blockTime,
        Duration blockExecutionTime, Timestamp miningDueTime)
    {
        PreviousBlockHash = previousBlockHash;
        PreviousBlockHeight = previousBlockHeight;
        BlockTime = blockTime;
        BlockExecutionTime = blockExecutionTime;
        MiningDueTime = miningDueTime;
    }

    public Hash PreviousBlockHash { get; }
    public long PreviousBlockHeight { get; }
    public Duration BlockExecutionTime { get; }
    public Timestamp BlockTime { get; }
    public Timestamp MiningDueTime { get; set; }
}