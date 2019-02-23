using System.Threading.Tasks;
using AElf.Common;

namespace AElf.CrossChain
{
    public interface ICrossChainReadOnlyTransactionExecutor
    {
        Task<T> ReadByTransactionAsync<T>(int chainId, Address toAddress, string methodName, Hash previousBlockHash,
            ulong preBlockHeight, params object[] @params);
    }
}