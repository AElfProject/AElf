using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;
using AElf.Types;
using AElf.Contracts.Configuration;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Miner.Application
{
    public class BlockTransactionLimitProvider : IBlockTransactionLimitProvider, ISingletonDependency
    {
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        public ILogger<BlockTransactionLimitProvider> Logger { get; set; }

        private Address ConfigurationContractAddress => _smartContractAddressService.GetAddressByContractName(
            ConfigurationSmartContractAddressNameProvider.Name);

        private readonly ConcurrentDictionary<BlockIndex, int> _forkCache =
            new ConcurrentDictionary<BlockIndex, int>();

        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        private int _limit = -1;

        public BlockTransactionLimitProvider(ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _chainBlockLinkService = chainBlockLinkService;
            Logger = NullLogger<BlockTransactionLimitProvider>.Instance;
        }


        public async Task InitAsync()
        {
            if (_limit == -1)
            {
                // Call ConfigurationContract GetBlockTransactionLimit()
                try
                {
                    var result = await CallContractMethodAsync(
                        ConfigurationContractAddress,
                        nameof(ConfigurationContainer.ConfigurationStub.GetBlockTransactionLimit),
                        new Empty());
                    _limit = Int32Value.Parser.ParseFrom(result).Value;
                    Logger.LogInformation($"Get blockTransactionLimit: {_limit} by ConfigurationStub");
                }
                catch (InvalidOperationException e)
                {
                    Logger.LogWarning($"Invalid ConfigurationContractAddress :{e.Message}");
                    _limit = 0;
                }
            }
        }

        public int GetLimit(IChainContext chainContext)
        {
            var keys = _forkCache.Keys.ToArray();
            if (keys.Length == 0) return _limit;
            var minHeight = keys.Select(k => k.BlockHeight).Min();
            int? limit = null;
            var blockIndex = new BlockIndex
            {
                BlockHash = chainContext.BlockHash,
                BlockHeight = chainContext.BlockHeight
            };
            do
            {
                if (_forkCache.TryGetValue(blockIndex, out var value))
                {
                    limit = value;
                    break;
                }

                var link = _chainBlockLinkService.GetCachedChainBlockLink(blockIndex.BlockHash);
                blockIndex.BlockHash = link?.PreviousBlockHash;
                blockIndex.BlockHeight--;
            } while (blockIndex.BlockHash != null && blockIndex.BlockHeight >= minHeight);

            return limit ?? _limit;
        }

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            foreach (var blockIndex in blockIndexes)
            {
                if(!_forkCache.TryGetValue(blockIndex, out _)) continue;
                _forkCache.TryRemove(blockIndex, out _);
            }
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            foreach (var blockIndex in blockIndexes)
            {
                if(!_forkCache.TryGetValue(blockIndex,out _)) continue;
                var limit = _forkCache[blockIndex];
                _limit = limit;
                _forkCache.TryRemove(blockIndex, out _);
            }
        }

        public void SetLimit(int limit, BlockIndex blockIndex)
        {
            _forkCache[blockIndex] = limit;
        }

        #region GetLimit

        private async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            IMessage input)
        {
            var tx = new Transaction
            {
                From = FromAddress,
                To = contractAddress,
                MethodName = methodName,
                Params = input.ToByteString(),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
            var chain = await _blockchainService.GetChainAsync();
            var transactionTrace = await _transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
            {
                BlockHash = chain.LastIrreversibleBlockHash,
                BlockHeight = chain.LastIrreversibleBlockHeight
            }, tx, TimestampHelper.GetUtcNow());

            return transactionTrace.ReturnValue;
        }

        #endregion
    }
}