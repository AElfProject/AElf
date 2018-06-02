using System.Collections.Generic;
using AElf.Network.Data;

namespace AElf.Network.Peers
{
    public static class Bootnodes
    {
        public static readonly List<NodeData> BootNodes = new List<NodeData>()
        {
            BootNode1,
            BootNode2,
            BootNode3
        };

        private static readonly NodeData BootNode1 = new NodeData()
        {
            IpAddress = "127.0.0.1",
            Port = 6800
        };
        
        private static readonly NodeData BootNode2 = new NodeData()
        {
            IpAddress = "127.0.0.1",
            Port = 6801
        };
        
        private static readonly NodeData BootNode3 = new NodeData()
        {
            IpAddress = "127.0.0.1",
            Port = 6802
        };
    }
}