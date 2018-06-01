using System.Collections.Generic;

namespace AElf.Network.Config
{
    public interface IAElfNetworkConfig
    {
        List<string> Bootnodes { get; }
        bool UseCustomBootnodes { get; }
        
        List<string> Peers { get; }
        
        string Host { get; }
        int Port { get; }
    }
}