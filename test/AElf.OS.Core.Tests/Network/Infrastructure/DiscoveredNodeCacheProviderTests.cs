using Shouldly;
using Xunit;

namespace AElf.OS.Network.Infrastructure
{
    public class DiscoveredNodeCacheProviderTests : NetworkInfrastructureTestBase
    {
        private readonly IDiscoveredNodeCacheProvider _discoveredNodeCacheProvider;
        
        public DiscoveredNodeCacheProviderTests()
        {
            _discoveredNodeCacheProvider = GetRequiredService<IDiscoveredNodeCacheProvider>();
        }

        [Fact]
        public void DiscoveredNodeCache_Test()
        {
            var endpoint1 = "192.168.100.100:101";
            var endpoint2 = "192.168.100.100:102";

            var result = _discoveredNodeCacheProvider.TryTake(out var endpoint);
            result.ShouldBeFalse();
            endpoint.ShouldBeNull();
            
            _discoveredNodeCacheProvider.Add(endpoint1);
            _discoveredNodeCacheProvider.Add(endpoint2);

            result = _discoveredNodeCacheProvider.TryTake(out endpoint);
            result.ShouldBeTrue();
            endpoint.ShouldBe(endpoint1);
            
            _discoveredNodeCacheProvider.Add(endpoint1);
            
            result = _discoveredNodeCacheProvider.TryTake(out endpoint);
            result.ShouldBeTrue();
            endpoint.ShouldBe(endpoint2);
        }
    }
}