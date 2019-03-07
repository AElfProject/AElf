using System.Collections.Generic;
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
    public interface ICrossChainReadOnlyTransactionExecutor
    {
        Task<T> ReadByTransactionAsync<T>(Address toAddress, string methodName, Hash previousBlockHash,
            long preBlockHeight, params object[] @params);
    }
    
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

        public Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResult(long height)
        {
            throw new System.NotImplementedException();
        }

        public async Task<T> ReadByTransactionAsync<T>(Address toAddress, string methodName, Hash previousBlockHash,
            long preBlockHeight, params object[] @params)
        {
            var transaction = await GenerateReadOnlyTransaction(toAddress: toAddress, methodName: methodName, @params: @params);
            var trace = await ExecuteReadOnlyTransaction(transaction: transaction, 
                previousBlockHash: previousBlockHash, preBlockHeight: preBlockHeight);
            if(trace.IsSuccessful())
                return (T) trace.RetVal.Data.DeserializeToType(type: typeof(T));
            return default(T);
        }
        
        private async Task<Transaction> GenerateReadOnlyTransaction(Address toAddress, string methodName, 
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

        //TODO: move ExecuteReadOnlyTransaction to smart contract execution project
        private async Task<TransactionTrace> ExecuteReadOnlyTransaction(Transaction transaction,
            Hash previousBlockHash, long preBlockHeight)
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
                BlockHash = previousBlockHash,
                BlockHeight = preBlockHeight
            };
            var executive =
                await _smartContractExecutiveService.GetExecutiveAsync( chainContext, transaction.To);
            await executive.SetTransactionContext(txCtxt).Apply();
            return trace;
        }
    }
}