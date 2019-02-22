using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Crosschain
{
    public class CrossChainReadOnlyTransactionExecutor : ICrossChainReadOnlyTransactionExecutor
    {
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;
        private readonly IAccountService _accountService;

        public CrossChainReadOnlyTransactionExecutor(ISmartContractExecutiveService smartContractExecutiveService, 
            IAccountService accountService)
        {
            _smartContractExecutiveService = smartContractExecutiveService;
            _accountService = accountService;
        }

        public Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResult(ulong height)
        {
            throw new System.NotImplementedException();
        }

        public async Task<T> ReadByTransaction<T>(int chainId, Address toAddress, string methodName,
            params object[] @params)
        {
            var transaction = await GenerateReadOnlyTransaction(toAddress, methodName, @params);
            var trace = await ExecuteReadOnlyTransaction(chainId, transaction);
            return (T) trace.RetVal.Data.DeserializeToType(typeof(T));
        }
        
        private async Task<Transaction> GenerateReadOnlyTransaction( Address toAddress, string methodName, 
            params object[] @params)
        {
            var transaction =  new Transaction
            {
                From = await _accountService.GetAccountAsync(),
                To = toAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(@params))
            };
            return transaction;
        }

        private async Task<TransactionTrace> ExecuteReadOnlyTransaction(int chainId, Transaction transaction)
        {
            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash(),
                RetVal = new RetVal()
            };
            var txCtxt = new TransactionContext
            {
                Transaction = transaction,
                Trace = trace
            };
            var executive =
                await _smartContractExecutiveService.GetExecutiveAsync(chainId, transaction.To);
            await executive.SetTransactionContext(txCtxt).Apply();
            return trace;
        }      
        
    }
}