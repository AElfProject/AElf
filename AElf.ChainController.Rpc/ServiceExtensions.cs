using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using Svc = AElf.ChainController.Rpc.ChainControllerRpcService;

namespace AElf.ChainController.Rpc
{
    internal static class ServiceExtensions
    {
        internal static async Task<IMessage> GetContractAbi(this Svc s, Hash address)
        {
            return await s.SmartContractService.GetAbiAsync(address);
        }

        internal static async Task<ITransaction> GetTransaction(this Svc s, Hash txId)
        {
            if (s.TxPoolService.TryGetTx(txId, out var tx))
            {
                return tx;
            }

            return await s.TransactionManager.GetTransaction(txId);
        }

        internal static async Task<TransactionResult> GetTransactionResult(this Svc s, Hash txHash)
        {
            var res = await s.TransactionResultService.GetResultAsync(txHash);
            return res;
        }

        internal static Hash GetGenesisContractHash(this Svc s, SmartContractType contractType)
        {
            return s.ChainCreationService.GenesisContractHash(s.NodeConfig.ChainId, contractType);
        }

        internal static async Task<IEnumerable<string>> GetTransactionParameters(this Svc s, ITransaction tx)
        {
            return await s.SmartContractService.GetInvokingParams(tx);
        }
    }
}