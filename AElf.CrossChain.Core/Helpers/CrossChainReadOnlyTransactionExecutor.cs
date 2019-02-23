using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.CrossChain
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

        public async Task<T> ReadByTransactionAsync<T>(int chainId, Address toAddress, string methodName, Hash previousBlockHash,
            ulong preBlockHeight, params object[] @params)
        {
            var transaction = await GenerateReadOnlyTransaction(toAddress: toAddress, methodName: methodName, @params: @params);
            var trace = await ExecuteReadOnlyTransaction(chainId: chainId, transaction: transaction, 
                previousBlockHash: previousBlockHash, preBlockHeight: preBlockHeight);
            return (T) trace.RetVal.Data.DeserializeToType(type: typeof(T));
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

        private async Task<TransactionTrace> ExecuteReadOnlyTransaction(int chainId, Transaction transaction,
            Hash previousBlockHash, ulong preBlockHeight)
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
            var chainContext = new ChainContext()
            {
                ChainId = chainId,
                BlockHash = previousBlockHash,
                BlockHeight = preBlockHeight
            };
            var executive =
                await _smartContractExecutiveService.GetExecutiveAsync(chainId, chainContext, transaction.To);
            await executive.SetTransactionContext(txCtxt).Apply();
            return trace;
        }      
        
    }
}