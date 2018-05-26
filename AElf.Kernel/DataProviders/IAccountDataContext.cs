namespace AElf.Kernel
{
    public interface IAccountDataContext
    {
        ulong IncrementId { get; set; }
        Hash Address { get; set; }
        
        Hash ChainId { get; set; }

        Hash GetHash();
    }
}