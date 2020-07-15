using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Grpc
{
    public class BoundedExpirationCacheTests : GrpcNetworkTestBase
    {
        [Fact]
        public void Test_Add_Existence()
        {
            var hash = HashHelper.ComputeFrom("hello_world");
            var cache = new BoundedExpirationCache(10, 10_000);
            cache.HasHash(hash).ShouldBeFalse();
            cache.TryAdd(hash).ShouldBeTrue();
            cache.HasHash(hash).ShouldBeTrue();
            cache.TryAdd(hash).ShouldBeFalse();
        }

        [Fact]
        public async Task Test_Expiration()
        {
            var hash = HashHelper.ComputeFrom("hello_world");
            var cache = new BoundedExpirationCache(10, 4); // 4ms timeout
            cache.TryAdd(hash);
            await Task.Delay(TimeSpan.FromSeconds(1));

            cache.HasHash(hash, false).ShouldBeTrue();

            cache.HasHash(hash).ShouldBeFalse();
        }

        [Fact]
        public async Task Test_MultiItem_Expiration()
        {
            int cacheCapacity = 10;
            int timeout = 1_000;
            var cache = new BoundedExpirationCache(cacheCapacity, timeout);
            List<string> hashStrings = new List<string>();
            for (int i = 0; i < cacheCapacity; i++)
            {
                var current = $"hello_world_{i}";
                hashStrings.Add(current);
                cache.TryAdd(HashHelper.ComputeFrom(current)).ShouldBeTrue();
            }
            
            await Task.Delay(TimeSpan.FromMilliseconds(timeout+500));
            
            cache.TryAdd(HashHelper.ComputeFrom($"hello_world_{cacheCapacity}")).ShouldBeTrue();

            foreach (string hashString in hashStrings)
            {
                cache.HasHash(HashHelper.ComputeFrom(hashString)).ShouldBeFalse();
            }
        }

        [Fact]
        public void Test_Max_Capacity()
        {
            int cacheCapacity = 2;
            BoundedExpirationCache cache = new BoundedExpirationCache(cacheCapacity, 10_000);

            for (int i = 0; i < cacheCapacity; i++)
                cache.TryAdd(HashHelper.ComputeFrom($"hello_world_{i}")).ShouldBeTrue();

            cache.TryAdd(HashHelper.ComputeFrom($"hello_world_{cacheCapacity}")).ShouldBeFalse();
        }
    }
}