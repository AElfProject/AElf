using System;
using AElf.Network.Data;
using AElf.Network.Peers;
using Newtonsoft.Json;

namespace AElf.Network.Eventing
{
    [JsonObject(MemberSerialization.OptIn)]
    public class PeerEventArgs : EventArgs
    {
        [JsonProperty(PropertyName = "ActionId")]
        public PeerEventType Actiontype { get; }
        
        public IPeer Peer { get; }

        [JsonProperty(PropertyName = "Peer")]
        public NodeData NodeData
        {
            get
            {
                return Peer?.DistantNodeData;
            }
        }

        public PeerEventArgs(IPeer peer, PeerEventType actiontype)
        {
            Peer = peer;
            Actiontype = actiontype;
        }
    }
    
    public enum PeerEventType { Added, Removed }
}