using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.Types;

namespace AElf.CrossChain.Indexing.Application
{
    public interface ICrossChainIndexingDataValidationService
    {
        Task<bool> ValidateCrossChainIndexingDataAsync(CrossChainBlockData crossChainBlockData, Hash blockHash,
            long blockHeight);
    }
}