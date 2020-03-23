using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.OS.Network.Types;
using AElf.Types;

namespace AElf.OS.Network.Application
{
    public interface INetworkService
    {
        Task<bool> AddPeerAsync(string address, bool isTrusted = false);
        Task<bool> RemovePeerByAddressAsync(string address,
            long removalTime = NetworkConstants.DefaultPeerRemovalSeconds);
        Task<bool> RemovePeerByPubkeyAsync(string peerPubkey,
            long removalTime = NetworkConstants.DefaultPeerRemovalSeconds);
        List<PeerInfo> GetPeers(bool includeFailing = true);
        PeerInfo GetPeerByPubkey(string peerPubkey);
        Task<Response<BlockWithTransactions>> GetBlockByHashAsync(Hash hash, string peerPubkey = null);
        Task<Response<List<BlockWithTransactions>>> GetBlocksAsync(Hash previousBlock, int count, string peerPubkey = null);
        Task BroadcastAnnounceAsync(BlockHeader blockHeader);
        Task BroadcastTransactionAsync(Transaction transaction);
        Task BroadcastBlockWithTransactionsAsync(BlockWithTransactions blockWithTransactions);
        Task BroadcastLibAnnounceAsync(Hash libHash, long libHeight);
        Task CheckPeersHealthAsync();
        void CheckNtpDrift();
        bool IsPeerPoolFull();
    }
}