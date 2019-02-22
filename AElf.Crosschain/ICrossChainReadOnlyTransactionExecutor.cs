using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Crosschain
{
    public interface ICrossChainReadOnlyTransactionExecutor
    {
        Task<T> ReadByTransactionAsync<T>(int chainId, Address toAddress, string methodName,
            params object[] @params);
    }
}