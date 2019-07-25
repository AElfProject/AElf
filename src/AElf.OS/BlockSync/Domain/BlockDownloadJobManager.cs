using System.Threading.Tasks;
using AElf.OS.BlockSync.Infrastructure;
using AElf.OS.BlockSync.Types;
using AElf.Types;

namespace AElf.OS.BlockSync.Domain
{
    public class BlockDownloadJobManager : IBlockDownloadJobManager
    {
        private readonly IBlockDownloadJobStore _blockDownloadJobStore;

        public BlockDownloadJobManager(IBlockDownloadJobStore blockDownloadJobStore)
        {
            _blockDownloadJobStore = blockDownloadJobStore;
        }

        public async Task<Hash> EnqueueAsync(Hash syncBlockHash, long syncBlockHeight, int batchRequestBlockCount,
            string suggestedPeerPubkey)
        {
            var blockDownloadJobInfo = new BlockDownloadJobInfo
            {
                JobId = syncBlockHash,
                TargetBlockHash = syncBlockHash,
                TargetBlockHeight = syncBlockHeight,
                BatchRequestBlockCount = batchRequestBlockCount,
                SuggestedPeerPubkey = suggestedPeerPubkey
            };

            var addResult = await _blockDownloadJobStore.AddAsync(blockDownloadJobInfo);

            return addResult ? blockDownloadJobInfo.JobId : null;
        }
    }
}