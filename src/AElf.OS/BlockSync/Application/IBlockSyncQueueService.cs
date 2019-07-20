using System;
using System.Threading.Tasks;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncQueueService
    {
        bool ValidateQueueAvailability(string queueName);

        void Enqueue(Func<Task> task, string queueName);
    }
}