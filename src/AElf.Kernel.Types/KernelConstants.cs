using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel;

public static class KernelConstants
{
    public const long ReferenceBlockValidPeriod = 64 * 8;
    public const int PreProtocolVersion = 1;
    public const int ProtocolVersion = 2;
    public const int ClosedPort = 0;
    public const int DefaultRunnerCategory = 0;
    public const int CodeCoverageRunnerCategory = 30;
    public const string MergeBlockStateQueueName = "MergeBlockStateQueue";
    public const string UpdateChainQueueName = "UpdateChainQueue";
    public const string ConsensusRequestMiningQueueName = "ConsensusRequestMiningQueue";
    public const string ChainCleaningQueueName = "ChainCleaningQueue";
    public const string StorageKeySeparator = ",";
    public const string SignaturePlaceholder = "SignaturePlaceholder";
    public const string BlockExecutedDataKey = "BlockExecutedData";
    public static Duration AllowedFutureBlockTimeSpan = new() { Seconds = 4 };
}