using System.Threading.Tasks;
using Acs7;
using AElf.Types;

namespace AElf.CrossChain.Indexing.Application
{
    public interface ICrossChainIndexingDataValidationService
    {
        Task<bool> ValidateCrossChainIndexingData(CrossChainBlockData crossChainBlockData, Hash blockHash,
            long blockHeight);
    }
}