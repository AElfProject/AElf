namespace AElf.Kernel
{
    public interface IAccountDataContext
    {
        ulong IncreasementId { get; set; }
        IHash Address { get; set; }
        
        IHash ChainId { get; set; }
    }
}