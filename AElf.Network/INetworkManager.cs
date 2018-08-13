using System;
using System.Threading.Tasks;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Peers;

namespace AElf.Network
{
    public interface INetworkManager
    {
        event EventHandler MessageReceived;
        
        void Start();
        
//        void QueueTransactionRequest(byte[] transaction, IPeer hint);
//        void QueueBlockRequestByIndex(int index);

        void QueueRequest(Message message, IPeer hint);

        Task<int> BroadcastBock(byte[] hash, byte[] payload);
        Task<int> BroadcastMessage(AElfProtocolType messageType, byte[] payload);
    }
}