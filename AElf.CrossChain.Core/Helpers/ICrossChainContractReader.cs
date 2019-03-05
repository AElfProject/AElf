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
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(ulong blockHeight);
        Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(ulong height);
        Task<ulong> GetBoundParentChainHeightAsync(ulong localChainHeight);

        Task<ulong> GetParentChainCurrentHeightAsync(IChainContext chainContext);

        Task<ulong> GetSideChainCurrentHeightAsync(int sideChainId, IChainContext chainContext);

        Task<int> GetParentChainIdAsync(IChainContext chainContext);

        Task<Dictionary<int, ulong>> GetSideChainIdAndHeightAsync(IChainContext chainContext);

        Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(ulong height);
        Task<Dictionary<int, ulong>> GetAllChainsIdAndHeightAsync(IChainContext chainContext);

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

        public Task<MerklePath> GetTxRootMerklePathInParentChainAsync(ulong blockHeight)
        {
            throw new System.NotImplementedException();
        }

        public Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(ulong height)
        {
            throw new System.NotImplementedException();
        }

        public Task<ulong> GetBoundParentChainHeightAsync(ulong localChainHeight)
        {
            throw new System.NotImplementedException();
        }

        public async Task<ulong> GetParentChainCurrentHeightAsync(IChainContext chainContext)
        {
            var readOnlyTransaction = await GenerateReadOnlyTransaction(CrossChainConsts.GetParentChainHeightMethodName);
            return await ReadByTransaction<ulong>(readOnlyTransaction, chainContext);
        }

        public async Task<ulong> GetSideChainCurrentHeightAsync(int sideChainId, IChainContext chainContext)
        {
            var readOnlyTransaction = await GenerateReadOnlyTransaction(CrossChainConsts.GetSideChainHeightMethodName,
                ChainHelpers.ConvertChainIdToBase58(sideChainId));
            return await ReadByTransaction<ulong>(readOnlyTransaction, chainContext);
        }

        public async Task<int> GetParentChainIdAsync(IChainContext chainContext)
        {
            var readOnlyTransaction = await GenerateReadOnlyTransaction(CrossChainConsts.GetParentChainIdMethodName);
            return await ReadByTransaction<int>(readOnlyTransaction, chainContext);
        }

        public async Task<Dictionary<int, ulong>> GetSideChainIdAndHeightAsync(IChainContext chainContext)
        {
            var readOnlyTransaction = await GenerateReadOnlyTransaction(CrossChainConsts.GetSideChainIdAndHeightMethodName);
            var dict = await ReadByTransaction<SideChainIdAndHeightDict>(readOnlyTransaction, chainContext);
            return new Dictionary<int, ulong>(dict.IdHeighDict);
        }

        public Task<IndexedSideChainBlockDataResult> GetIndexedSideChainBlockInfoResultAsync(ulong height)
        {
            throw new System.NotImplementedException();
        }

        public async Task<Dictionary<int, ulong>> GetAllChainsIdAndHeightAsync(IChainContext chainContext)
        {
            var readOnlyTransaction = await GenerateReadOnlyTransaction(CrossChainConsts.GetAllChainsIdAndHeightMethodName);
            var dict = await ReadByTransaction<SideChainIdAndHeightDict>(readOnlyTransaction, chainContext);
            return dict == null ? null : new Dictionary<int, ulong>(dict.IdHeighDict);
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