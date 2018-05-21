using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.Node.Network.Config
{
    public class AElfNetworkConfig : IAElfNetworkConfig
    {
        public List<string> Bootnodes { get; set; }
        
        public bool UseCustomBootnodes
        {
            get { return Bootnodes != null && Bootnodes.Any(); }
        }
    }
}