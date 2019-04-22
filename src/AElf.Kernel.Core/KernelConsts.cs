using System;

namespace AElf.Kernel
{
    public class KernelConsts
    {
        public const string MergeBlockStateQueueName = "MergeBlockStateQueue";
        public const string CleanBranchesQueueName = "CleanBranchesQueue";
        public const string UpdateChainQueueName = "UpdateChainQueue";
        public const string StorageKeySeparator = ",";
        public static TimeSpan AllowedFutureBlockTimeSpan = TimeSpan.FromSeconds(4);
    }
}