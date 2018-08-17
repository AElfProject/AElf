namespace AElf.Configuration
{
    public class NodeConfig : ConfigBase<NodeConfig>
    {
        public bool FullNode { get; set; }
        public bool IsMiner { get; set; }
        public string ChainId { get; set; }
        //public string Coinbase { get; set; }
        public string DataDir { get; set; }
        public bool IsChainCreator { get; set; }
        public bool ConsensusInfoGenerater { get; set; }
    }
}