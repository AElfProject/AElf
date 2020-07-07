using System;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class BlackListProviderTests : NetworkInfrastructureTestBase
    {
        private IBlackListedPeerProvider _blackListProvider;

        public BlackListProviderTests()
        {
            _blackListProvider = GetRequiredService<IBlackListedPeerProvider>();
        }

        [Fact]
        public async Task AddPeerToBlacklist_ShouldTimeout()
        {
            var ipAddress = "127.0.0.1";

            _blackListProvider.AddHostToBlackList(ipAddress, 1);
            _blackListProvider.IsIpBlackListed(ipAddress).ShouldBeTrue();
            
            await Task.Delay(TimeSpan.FromSeconds(2));
            
            _blackListProvider.IsIpBlackListed(ipAddress).ShouldBeFalse();
        }
        
        [Fact]
        public void RemovePeerFromBlacklist_Test()
        {
            var ipAddress = "127.0.0.1";

            _blackListProvider.AddHostToBlackList(ipAddress, int.MaxValue);
            _blackListProvider.IsIpBlackListed(ipAddress).ShouldBeTrue();

            _blackListProvider.RemoveHostFromBlackList(ipAddress);
            
            _blackListProvider.IsIpBlackListed(ipAddress).ShouldBeFalse();
        }

        [Fact]
        public async Task CleanBlackList_Test()
        {
            var ipAddress = "192.168.100.1";
            _blackListProvider.AddHostToBlackList(ipAddress, 1);
            
            await Task.Delay(1200);
            _blackListProvider.AddHostToBlackList("192.168.100.2", 1);

            var removeResult = _blackListProvider.RemoveHostFromBlackList(ipAddress);
            removeResult.ShouldBeFalse();
        }
    }
}