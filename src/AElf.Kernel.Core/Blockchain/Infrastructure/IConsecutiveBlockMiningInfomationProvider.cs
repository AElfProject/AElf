namespace AElf.Kernel.Blockchain.Infrastructure
{
    public interface IConsecutiveBlockMiningInfomationProvider
    {
        long GetMaximalConsecutiveBlockMiningCount();
    }
}