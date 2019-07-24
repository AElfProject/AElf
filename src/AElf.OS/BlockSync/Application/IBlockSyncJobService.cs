using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncJobService
    {
        Task<bool> DoFetchBlockAsync(BlockFetchJobDto blockFetchJobDto, Func<IEnumerable<string>, bool> isQueueAvailable);
        Task<bool> DoDownloadBlocksAsync(BlockDownloadJobDto blockDownloadJobDto, Func<IEnumerable<string>, bool> isQueueAvailable);
    }

    public class BlockFetchJobDto
    {
        public Hash BlockHash { get; set; }

        public long BlockHeight { get; set; }

        public string SuggestedPeerPubkey { get; set; }
    }

    public class BlockDownloadJobDto
    {
        public Hash BlockHash { get; set; }

        public long BlockHeight { get; set; }

        public string SuggestedPeerPubkey { get; set; }
        public int BatchRequestBlockCount { get; set; }
    }
}