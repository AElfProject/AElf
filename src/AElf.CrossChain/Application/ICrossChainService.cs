using System.Threading.Tasks;
using AElf.Types;

namespace AElf.CrossChain.Application
{
    public interface ICrossChainService
    {
        Task FinishInitialSyncAsync();
        Task UpdateCrossChainDataWithLibAsync(Hash blockHash, long blockHeight);
    }
}