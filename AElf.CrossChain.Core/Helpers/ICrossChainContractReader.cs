using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
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
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        public CrossChainContractReader(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService, 
            ISmartContractAddressService smartContractAddressService)
        {
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
            var readOnlyTransaction = GenerateReadOnlyTransaction(
                nameof(CrossChainContractMethodNames.GetParentChainHeight),
                new Empty());
            return (await ReadByTransactionAsync<SInt64Value>(readOnlyTransaction, blockHash, blockHeight))?.Value ?? 0;
        }

        

        public async Task<long> GetSideChainCurrentHeightAsync(int sideChainId, Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction = GenerateReadOnlyTransaction(
                nameof(CrossChainContractMethodNames.GetSideChainHeight),
                new SInt32Value()
                {
                    Value = sideChainId
                });
            return (await ReadByTransactionAsync<SInt64Value>(readOnlyTransaction, blockHash, blockHeight))?.Value ?? 0;
        }

        public async Task<int> GetParentChainIdAsync(Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction = GenerateReadOnlyTransaction(
                nameof(CrossChainContractMethodNames.GetParentChainId),
                new Empty());
            return (await ReadByTransactionAsync<SInt32Value>(readOnlyTransaction, blockHash, blockHeight))?.Value ?? 0;
        }

        public async Task<Dictionary<int, long>> GetSideChainIdAndHeightAsync(Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction = GenerateReadOnlyTransaction(
                nameof(CrossChainContractMethodNames.GetSideChainIdAndHeight),
                new Empty());
            var dict = await ReadByTransactionAsync<SideChainIdAndHeightDict>(readOnlyTransaction, blockHash, blockHeight);
            return new Dictionary<int, long>(dict.IdHeightDict);
        }

        public async Task<Dictionary<int, long>> GetAllChainsIdAndHeightAsync(Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction = GenerateReadOnlyTransaction(
                nameof(CrossChainContractMethodNames.GetAllChainsIdAndHeight),
                new Empty());
            var dict = await ReadByTransactionAsync<SideChainIdAndHeightDict>(readOnlyTransaction, blockHash, blockHeight);
            return dict == null ? null : new Dictionary<int, long>(dict.IdHeightDict);
        }

        public async Task<CrossChainBlockData> GetIndexedCrossChainBlockDataAsync(Hash blockHash, long blockHeight)
        {
            var readOnlyTransaction =
                GenerateReadOnlyTransaction(
                    nameof(CrossChainContractMethodNames.GetIndexedCrossChainBlockDataByHeight),
                    new SInt64Value(){Value = blockHeight});
            return await ReadByTransactionAsync<CrossChainBlockData>(readOnlyTransaction, blockHash, blockHeight);
        }

        private Address CrossChainContractMethodAddress =>
            _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);
        
        private Transaction GenerateReadOnlyTransaction(string methodName, IMessage input)
        {
            var transaction =  new Transaction
            {
                From = Address.Generate(), // this is not good enough, only used for temporary.
                To = CrossChainContractMethodAddress,
                MethodName = methodName,
                Params = input.ToByteString()
            };
            return transaction;
        }

        private async Task<T> ReadByTransactionAsync<T>(Transaction readOnlyTransaction, Hash blockHash, long blockHeight)
        where T: IMessage<T>, new()
        {
            var chainContext = GenerateChainContext(blockHash, blockHeight);
            var trace =
                await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, readOnlyTransaction, DateTime.UtcNow);

            if (trace.IsSuccessful())
            {
                var obj = new T();
                obj.MergeFrom(trace.ReturnValue);
                return obj;
            }
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
    
    public enum CrossChainContractMethodNames
    {
        GetParentChainHeight,
        GetSideChainHeight,
        GetParentChainId,
        GetSideChainIdAndHeight,
        GetAllChainsIdAndHeight,
        GetIndexedCrossChainBlockDataByHeight,
            
    }
}