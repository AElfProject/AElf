namespace AElf.Kernel
{
    public interface IHashProvider<T>
    {
        IHash<T> GetHash();
    }
}