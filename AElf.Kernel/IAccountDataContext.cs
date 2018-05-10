namespace AElf.Kernel
{
    public interface IAccountDataContext
    {
        ulong IncreasementId { get; set; }
        Hash Address { get; set; }
        
        Hash ChainId { get; set; }

        Hash GetHash();
    }
}