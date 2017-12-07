namespace AElf.Kernel
{
    public interface IMerkleTree<T>
    {
        IHash<IMerkleTree<T>> ComputeRootHash();
        void AddNode(IHash<T> hash);
    }
} 