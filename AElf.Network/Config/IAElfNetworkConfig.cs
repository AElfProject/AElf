using System.Collections.Generic;
using AElf.Network.Data;

namespace AElf.Network.Config
{
    public interface IAElfNetworkConfig
    {
        List<NodeData> Bootnodes { get; }
        bool UseCustomBootnodes { get; }
        
        List<string> Peers { get; }

        string PeersDbPath { get; }

        //string Host { get; }
        int ListeningPort { get; }
        
        int MaxPeers { get; }
    }
}