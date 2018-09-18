using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;

namespace AElf.Management.Services
{
    public class TransactionService:ITransactionService
    {
        public ulong GetPoolSize(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_txpool_size";

            var state = HttpRequestHelper.Request<JsonRpcResult<TxPoolSizeResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.CurrentTransactionPoolSize;
        }
    }
}