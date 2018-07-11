using AElf.Kernel.Types;
using AElf.Kernel;

namespace AElf.SmartContract
{
    public class AccountDataContext : IAccountDataContext
    {
        public ulong IncrementId { get; set; }
        public Hash Address { get; set; }
        public Hash ChainId { get; set; }

        public Hash GetHash()
        {
            return ChainId.CalculateHashWith(Address);
        }
    }
}