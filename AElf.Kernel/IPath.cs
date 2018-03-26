namespace AElf.Kernel
{
    /// <summary>
    /// There exists a Pointer class in System.Reflection namespace,
    /// so use "Path" to avoid the misleading.
    /// </summary>
    public interface IPath
    {
        Path SetChainHash(IHash<IChain> chainHash);
        Path SetBlockHash(IHash<IBlock> blockHash);
        Path SetAccount(IHash<IAccount> accountAddress);
        Path SetItemName(string itemName);
        IHash<IPath> GetPointerHash();
        IHash<IPath> GetPathHash();
    }
}