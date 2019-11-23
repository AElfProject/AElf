using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class TransactionSizeFeeUnitProvider : ITransactionSizeFeeUnitPriceProvider
    {
        private readonly ITokenContractReaderFactory _tokenStTokenContractReaderFactory;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainBlockLinkService _chainBlockLinkService;

        public ILogger<TransactionSizeFeeUnitProvider> Logger { get; set; }

        private long? _unitPrice;
        private ConcurrentDictionary<BlockIndex, long> _forkCache = new ConcurrentDictionary<BlockIndex, long>();

        public TransactionSizeFeeUnitProvider(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService, 
            IChainBlockLinkService chainBlockLinkService)
        {
            _tokenStTokenContractReaderFactory = tokenStTokenContractReaderFactory;
            _blockchainService = blockchainService;
            _chainBlockLinkService = chainBlockLinkService;

            Logger = NullLogger<TransactionSizeFeeUnitProvider>.Instance;
        }

        public void SetUnitPrice(long unitPrice,BlockIndex blockIndex)
        {
            Logger.LogTrace($"Set tx size fee unit price: {unitPrice}");
            _forkCache[blockIndex] = unitPrice;
        }

        public async Task<long> GetUnitPriceAsync(IChainContext chainContext)
        {
            var keys = _forkCache.Keys.ToArray();
            if (keys.Length == 0) return await GetUnitPriceAsync();
            var minHeight = keys.Select(k => k.BlockHeight).Min();
            long? unitPrice = null;
            var blockIndex = new BlockIndex
            {
                BlockHash = chainContext.BlockHash,
                BlockHeight = chainContext.BlockHeight
            };
            do
            {
                if (_forkCache.TryGetValue(blockIndex, out var value))
                {
                    unitPrice = value;
                }

                var link = _chainBlockLinkService.GetCachedChainBlockLink(blockIndex.BlockHash);
                blockIndex.BlockHash = link?.PreviousBlockHash;
                blockIndex.BlockHeight--;
            } while (blockIndex.BlockHash != null && blockIndex.BlockHeight >= minHeight);

            if (unitPrice == null) unitPrice = await GetUnitPriceAsync();
            Logger.LogTrace($"Get tx size fee unit price: {unitPrice.Value}");
            return unitPrice.Value;
        }

        private async Task<long> GetUnitPriceAsync()
        {
            if (_unitPrice != null)
            {
//                Logger.LogTrace($"Get tx size fee unit price: {_unitPrice.Value}");
                return _unitPrice.Value;
            }

            var chain = await _blockchainService.GetChainAsync();

            var tokenStub = _tokenStTokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = chain.LastIrreversibleBlockHash,
                BlockHeight = chain.LastIrreversibleBlockHeight
            });

            _unitPrice = (await tokenStub.GetTransactionSizeFeeUnitPrice.CallAsync(new Empty()))?.Value ?? 0;

            return _unitPrice.Value;
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
                _unitPrice = _forkCache[blockIndex];
                _forkCache.TryRemove(blockIndex, out _);
            }
        }
    }
}