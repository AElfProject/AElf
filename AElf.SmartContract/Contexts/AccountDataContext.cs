using AElf.Common;

namespace AElf.SmartContract
{
    public class AccountDataContext : IAccountDataContext
    {
        public ulong IncrementId { get; set; }
        public Address Address { get; set; }
        public int ChainId { get; set; }

//        public Hash GetHash()
//        {
//            return HashExtensions.CalculateHashOfHashList(ChainId, Address);
//        }
    }
}