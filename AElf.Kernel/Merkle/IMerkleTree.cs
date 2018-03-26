namespace AElf.Kernel.Merkle
 {
     public interface IMerkleTree
     {
         Hash ComputeRootHash();
         void AddNode(IHash hash);
     }
 }