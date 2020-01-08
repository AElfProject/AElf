using System;
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
    internal class ExtraAcceptedTokenService : IExtraAcceptedTokenService, ISingletonDependency
    {
        private readonly IExtraAcceptedTokensCacheProvider _cacheProvider;
        private readonly ITokenContractReaderFactory _tokenStTokenContractReaderFactory;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        public ILogger<ExtraAcceptedTokenService> Logger { get; set; }

        public ExtraAcceptedTokenService(IExtraAcceptedTokensCacheProvider cacheProvider,
                                         IChainBlockLinkService chainBlockLinkService,
                                         ITokenContractReaderFactory tokenStTokenContractReaderFactory,
                                         IBlockchainService blockchainService)
        {
            _cacheProvider = cacheProvider;
            _chainBlockLinkService = chainBlockLinkService;
            _tokenStTokenContractReaderFactory = tokenStTokenContractReaderFactory;
            _blockchainService = blockchainService;
            Logger = new NullLogger<ExtraAcceptedTokenService>();
        }
        public async Task<Dictionary<string, Tuple<int, int>>> GetExtraAcceptedTokensInfoAsync(IChainContext chainContext)
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
            Dictionary<string, Tuple<int, int>> tokenInfoDic = null;
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
        public void SetExtraAcceptedTokenInfoToForkCache(BlockIndex index, Dictionary<string, Tuple<int, int>> tokenInfos)
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

        private async Task<Dictionary<string, Tuple<int, int>>> GetExtraAcceptedTokensInfoFromCacheAsync()
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
            var tokenInfos = await tokenStub.GetAvailableTokenInfos.CallAsync(new Empty());
            var tokenInfoDic = new Dictionary<string, Tuple<int, int>>();
            if (tokenInfos != null)
            {
                foreach (var tokenInfo in tokenInfos.AllAvailableTokens)
                {
                    tokenInfoDic[tokenInfo.TokenSymbol] = Tuple.Create(tokenInfo.BaseTokenWeight, tokenInfo.AddedTokenWeight);
                }
            }
            _cacheProvider.SetExtraAcceptedTokenInfoToCache(tokenInfoDic);
            return tokenInfoDic;
        }

        public int TestGetCount()
        {
            return _cacheProvider.GetForkCacheKeys().Length;
        }
    }
}