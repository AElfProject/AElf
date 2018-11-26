using AElf.Cryptography.ECDSA;

namespace AElf.Configuration
{
    [ConfigFile(FileName = "transaction-pool.json")]
    public class TransactionPoolConfig: ConfigBase<TransactionPoolConfig>
    {
        public ulong PoolLimitSize { get; set; }
 
        public uint TxLimitSize { get; set; }
 
        public ulong FeeThreshold { get; set; }
        
        public int Minimal { get; set; }
        
        public int Maximal { get; set; }
        
        public ECKeyPair EcKeyPair { get; set; }
    }
}