using AElf.Kernel.KernelAccount;

 namespace AElf.Kernel
 {
     public class ChainContext : IChainContext
     {
         public void Initialize(IChain chain)
         {
             ChainId = chain.Id;
             // TODO: initialize SmartContractZero
         }
 
         public ISmartContractZero SmartContractZero { get; }
         public IHash<IChain> ChainId { get; private set; }
     }
 }