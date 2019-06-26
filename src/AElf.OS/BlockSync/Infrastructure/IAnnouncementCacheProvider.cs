using AElf.Types;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IAnnouncementCacheProvider
    {
        bool TryAddAnnouncementCache(Hash blockHash, long blockHeight);
    }
}