using AElf.Kernel.Types;
using AElf.Common;

namespace AElf.SmartContract
{
    public interface IAccountDataContext
    {
        ulong IncrementId { get; set; }
        Address Address { get; set; }
        
        Hash ChainId { get; set; }

//        Hash GetHash();
    }
}