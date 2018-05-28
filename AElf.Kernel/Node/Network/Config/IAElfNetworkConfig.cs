using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.Node.Network.Config
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