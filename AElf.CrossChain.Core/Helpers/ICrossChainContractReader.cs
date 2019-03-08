using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.CrossChain
{
    public interface ICrossChainContractReader
    {
        Task<MerklePath> GetTxRootMerklePathInParentChainAsync(long blockHeight);
        Task<ParentChainBlockData> GetBoundParentChainBlockInfoAsync(long height);
        Task<long> GetBoundParentChainHeightAsync(long localChainHeight);

        Task<long> GetParentChainCurrentHeightAsync(Hash blockHash, long blockHeight);

        Task<long> GetSideChainCurrentHeightAsync(int sideChainId, Hash blockHash, long blockHeight);

        Task<int> GetParentChainIdAsync(Hash blockHash, long blockHeight);

        Task<Dictionary<int, long>> GetSideChainIdAndHeightAsync(Hash blockHash, long blockHeight);

        Task<Dictionary<int, long>> GetAllChainsIdAndHeightAsync(Hash blockHash, long blockHeight);

        Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash blockHash, long blockHeight);
    }

    public class CrossChainContractReader : ICrossChainContractReader, ITransientDependency
    {
        private IChainManager _chainManager;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        public CrossChainContractReader(IChainManager chainManager, 
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService, 
            ISmartContractAddressService smartContractAddressService)
        {
            _chainManager = chainManager;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _smartContractAddressService = smartContractAddressService;
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

        public async Task<long> GetParentChainCurrentHeightAsync(Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction = GenerateReadOnlyTransaction(CrossChainConsts.GetParentChainHeightMethodName);
            return await ReadByTransactionAsync<long>(readOnlyTransaction, blockHash, blockHeight);
        }

        public async Task<long> GetSideChainCurrentHeightAsync(int sideChainId, Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction = GenerateReadOnlyTransaction(CrossChainConsts.GetSideChainHeightMethodName,
                sideChainId);
            return await ReadByTransactionAsync<long>(readOnlyTransaction, blockHash, blockHeight);
        }

        public async Task<int> GetParentChainIdAsync(Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction = GenerateReadOnlyTransaction(CrossChainConsts.GetParentChainIdMethodName);
            return await ReadByTransactionAsync<int>(readOnlyTransaction, blockHash, blockHeight);
        }

        public async Task<Dictionary<int, long>> GetSideChainIdAndHeightAsync(Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction = GenerateReadOnlyTransaction(CrossChainConsts.GetSideChainIdAndHeightMethodName);
            var dict = await ReadByTransactionAsync<SideChainIdAndHeightDict>(readOnlyTransaction, blockHash, blockHeight);
            return new Dictionary<int, long>(dict.IdHeighDict);
        }

        public async Task<Dictionary<int, long>> GetAllChainsIdAndHeightAsync(Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction = GenerateReadOnlyTransaction(CrossChainConsts.GetAllChainsIdAndHeightMethodName);
            var dict = await ReadByTransactionAsync<SideChainIdAndHeightDict>(readOnlyTransaction, blockHash, blockHeight);
            return dict == null ? null : new Dictionary<int, long>(dict.IdHeighDict);
        }

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction =
                GenerateReadOnlyTransaction(CrossChainConsts.GetIndexedCrossChainBlockDataByHeight,
                    blockHeight);
            return await ReadByTransactionAsync<CrossChainBlockData>(readOnlyTransaction, blockHash, blockHeight);
        }

        private Address CrossChainContractMethodAddress =>
            _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);
        
        private Transaction GenerateReadOnlyTransaction(string methodName, params object[] @params)
        {
            var transaction =  new Transaction
            {
                From = Address.Generate(), // this is not good enough, only used for temporary.
                To = CrossChainContractMethodAddress,
                MethodName = methodName,
                Params = ByteString.CopyFrom(ParamsPacker.Pack(@params))
            };
            return transaction;
        }

        private async Task<T> ReadByTransactionAsync<T>(Transaction readOnlyTransaction, Hash blockHash, long blockHeight)
        {
            var chainContext = GenerateChainContext(blockHash, blockHeight);
            var trace =
                await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, readOnlyTransaction, DateTime.UtcNow);
            
            if(trace.IsSuccessful())
                return (T)trace.RetVal.Data.DeserializeToType(typeof(T));
            return default(T);
        }
        
        private IChainContext GenerateChainContext(Hash blockHash, long blockHeight)
        {
            var chainContext = new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            };
            return chainContext;
        }
    }
}