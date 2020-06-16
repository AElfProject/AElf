using System;
using Xunit;

namespace AElf.OS.BlockSync.Application
{
    public class BlockSyncQueueServiceTests : BlockSyncTestBase
    {
        private readonly IBlockSyncQueueService _blockSyncQueueService;
        
        public BlockSyncQueueServiceTests()
        {
            _blockSyncQueueService = GetRequiredService<IBlockSyncQueueService>();
        }

        [Fact]
        public void ValidateQueueAvailability_InvalidQueueName_Test()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _blockSyncQueueService.ValidateQueueAvailability("InvalidQueueName"));
        }
    }
}