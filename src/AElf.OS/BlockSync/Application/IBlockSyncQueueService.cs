using System;
using System.Threading.Tasks;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncQueueService
    {
        bool IsQueueAvailable(string queueName);

        void Enqueue(Func<Task> task, string queueName);
    }
}