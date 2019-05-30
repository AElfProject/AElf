using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.OS
{
    public class BlockSyncTestHelper
    {
        private readonly ITaskQueueManager _taskQueueManager;
        public BlockSyncTestHelper(ITaskQueueManager taskQueueManager)
        {
            _taskQueueManager = taskQueueManager;
        }

        public void DisposeQueue()
        {
            _taskQueueManager.GetQueue(OSConsts.BlockSyncQueueName).Dispose();
            _taskQueueManager.GetQueue(OSConsts.BlockSyncAttachQueueName).Dispose();
            _taskQueueManager.GetQueue(KernelConstants.UpdateChainQueueName).Dispose();
            _taskQueueManager.GetQueue(KernelConstants.MergeBlockStateQueueName).Dispose();
        }
    }
}