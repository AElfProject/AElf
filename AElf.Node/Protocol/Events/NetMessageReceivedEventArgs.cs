using System;
using AElf.Network.Connection;
using AElf.Network.Peers;

namespace AElf.Node.Protocol.Events
{
    public class NetMessageReceivedEventArgs : EventArgs
    {
        public Message Message { get; private set; }
        public PeerMessageReceivedArgs PeerMessage { get; private set; }
        
        public NetMessageReceivedEventArgs(Message message, PeerMessageReceivedArgs peerMessage)
        {
            Message = message;
            PeerMessage = peerMessage;
        }
    }
}