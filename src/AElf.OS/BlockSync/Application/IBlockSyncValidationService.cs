using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network;
using AElf.Types;

namespace AElf.OS.BlockSync.Application
{
    public interface IBlockSyncValidationService
    {
        Task<bool> ValidateAnnouncementAsync(Chain chain, BlockAnnouncement blockAnnouncement, string senderPubKey);

        Task<bool> ValidateBlockAsync(Chain chain, BlockWithTransactions blockWithTransactions, string senderPubKey);

        bool ValidateQueueAvailability(IEnumerable<string> queueNames);
    }
}