using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Common.Collections;
using AElf.Network.Peers;
using Xunit;

namespace AElf.Network.Tests.NetworkManagerTests
{
    public class PriorityTest
    {
        [Fact]
        public void TakeTest_EnqueueMessage()
        {
            BlockingPriorityQueue<PeerMessageReceivedArgs> prioQ = new BlockingPriorityQueue<PeerMessageReceivedArgs>();
            
            PeerMessageReceivedArgs higherPriorityMsg = new PeerMessageReceivedArgs();
            PeerMessageReceivedArgs msg = new PeerMessageReceivedArgs();
            
            prioQ.Enqueue(msg, 1);
            prioQ.Enqueue(higherPriorityMsg, 0);
            
            var out01 = prioQ.Take();
            
            Assert.NotNull(out01);
            Assert.Equal(out01, higherPriorityMsg);

            var out02 = prioQ.Take();
            
            Assert.NotNull(out02);
            Assert.Equal(out02, msg);
        }

        [Fact]
        public void EnqueueMessage()
        {
            var priorityQueue = new PriorityQueue<int, PeerMessageReceivedArgs>(2);
            var incomingJobs = new BlockingCollection<KeyValuePair<int, PeerMessageReceivedArgs>>(priorityQueue);

            PeerMessageReceivedArgs higherPriorityMsg = new PeerMessageReceivedArgs();
            PeerMessageReceivedArgs msg = new PeerMessageReceivedArgs();
            
            var kvp_p_1 = new KeyValuePair<int, PeerMessageReceivedArgs>(0, higherPriorityMsg);
            var kvp_p_2 = new KeyValuePair<int, PeerMessageReceivedArgs>(1, msg);
            
            // Event though we add kvp_p_2 first, kvp_p_1 should be the first to be dequeued.
            incomingJobs.Add(kvp_p_2);
            incomingJobs.Add(kvp_p_1);

            incomingJobs.TryTake(out var out01);
            
            Assert.NotNull(out01);
            Assert.Equal(out01.Value, kvp_p_1.Value);

            incomingJobs.TryTake(out var out02);
            
            Assert.NotNull(out02);
            Assert.Equal(out02.Value, kvp_p_2.Value);
        }
    }
}