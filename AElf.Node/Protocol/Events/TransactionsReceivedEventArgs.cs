using System;
using System.Collections.Generic;
using AElf.Kernel;
using AElf.Network;
using AElf.Network.Connection;
using AElf.Network.Peers;

namespace AElf.Node.Protocol.Events
{
    public class TransactionsReceivedEventArgs : EventArgs
    {
        public TransactionList Transactions { get; }
        public Peer Peer { get; }
        public AElfProtocolMsgType MsgType { get; }

        public TransactionsReceivedEventArgs(TransactionList txList, Peer peer, AElfProtocolMsgType msgType)
        {
            Transactions = txList;
            Peer = peer;
            MsgType = msgType;
        }
    }
}