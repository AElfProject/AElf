namespace AElf.Kernel
{
    public class AccountDataContext : IAccountDataContext
    {
        public ulong IncreasementId { get; set; }
        public IHash<IAccount> Address { get; set; }
        public IHash<IChain> ChainId { get; set; }
    }
}