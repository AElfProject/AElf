using System.Collections.Generic;
using AElf.Network.Data;
using NodeData = AElf.Network.Data.Protobuf.NodeData;

namespace AElf.Network.Config
{
    public interface IAElfNetworkConfig
    {
        List<NodeData> Bootnodes { get; }
        bool UseCustomBootnodes { get; }
        
        List<string> Peers { get; }

        string PeersDbPath { get; }

        //string Host { get; }
        int Port { get; }
        
        int MaxPeers { get; }
    }
}