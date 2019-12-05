using System.Threading.Tasks;
using Acs7;
using AElf.Kernel;

namespace AElf.CrossChain
{
    public interface ICrossChainIndexingDataValidationService
    {
        Task<bool> ValidateCrossChainIndexingData(CrossChainBlockData crossChainBlockData, Block block);
    }
}