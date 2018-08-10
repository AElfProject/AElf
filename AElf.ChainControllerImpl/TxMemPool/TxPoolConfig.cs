using AElf.Cryptography.ECDSA;
using AElf.Kernel;
 
 namespace AElf.ChainController
 {
     public class TxPoolConfig : ITxPoolConfig
     {
         public static readonly TxPoolConfig Default = new TxPoolConfig
         {
             PoolLimitSize = 1024 * 1024,
             TxLimitSize = 1024 * 20,
             ChainId = Hash.Generate(),
             Minimal = 1,
             Maximal = 1024,
             FeeThreshold = 0
         };
         
         public Hash ChainId { get; set; } 
 
         public ulong PoolLimitSize { get; set; }
 
         public uint TxLimitSize { get; set; }
 
         public ulong FeeThreshold { get; set; }
        
         //public ulong EntryThreshold { get; set; }
         
         public ECKeyPair EcKeyPair { get; set; }
         
         public ulong Minimal { get; set; }
         public ulong Maximal { get; set; }
     }
 }