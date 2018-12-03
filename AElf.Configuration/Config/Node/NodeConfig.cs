using AElf.Common;
using AElf.Cryptography.ECDSA;

// ReSharper disable once CheckNamespace
namespace AElf.Configuration
{
    // ReSharper disable InconsistentNaming
    [ConfigFile(FileName = "node.json")]
    public class NodeConfig : ConfigBase<NodeConfig>
    {
        public bool IsMiner { get; set; }
        public bool IsChainCreator { get; set; }
        public string ExecutorType { get; set; }
        public string NodeName { get; set; }
        public string NodeAccount { get; set; }
        public string NodeAccountPassword { get; set; }
        public ECKeyPair ECKeyPair { get; set; }
        public ConsensusKind ConsensusKind { get; set; }
    }
}