namespace AElf.Configuration
{
    public class NodeConfig : INodeConfig
    {
        public bool FullNode { get; set; }
        public bool IsMiner { get; set; }
        public byte[] ChainId { get; set; }
        public byte[] Coinbase { get; set; }
        public string DataDir { get; set; }
        public bool IsChainCreator { get; set; }
        public bool ConsensusInfoGenerater { get; set; }
    }
}