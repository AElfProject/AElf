using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.CrossChain
{
    public interface ICrossChainContractReader
    {
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(long blockHeight);
        Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(long height);
        Task<long> GetBoundParentChainHeightAsync(long localChainHeight);

        Task<long> GetParentChainCurrentHeightAsync(IChainContext chainContext);

        Task<long> GetSideChainCurrentHeightAsync(int sideChainId, IChainContext chainContext);

        Task<int> GetParentChainIdAsync(IChainContext chainContext);

        Task<Dictionary<int, long>> GetSideChainIdAndHeightAsync(IChainContext chainContext);

        Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(long height);
        Task<Dictionary<int, long>> GetAllChainsIdAndHeightAsync(IChainContext chainContext);

        Task<CrossChainBlockData> GetCrossChainBlockDataAsync(IChainContext chainContext);
    }

    public class CrossChainContractReader : ICrossChainContractReader
    {
        private readonly IAccountService _accountService;
        private IChainManager _chainManager;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        public CrossChainContractReader(IChainManager chainManager, IAccountService accountService, 
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _chainManager = chainManager;
            _accountService = accountService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        }

        public Task<MerklePath> GetTxRootMerklePathInParentChainAsync(long blockHeight)
        {
            throw new System.NotImplementedException();
        }

        public Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(long height)
        {
            throw new System.NotImplementedException();
        }

        public Task<long> GetBoundParentChainHeightAsync(long localChainHeight)
        {
            throw new System.NotImplementedException();
        }

        public async Task<long> GetParentChainCurrentHeightAsync(IChainContext chainContext)
        {
            var readOnlyTransaction = await GenerateReadOnlyTransaction(CrossChainConsts.GetParentChainHeightMethodName);
            return await ReadByTransaction<long>(readOnlyTransaction, chainContext);
        }

        public async Task<long> GetSideChainCurrentHeightAsync(int sideChainId, IChainContext chainContext)
        {
            var readOnlyTransaction = await GenerateReadOnlyTransaction(CrossChainConsts.GetSideChainHeightMethodName,
                sideChainId);
            return await ReadByTransaction<long>(readOnlyTransaction, chainContext);
        }

        public async Task<int> GetParentChainIdAsync(IChainContext chainContext)
        {
            var readOnlyTransaction = await GenerateReadOnlyTransaction(CrossChainConsts.GetParentChainIdMethodName);
            return await ReadByTransaction<int>(readOnlyTransaction, chainContext);
        }

        public async Task<Dictionary<int, long>> GetSideChainIdAndHeightAsync(IChainContext chainContext)
        {
            var readOnlyTransaction = await GenerateReadOnlyTransaction(CrossChainConsts.GetSideChainIdAndHeightMethodName);
            var dict = await ReadByTransaction<SideChainIdAndHeightDict>(readOnlyTransaction, chainContext);
            return new Dictionary<int, long>(dict.IdHeighDict);
        }

        public Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(long height)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Dictionary<int, long>> GetAllChainsIdAndHeightAsync(IChainContext chainContext)
        {
            var readOnlyTransaction = await GenerateReadOnlyTransaction(CrossChainConsts.GetAllChainsIdAndHeightMethodName);
            var dict = await ReadByTransaction<SideChainIdAndHeightDict>(readOnlyTransaction, chainContext);
            return dict == null ? null : new Dictionary<int, long>(dict.IdHeighDict);
        }

        public async Task<CrossChainBlockData> GetCrossChainBlockDataAsync(IChainContext chainContext)
        {
            var readOnlyTransaction =
                await GenerateReadOnlyTransaction(CrossChainConsts.GetIndexedCrossChainBlockDataByHeight,
                    chainContext.BlockHeight);
            return await ReadByTransaction<CrossChainBlockData>(readOnlyTransaction, chainContext);
        }

        Address CrossChainContractMethodAddress => ContractHelpers.GetCrossChainContractAddress(_chainManager.GetChainId());

        private async Task<Transaction> GenerateReadOnlyTransaction(string methodName, params object[] @params)
        {
            var transaction =  new Transaction
            {
                From = await _accountService.GetAccountAsync(),
                To = CrossChainContractMethodAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(@params))
            };
            return transaction;
        }

        private async Task<T> ReadByTransaction<T>(Transaction readOnlyTransaction, IChainContext chainContext)
        {
            return await ReadByTransactionAsync<T>(readOnlyTransaction, chainContext);
        }
        
        private async Task<T> ReadByTransactionAsync<T>(Transaction readOnlyTransaction, IChainContext chainContext)
        {
            var trace =
                await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, readOnlyTransaction, DateTime.UtcNow);
            
            if(trace.IsSuccessful())
                return (T) trace.RetVal.Data.DeserializeToType(type: typeof(T));
            return default(T);
        }
    }
}