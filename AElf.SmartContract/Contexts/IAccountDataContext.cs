using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public interface IAccountDataContext
    {
        ulong IncrementId { get; set; }
        Hash Address { get; set; }
        
        Hash ChainId { get; set; }

        Hash GetHash();
    }
}