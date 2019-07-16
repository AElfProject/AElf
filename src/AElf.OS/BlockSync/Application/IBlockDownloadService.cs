using System.Threading.Tasks;
using AElf.OS.BlockSync.Types;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockDownloadService
    {
        Task<DownloadBlocksResult> DownloadBlocksAsync(Hash previousBlockHash, long previousBlockHeight, int batchRequestBlockCount,
            string suggestedPeerPubKey);

        bool ValidateQueueAvailability();
    }
}