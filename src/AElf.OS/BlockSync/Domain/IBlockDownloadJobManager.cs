using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.BlockSync.Domain
{
    public interface IBlockDownloadJobManager
    {
        Task<Hash> EnqueueAsync(Hash syncBlockHash, long syncBlockHeight, int batchRequestBlockCount,
            string suggestedPeerPubkey);
    }
}