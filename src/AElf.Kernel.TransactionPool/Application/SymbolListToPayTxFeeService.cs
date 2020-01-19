using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;
using AElf.Kernel.TransactionPool.Infrastructure;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class SymbolListToPayTxFeeService : ISymbolListToPayTxFeeService, ITransientDependency
    {
        private readonly ISymbolListToPayTxFeeCacheProvider _cacheProvider;
        private readonly ITokenContractReaderFactory _tokenStTokenContractReaderFactory;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        public ILogger<SymbolListToPayTxFeeService> Logger { get; set; }

        public SymbolListToPayTxFeeService(ISymbolListToPayTxFeeCacheProvider cacheProvider,
            IChainBlockLinkService chainBlockLinkService,
            ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService)
        {
            _cacheProvider = cacheProvider;
            _chainBlockLinkService = chainBlockLinkService;
            _tokenStTokenContractReaderFactory = tokenStTokenContractReaderFactory;
            _blockchainService = blockchainService;
            Logger = new NullLogger<SymbolListToPayTxFeeService>();
        }

        public async Task<List<AvailableTokenInfoInCache>> GetExtraAcceptedTokensInfoAsync(
            IChainContext chainContext)
        {
            var keys = _cacheProvider.GetForkCacheKeys();
            if (keys.Length == 0)
                return await GetExtraAcceptedTokensInfoFromCacheAsync();
            var blockIndex = new BlockIndex
            {
                BlockHash = chainContext.BlockHash,
                BlockHeight = chainContext.BlockHeight
            };
            var minHeight = keys.Select(k => k.BlockHeight).Min();
            List<AvailableTokenInfoInCache> tokenInfoDic = null;
            do
            {
                if (_cacheProvider.TryGetExtraAcceptedTokensInfoFromForkCache(blockIndex, out var value))
                {
                    tokenInfoDic = value;
                    break;
                }

                var link = _chainBlockLinkService.GetCachedChainBlockLink(blockIndex.BlockHash);
                blockIndex.BlockHash = link?.PreviousBlockHash;
                blockIndex.BlockHeight--;
            } while (blockIndex.BlockHash != null && blockIndex.BlockHeight >= minHeight);

            return tokenInfoDic ?? await GetExtraAcceptedTokensInfoFromCacheAsync();
        }

        public void SetExtraAcceptedTokenInfoToForkCache(BlockIndex index,
            List<AvailableTokenInfoInCache> tokenInfos)
        {
            _cacheProvider.SetExtraAcceptedTokenInfoToForkCache(index, tokenInfos);
        }

        public void RemoveFromForkCacheByBlockIndex(List<BlockIndex> blockIndexes)
        {
            _cacheProvider.RemoveFromForkCacheByBlockIndex(blockIndexes);
        }

        public void SyncCache(List<BlockIndex> blockIndexes)
        {
            _cacheProvider.SyncCache(blockIndexes);
        }

        private async Task<List<AvailableTokenInfoInCache>> GetExtraAcceptedTokensInfoFromCacheAsync()
        {
            var normalCache = _cacheProvider.GetExtraAcceptedTokensInfoFromNormalCache();
            if (normalCache != null)
                return normalCache;
            var chain = await _blockchainService.GetChainAsync();
            var tokenStub = _tokenStTokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = chain.LastIrreversibleBlockHash,
                BlockHeight = chain.LastIrreversibleBlockHeight
            });
            var tokenInfos = await tokenStub.GetSymbolsToPayTXSizeFee.CallAsync(new Empty());
            var tokenInfoList = new List<AvailableTokenInfoInCache>();
            if (tokenInfos != null)
            {
                foreach (var tokenInfo in tokenInfos.SymbolsToPayTxSizeFee)
                {
                    tokenInfoList.Add(new AvailableTokenInfoInCache
                    {
                        TokenSymbol = tokenInfo.TokenSymbol,
                        AddedTokenWeight = tokenInfo.AddedTokenWeight,
                        BaseTokenWeight = tokenInfo.BaseTokenWeight
                    });
                }
            }

            _cacheProvider.SetExtraAcceptedTokenInfoToCache(tokenInfoList);
            return tokenInfoList;
        }
    }
}