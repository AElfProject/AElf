using System;
using Shouldly;
using Xunit;

namespace AElf.OS.BlockSync.Infrastructure
{
    public class AnnouncementCacheProviderTests : BlockSyncTestBase
    {
        private readonly IAnnouncementCacheProvider _announcementCacheProvider;
        
        public AnnouncementCacheProviderTests()
        {
            _announcementCacheProvider = GetRequiredService<IAnnouncementCacheProvider>();
        }
        
        [Fact]
        public void AnnouncementCache_Test()
        {
            var blockHash = HashHelper.ComputeFrom("BlockHash");
            var blockHeight = 0;
            var pubkey = "Pubkey";
            var addResult = _announcementCacheProvider.TryAddOrUpdateAnnouncementCache(blockHash, blockHeight, pubkey);
            addResult.ShouldBeTrue();

            var getResult = _announcementCacheProvider.TryGetAnnouncementNextSender(blockHash, out var senderPubKey);
            getResult.ShouldBeTrue();
            senderPubKey.ShouldBe(pubkey);
            
            getResult = _announcementCacheProvider.TryGetAnnouncementNextSender(blockHash, out senderPubKey);
            getResult.ShouldBeFalse();
            senderPubKey.ShouldBeNull();
            
            for (var i = 0; i < 101; i++)
            {
                var hash = HashHelper.ComputeFrom("BlockHash" + i);
                addResult = _announcementCacheProvider.TryAddOrUpdateAnnouncementCache(hash, blockHeight, pubkey);
                addResult.ShouldBeTrue();
            }
            
            getResult = _announcementCacheProvider.TryGetAnnouncementNextSender(HashHelper.ComputeFrom("BlockHash" + 0), out senderPubKey);
            getResult.ShouldBeFalse();
            senderPubKey.ShouldBeNull();
        }

        [Fact]
        public void AnnouncementCache_SameBlockHash_Test()
        {
            var blockHash = HashHelper.ComputeFrom("Hash");
            var blockHeight = 10;
            var pubkey = "Pubkey";

            var addResult = _announcementCacheProvider.TryAddOrUpdateAnnouncementCache(blockHash, blockHeight, pubkey);
            addResult.ShouldBeTrue();
            
            for (int i = 0; i < 10; i++)
            {
                addResult = _announcementCacheProvider.TryAddOrUpdateAnnouncementCache(blockHash, blockHeight,
                    pubkey + i);
                addResult.ShouldBeFalse();
            }
            
            var getResult = _announcementCacheProvider.TryGetAnnouncementNextSender(blockHash, out var senderPubKey);
            getResult.ShouldBeTrue();
            senderPubKey.ShouldBe(pubkey);
            
            for (int i = 0; i < 10; i++)
            {
                getResult = _announcementCacheProvider.TryGetAnnouncementNextSender(blockHash, out senderPubKey);
                getResult.ShouldBeTrue();
                senderPubKey.ShouldBe(pubkey + i);
            }
            
            getResult = _announcementCacheProvider.TryGetAnnouncementNextSender(blockHash, out senderPubKey);
            getResult.ShouldBeFalse();
            senderPubKey.ShouldBeNull();
        }
    }
}