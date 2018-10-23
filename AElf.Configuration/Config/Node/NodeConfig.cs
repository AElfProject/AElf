using AElf.Common;
using AElf.Cryptography.ECDSA;

namespace AElf.Configuration
{
    // ReSharper disable InconsistentNaming
    public class NodeConfig : ConfigBase<NodeConfig>
    {
        public bool FullNode { get; set; }
        public bool IsMiner { get; set; }
        public string ChainId { get; set; }
        //public string Coinbase { get; set; }
        //public string DataDir { get; set; }
        public bool IsChainCreator { get; set; }
        public bool ConsensusInfoGenerator { get; set; }
        public string ExecutorType { get; set; }
        public string NodeName { get; set; }
        public string NodeAccount { get; set; }
        public string NodeAccountPassword { get; set; }
        public ECKeyPair ECKeyPair { get; set; }
        public ConsensusKind ConsensusKind { get; set; }
    }
}