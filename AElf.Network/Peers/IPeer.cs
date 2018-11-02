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
        event EventHandler SyncFinished;
        
        string IpAddress { get; }
        ushort Port { get; }

        bool IsAuthentified { get; }
        bool IsBp { get; }
        int KnownHeight { get; }

        bool Start();
        
        NodeData DistantNodeData { get; }
        byte[] DistantNodeAddress { get; }
        void EnqueueOutgoing(Message msg, Action<Message> successCallback = null);
        void Sync(int start, int target);
        void OnNewBlockAccepted(IBlock block);

        bool AnySyncing();
        void RequestHeaders(int headerIndex, int headerRequestCount);
    }
}