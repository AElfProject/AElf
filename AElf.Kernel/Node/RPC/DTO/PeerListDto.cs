using System.Collections.Generic;
using AElf.Kernel.Node.Network.Data;

namespace AElf.Node.RPC.DTO
{
    public class PeerListDto
    {
        public List<NodeData> PeerList { get; set; }
    }
}