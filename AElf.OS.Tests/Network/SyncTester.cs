using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Services;
using AElf.OS.Handlers;
using AElf.OS.Jobs;
using AElf.OS.Network;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using Volo.Abp.EventBus.Local;
using Xunit;
using Xunit.Abstractions;

namespace AElf.OS.Tests.Network
{
    public class SyncTester : OSTestBase
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly ILocalEventBus _eventBus;

        public SyncTester(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _eventBus = GetRequiredService<ILocalEventBus>();
        }
        
        [Fact]
        public void Test()
        {
            _eventBus.Subscribe<PeerConnectedEventData, PeerConnectedEventHandler>();
            
            // simulate the connection
            _eventBus.PublishAsync(new PeerConnectedEventData { BlockId = new byte[]{ 0x01, 0x02 }});
        }
    }
}