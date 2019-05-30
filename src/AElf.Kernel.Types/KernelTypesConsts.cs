using System;

namespace AElf.Kernel
{
    public static class KernelConstants
    {
//        public const long GenesisBlockHeight = 1;
        public const long ReferenceBlockValidPeriod = 64 * 8;
        public const int ProtocolVersion = 1;
        public const int DefaultRunnerCategory = 0;
        public const int CodeCoverageRunnerCategory = 30;
        public const string MergeBlockStateQueueName = "MergeBlockStateQueue";
        public const string UpdateChainQueueName = "UpdateChainQueue";
        public const string ConsensusRequestMiningQueueName = "ConsensusRequestMiningQueue";
        public const string StorageKeySeparator = ",";
        public static TimeSpan AllowedFutureBlockTimeSpan = TimeSpan.FromSeconds(4);
    }
}