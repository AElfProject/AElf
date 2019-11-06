using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Grpc;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.OS.Network
{
    public class BoundedExpirationCacheTests : GrpcBasicNetworkTestBase
    {
        [Fact]
        public void Test_Add_Existence()
        {
            BoundedExpirationCache cache = new BoundedExpirationCache(10, 10_000);
            cache.HasHash(Hash.FromString("hello_world")).ShouldBeFalse();
            cache.TryAdd(Hash.FromString("hello_world")).ShouldBeTrue();
            cache.HasHash(Hash.FromString("hello_world")).ShouldBeTrue();
            cache.TryAdd(Hash.FromString("hello_world")).ShouldBeFalse();
        }

        [Fact]
        public async Task Test_Expiration()
        {
            BoundedExpirationCache cache = new BoundedExpirationCache(10, 4); // 4ms timeout
            cache.TryAdd(Hash.FromString("hello_world"));
            await Task.Delay(TimeSpan.FromSeconds(1));
            cache.HasHash(Hash.FromString("hello_world")).ShouldBeFalse();
        }
        
        [Fact]
        public async Task Test_MultiItem_Expiration()
        {
            int cacheCapacity = 10;
            int timeout = 1_000;
            BoundedExpirationCache cache = new BoundedExpirationCache(cacheCapacity, timeout);
            List<string> hashStrings = new List<string>();
            for (int i = 0; i < cacheCapacity; i++)
            {
                var current = $"hello_world_{i}";
                hashStrings.Add(current);
                cache.TryAdd(Hash.FromString(current)).ShouldBeTrue();
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(timeout+500));
            
            cache.TryAdd(Hash.FromString($"hello_world_{cacheCapacity}")).ShouldBeTrue();

            foreach (string hashString in hashStrings)
            {
                cache.HasHash(Hash.FromString(hashString)).ShouldBeFalse();
            }
        }

        [Fact]
        public async Task Test_Max_Capacity()
        {
            int cacheCapacity = 2;
            BoundedExpirationCache cache = new BoundedExpirationCache(cacheCapacity, 10_000);

            for (int i = 0; i < cacheCapacity; i++)
                cache.TryAdd(Hash.FromString($"hello_world_{i}")).ShouldBeTrue();

            cache.TryAdd(Hash.FromString($"hello_world_{cacheCapacity}")).ShouldBeFalse();
        }
    }
}