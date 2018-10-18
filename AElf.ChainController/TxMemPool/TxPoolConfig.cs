using AElf.ChainController.TxMemPool;
using AElf.Cryptography.ECDSA;
using AElf.Common;

namespace AElf.ChainController.TxMemPool
 {
     public class TxPoolConfig : ITxPoolConfig
     {
         public static readonly TxPoolConfig Default = new TxPoolConfig
         {
             PoolLimitSize = 1024 * 1024,
             TxLimitSize = 1024 * 20,
             Minimal = 1,
             Maximal = 1024,
             FeeThreshold = 0
         };

         public ulong PoolLimitSize { get; set; }
 
         public uint TxLimitSize { get; set; }
 
         public ulong FeeThreshold { get; set; }
        
         //public ulong EntryThreshold { get; set; }
         
         public ECKeyPair EcKeyPair { get; set; }
         
         public int Minimal { get; set; }
         public int Maximal { get; set; }
     }
 }