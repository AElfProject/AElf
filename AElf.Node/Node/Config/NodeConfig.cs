using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.Kernel.Node.Config
{
    public class NodeConfig : INodeConfig
    {
        public bool FullNode { get; set; }
        public bool IsMiner { get; set; }
        public Hash ChainId { get; set; }
        public Hash Coinbase { get; set; }
        public string DataDir { get; set; }
        public bool IsChainCreator { get; set; }
        public bool ConsensusInfoGenerater { get; set; }
    }
}