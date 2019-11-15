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
        public ILogger<BlockTransactionLimitProvider> Logger { get; set; }

        private Address ConfigurationContractAddress => _smartContractAddressService.GetAddressByContractName(
            ConfigurationSmartContractAddressNameProvider.Name);

        private readonly ConcurrentDictionary<Hash, int> _forkCache =
            new ConcurrentDictionary<Hash, int>();
        private Address FromAddress { get; } = Address.FromBytes(new byte[] { }.ComputeHash());

        private int _limit = -1;

        public BlockTransactionLimitProvider(ISmartContractAddressService smartContractAddressService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IBlockchainService blockchainService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            Logger = NullLogger<BlockTransactionLimitProvider>.Instance;
        }

        public async Task InitAsync()
        {
            var chain = await _blockchainService.GetChainAsync();
            if (_limit == -1)
            {
                // Call ConfigurationContract GetBlockTransactionLimit()
                try
                {
                    var result = await CallContractMethodAsync(new ChainContext{BlockHash = chain.LastIrreversibleBlockHash,BlockHeight = chain.LastIrreversibleBlockHeight}, 
                        ConfigurationContractAddress,
                        nameof(ConfigurationContainer.ConfigurationStub.GetBlockTransactionLimit),
                        new Empty());
                    _limit = Int32Value.Parser.ParseFrom(result).Value;
                    Logger.LogInformation($"Init blockTransactionLimit: {_limit} by ConfigurationStub");
                }
                catch (InvalidOperationException e)
                {
                    Logger.LogWarning($"Invalid ConfigurationContractAddress :{e.Message}");
                }
            }
        }

        public int GetLimit()
        {
            return _limit == -1 ? 0 : _limit;
        }

        public void RemoveForkCache(List<Hash> blockHashes)
        {
            foreach (var blockHash in blockHashes)
            {
                if (!_forkCache.TryGetValue(blockHash, out _)) continue;
                _forkCache.TryRemove(blockHash, out _);
            }
        }

        public void SetIrreversedCache(List<Hash> blockHashes)
        {
            foreach (var blockHash in blockHashes)
            {
                SetIrreversedCache(blockHash);
            }
        }

        public void SetIrreversedCache(Hash blockHash)
        {
            if (!_forkCache.TryGetValue(blockHash, out var limit)) return;
            _limit = limit;
            _forkCache.TryRemove(blockHash, out _);
            Logger.LogInformation($"BlockTransactionLimit has been changed to {_limit}");
        }

        public void SetLimit(int limit,Hash blockHash)
        {
            _forkCache[blockHash] = limit;
        }

        #region GetLimit

        private async Task<ByteString> CallContractMethodAsync(IChainContext chainContext, Address contractAddress,
            string methodName, IMessage input)
        {
            var tx = new Transaction
            {
                From = FromAddress,
                To = contractAddress,
                MethodName = methodName,
                Params = input.ToByteString(),
                Signature = ByteString.CopyFromUtf8("SignaturePlaceholder")
            };
            var transactionTrace =
                await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx, TimestampHelper.GetUtcNow());

            return transactionTrace.ReturnValue;
        }

        #endregion
    }
}