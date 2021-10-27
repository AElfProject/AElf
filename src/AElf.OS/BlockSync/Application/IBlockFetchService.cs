using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockFetchService
    {
        Task<bool> FetchBlockAsync(Hash blockHash, long blockHeight, string suggestedPeerPubKey);
    }
}