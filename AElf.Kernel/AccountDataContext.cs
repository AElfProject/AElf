namespace AElf.Kernel
{
    public class AccountDataContext : IAccountDataContext
    {
        public ulong IncreasementId { get; set; }
        public IHash Address { get; set; }
        public IHash ChainId { get; set; }
    }
}