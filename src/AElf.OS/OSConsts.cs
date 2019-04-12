using System;

namespace AElf.OS
{
    public class OSConsts
    {
        public const string BlockSyncQueueName = "BlockSyncQueue";
        public static TimeSpan AllowedFutureBlockTimeSpan = TimeSpan.FromSeconds(4);
    }
}