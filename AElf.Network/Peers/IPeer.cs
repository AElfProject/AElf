using System;
using AElf.Kernel;
using AElf.Network.Connection;
using AElf.Network.Data;

namespace AElf.Network.Peers
{
    public interface IPeer : IDisposable
    {
        event EventHandler MessageReceived;
        event EventHandler PeerDisconnected;
        event EventHandler AuthFinished;
        
        string IpAddress { get; }
        ushort Port { get; }

        bool IsAuthentified { get; }
        bool IsBp { get; }
        int KnownHeight { get; }
        
        bool IsSyncingHistory { get; }
        bool IsSyncingAnnounced { get; }
        int CurrentlyRequestedHeight { get; }
        bool AnyStashed { get; }
        bool IsSyncing { get; }

        bool Start();
        
        NodeData DistantNodeData { get; }
        byte[] DistantNodeAddress { get; }
        void EnqueueOutgoing(Message msg, Action<Message> successCallback = null);
        
        void StashAnnouncement(Announce announce);
        int GetLowestAnnouncement();
        
        void SyncToHeight(int start, int target);
        bool SyncNextHistory();
        bool SyncNextAnnouncement(int? expected = null);

        void RequestHeaders(int headerIndex, int headerRequestCount);
    }
}