using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockDownloadService
    {
        Task<int> DownloadBlocksAsync(Hash previousBlockHash, long previousBlockHeight, int batchRequestBlockCount,
            string suggestedPeerPubKey);
    }
}