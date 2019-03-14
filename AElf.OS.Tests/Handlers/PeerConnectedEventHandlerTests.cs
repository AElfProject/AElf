using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Jobs;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Events;
using AElf.OS.Network.Infrastructure;
using Moq;
using Volo.Abp.BackgroundJobs;
using Xunit;

namespace AElf.OS.Handlers
{
    public class PeerConnectedEventHandlerTests : SyncTestBase
    {
        // Cases to test:
        // 1) we already have the block
        // 2) we don't have the block => go get it:
        //     a) It's linkable => nothing happens
        //     b) It's not linkable => queue job
        
        private readonly PeerConnectedEventHandler _handler;
        private readonly List<ForkDownloadJobArgs> _jobQueue = new List<ForkDownloadJobArgs>();

        public PeerConnectedEventHandlerTests()
        {
            // todo find out why property injection doesn't work here
            _handler = GetRequiredService<PeerConnectedEventHandler>();
            _handler.BlockchainService = GetRequiredService<IBlockchainService>();
            _handler.NetworkService = GetRequiredService<INetworkService>();
            _handler.BlockchainExecutingService = GetRequiredService<IBlockchainExecutingService>();
            _handler.PeerPool = GetRequiredService<IPeerPool>();
            
            var jobManagerMock = new Mock<IBackgroundJobManager>();
            jobManagerMock
                .Setup(b => b.EnqueueAsync(It.IsAny<ForkDownloadJobArgs>(), It.IsAny<BackgroundJobPriority>(), It.IsAny<TimeSpan?>()))
                .Callback<ForkDownloadJobArgs, BackgroundJobPriority, TimeSpan?>((args, p, t) => { _jobQueue.Add(args); })
                .Returns(Task.FromResult(""));

            _handler.BackgroundJobManager = jobManagerMock.Object;
        }

        [Fact]
        public async Task HandleEventAsync_BlockAlreadyKnown_DoesNothing()
        {
            var announcement = new PeerNewBlockAnnouncement { BlockHash = Hash.FromString("block") };
            await _handler.HandleEventAsync(new AnnouncementReceivedEventData(announcement, "bp1"));
            
            Assert.Empty(_jobQueue);
        }
        
        [Fact]
        public async Task HandleEventAsync_UnknownLinkableBlock_DoesNotQueueJob()
        {
            var announcement = new PeerNewBlockAnnouncement { BlockHash = Hash.FromString("linkable") };
            await _handler.HandleEventAsync(new AnnouncementReceivedEventData(announcement, "bp1"));
            
            Assert.Empty(_jobQueue);
        }
        
        [Fact]
        public async Task HandleEventAsync_UnknownUnLinkableBlock_QueuesJob()
        {
            var announcement = new PeerNewBlockAnnouncement { BlockHash = Hash.FromString("unlinkable") };
            await _handler.HandleEventAsync(new AnnouncementReceivedEventData(announcement, "bp1"));
            
            Assert.True(_jobQueue.Count == 1);
        }
    }
}