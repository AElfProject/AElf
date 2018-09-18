using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Peers;

namespace AElf.Network
{
    public interface INetworkManager
    {
        event EventHandler MessageReceived;
        event EventHandler BlockReceived;
        event EventHandler TransactionsReceived;
        
        void Start();
        
        void QueueTransactionRequest(List<byte[]> transactionHashes, IPeer hint);
        void QueueBlockRequestByIndex(int index);

        //void QueueRequest(Message message, IPeer hint);

        Task<int> BroadcastBlock(byte[] hash, byte[] payload);
        Task<int> BroadcastMessage(AElfProtocolMsgType messageMsgType, byte[] payload);

        int GetPendingRequestCount();
    }
}