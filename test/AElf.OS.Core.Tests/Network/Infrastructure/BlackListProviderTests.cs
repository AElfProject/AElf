using System;
using System.Net;
using System.Threading.Tasks;
using AElf.OS.Network.Helpers;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Infrastructure
{
    public class BlackListProviderTests : NetworkInfrastructureTestBase
    {
        private IBlackListedPeerProvider _blackListProvider;

        public BlackListProviderTests()
        {
            _blackListProvider = GetRequiredService<IBlackListedPeerProvider>();
        }

        [Fact]
        public async Task IsIpBlackListed_Test()
        {
            var ipAddress = "127.0.0.1";

            _blackListProvider.IsIpBlackListed(ipAddress).ShouldBeFalse();
            
            _blackListProvider.AddHostToBlackList(ipAddress, 1);
            _blackListProvider.IsIpBlackListed(ipAddress).ShouldBeTrue();
            
            await Task.Delay(1200);
            
            _blackListProvider.IsIpBlackListed(ipAddress).ShouldBeFalse();
        }
        
        [Fact]
        public void RemovePeerFromBlacklist_Test()
        {
            var ipAddress = "127.0.0.1";

            _blackListProvider.AddHostToBlackList(ipAddress, int.MaxValue);
            _blackListProvider.IsIpBlackListed(ipAddress).ShouldBeTrue();

            _blackListProvider.RemoveHostFromBlackList(ipAddress).ShouldBeTrue();
            
            _blackListProvider.IsIpBlackListed(ipAddress).ShouldBeFalse();
            
            _blackListProvider.RemoveHostFromBlackList(ipAddress).ShouldBeFalse();
        }

        [Fact]
        public async Task AddHostToBlackList_Test()
        {
            var ipAddress1 = "192.168.100.1";
            var ipAddress2 = "192.168.100.2";
            _blackListProvider.AddHostToBlackList(ipAddress1, 1).ShouldBeTrue();
            
            await Task.Delay(1200);
            _blackListProvider.AddHostToBlackList(ipAddress2, 1).ShouldBeTrue();

            var removeResult = _blackListProvider.RemoveHostFromBlackList(ipAddress1);
            removeResult.ShouldBeFalse();
            removeResult = _blackListProvider.RemoveHostFromBlackList(ipAddress2);
            removeResult.ShouldBeTrue();
            
            _blackListProvider.AddHostToBlackList(ipAddress1, 1);
            _blackListProvider.AddHostToBlackList(ipAddress2, 5);
            
            await Task.Delay(1200);
            _blackListProvider.AddHostToBlackList(ipAddress2, 1).ShouldBeFalse();
            removeResult = _blackListProvider.RemoveHostFromBlackList(ipAddress1);
            removeResult.ShouldBeFalse();
        }
    }
}