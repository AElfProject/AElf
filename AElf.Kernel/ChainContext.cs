using AElf.Kernel.KernelAccount;

 namespace AElf.Kernel
 {
     public class ChainContext : IChainContext
     {
         public ChainContext(ISmartContractZero smartContractZero, Hash chainId)
         {
             SmartContractZero = smartContractZero;
             ChainId = chainId;
         }

 
         public ISmartContractZero SmartContractZero { get; }
         public Hash ChainId { get; }
     }
 }