using System;
using System.Threading.Tasks;
using AElf.Network.Data;
using AElf.Network.Peers;

namespace AElf.Network
{
    public interface INetworkManager
    {
        event EventHandler MessageReceived;
        
        void Start();
        
        void QueueTransactionRequest(byte[] transaction, Peer hint);
        void QueueBlockRequestByIndex(int index);

        Task<int> BroadcastMessage(MessageType messageType, byte[] payload);
    }
}