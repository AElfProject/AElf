using System;
using System.Collections.Generic;
using AElf.Network.Connection;
using AElf.Network.Peers;

namespace AElf.Node.Protocol.Events
{
    public class RequestFailedEventArgs : EventArgs
    {
        public Message RequestMessage { get; set; }
        
        public byte[] ItemHash { get; set; }
        public int BlockIndex { get; set; }
        
        public List<IPeer> TriedPeers = new List<IPeer>();
    }
}