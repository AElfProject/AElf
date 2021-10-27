using AElf.Types;

namespace AElf.OS.BlockSync.Infrastructure
{
    public interface IAnnouncementCacheProvider
    {
        bool TryAddOrUpdateAnnouncementCache(Hash blockHash, long blockHeight, string senderPubKey);
        bool TryGetAnnouncementNextSender(Hash blockHash, out string senderPubKey);
    }
}