namespace AElf.Kernel
{
    public interface IAccountDataContext
    {
        ulong IncreasementId { get; set; }
        IHash<IAccount> Address { get; set; }
        
        IHash<IChain> ChainId { get; set; }
    }
}