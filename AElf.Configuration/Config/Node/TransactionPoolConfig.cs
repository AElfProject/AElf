using AElf.Cryptography.ECDSA;

namespace AElf.Configuration
{
    public class TransactionPoolConfig: ConfigBase<TransactionPoolConfig>
    {
 
        public ulong PoolLimitSize { get; set; }
 
        public uint TxLimitSize { get; set; }
 
        public ulong FeeThreshold { get; set; }
        
        public int Minimal { get; set; }
        
        public int Maximal { get; set; }
        
        public ECKeyPair EcKeyPair { get; set; }
         
        public TransactionPoolConfig()
        {
            PoolLimitSize = 1024 * 1024;
            TxLimitSize = 1024 * 20;
            Minimal = 1;
            Maximal = 1024;
            FeeThreshold = 0;
        }
    }
}