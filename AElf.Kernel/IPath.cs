namespace AElf.Kernel
{
    /// <summary>
    /// There exists a Pointer class in System.Reflection namespace,
    /// so use "Path" to avoid the misleading.
    /// </summary>
    public interface IPath
    {
        Path SetChainHash(IHash chainHash);
        Path SetBlockHash(IHash blockHash);
        Path SetAccount(IHash accountAddress);
        Path SetItemName(string itemName);
        IHash GetPointerHash();
        IHash GetPathHash();
    }
}