using System.Threading.Tasks;

namespace AElf.Kernel.Miner.Application
{
    public interface IBlockTransactionLimitProvider
    {
        Task<int> GetLimitAsync();
        void SetLimit(int limit);
    }
}