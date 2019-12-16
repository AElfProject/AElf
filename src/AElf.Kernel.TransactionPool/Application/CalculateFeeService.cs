using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    #region ICalculateAlgorithm implemention

    class CalculateAlgorithmContext : ICalculateAlgorithmContext
    {
        public int CalculateFeeTypeEnum { get; set; }
        public BlockIndex BlockIndex { get; set; }
    }

    interface ICalculateFunctionProvider : ITransientDependency
    {
        Dictionary<int, ICalculateWay> PieceWiseFuncCache { get; set; }
        Dictionary<int, ICalculateWay> DefaultWiseFuncCache { get; set; }
        ConcurrentDictionary<BlockIndex, Dictionary<int, ICalculateWay>> ForkCache { get; set; }
    }
    class CalculateFunctionProvider : ICalculateFunctionProvider
    {
        public Dictionary<int, ICalculateWay> PieceWiseFuncCache { get; set; }
        public Dictionary<int, ICalculateWay> DefaultWiseFuncCache { get; set; } = new Dictionary<int, ICalculateWay>();
        public ConcurrentDictionary<BlockIndex, Dictionary<int, ICalculateWay>> ForkCache { get; set; } = new ConcurrentDictionary<BlockIndex, Dictionary<int, ICalculateWay>>();
    }
    class CalculateAlgorithmService : ICalculateAlgorithmService
    {
        private readonly ITokenContractReaderFactory _tokenStTokenContractReaderFactory;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainBlockLinkService _chainBlockLinkService;
        private readonly ICalculateFunctionProvider _cacheProvider;

        public ICalculateAlgorithmContext CalculateAlgorithmContext { get; } = new CalculateAlgorithmContext();
        public ILogger<CalculateAlgorithmService> Logger { get; set; }

        public CalculateAlgorithmService(ITokenContractReaderFactory tokenStTokenContractReaderFactory,
            IBlockchainService blockchainService,
            IChainBlockLinkService chainBlockLinkService,
            ICalculateFunctionProvider cacheProvider)
        {
            _tokenStTokenContractReaderFactory = tokenStTokenContractReaderFactory;
            _blockchainService = blockchainService;
            _chainBlockLinkService = chainBlockLinkService;
            _cacheProvider = cacheProvider;
            Logger = new NullLogger<CalculateAlgorithmService>();
        }
       

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            var forkCache = _cacheProvider.ForkCache;
            foreach (var blockIndex in blockIndexes.Where(blockIndex =>forkCache.TryGetValue(blockIndex, out _)))
            {
                forkCache.TryRemove(blockIndex, out _);
            }
        }
        public ICalculateAlgorithmService AddDefaultAlgorithm(int limit, ICalculateWay func)
        {
            _cacheProvider.DefaultWiseFuncCache[limit] = func;
            return this;
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            var forkCache = _cacheProvider.ForkCache;
            foreach (var blockIndex in blockIndexes)
            {
                if (!forkCache.TryGetValue(blockIndex, out var calAlgorithm)) continue;
                _cacheProvider.PieceWiseFuncCache = calAlgorithm;
                forkCache.TryRemove(blockIndex, out _);
            }
        }

        public async Task<long> CalculateAsync(int count)
        {
            count = count < 0 ? int.MaxValue : count;
            var pieceWiseFunc = await GetPieceWiseFuncUnderContextAsync();
            long totalCost = 0;
            int prePieceKey = 0;
            foreach (var piece in pieceWiseFunc.OrderBy(x => x.Key))
            {
                if (count < piece.Key)
                {
                    totalCost = piece.Value.GetCost(count.Sub(prePieceKey)).Add(totalCost);
                    break;
                }

                var span = piece.Key.Sub(prePieceKey);
                totalCost = piece.Value.GetCost(span).Add(totalCost);
                prePieceKey = piece.Key;
                if (count == piece.Key)
                    break;
            }

            return totalCost;
        }

        public void AddAlgorithmByBlock(BlockIndex blockIndex, IList<ICalculateWay> allFunc)
        {
            var funcDic = new Dictionary<int, ICalculateWay>();
            foreach (var func in allFunc)
            {
                funcDic[func.PieceKey] = func;
            }

            _cacheProvider.ForkCache[blockIndex] = funcDic;
        }

        private async Task<Dictionary<int, ICalculateWay>> GetPieceWiseFuncUnderContextAsync()
        {
            var forkCache = _cacheProvider.ForkCache;
            var keys = forkCache.Keys.ToArray();
            if (keys.Length == 0) return await GetDefaultPieceWiseFunctionAsync();
            var minHeight = keys.Select(k => k.BlockHeight).Min();
            Dictionary<int, ICalculateWay> algorithm = null;
            var blockIndex = new BlockIndex
            {
                BlockHash = CalculateAlgorithmContext.BlockIndex.BlockHash,
                BlockHeight = CalculateAlgorithmContext.BlockIndex.BlockHeight
            };
            do
            {
                if (forkCache.TryGetValue(blockIndex, out var value))
                {
                    algorithm = value;
                    break;
                }

                var link = _chainBlockLinkService.GetCachedChainBlockLink(blockIndex.BlockHash);
                blockIndex.BlockHash = link?.PreviousBlockHash;
                blockIndex.BlockHeight--;
            } while (blockIndex.BlockHash != null && blockIndex.BlockHeight >= minHeight);

            return algorithm ?? await GetDefaultPieceWiseFunctionAsync();
        }

        private async Task<Dictionary<int, ICalculateWay>> GetDefaultPieceWiseFunctionAsync()
        {
            if (_cacheProvider.PieceWiseFuncCache != null)
            {
                return _cacheProvider.PieceWiseFuncCache;
            }

            var chain = await _blockchainService.GetChainAsync();

            var tokenStub = _tokenStTokenContractReaderFactory.Create(new ChainContext
            {
                BlockHash = chain.LastIrreversibleBlockHash,
                BlockHeight = chain.LastIrreversibleBlockHeight
            });

            CalculateFeeCoefficientsOfType parameters;
            if (CalculateAlgorithmContext.CalculateFeeTypeEnum == (int) FeeTypeEnum.Tx)
            {
                parameters = await tokenStub.GetCalculateFeeCoefficientOfSender.CallAsync(new Empty());
            }
            else
            {
                parameters = await tokenStub.GetCalculateFeeCoefficientOfContract.CallAsync(new SInt32Value
                    {Value = CalculateAlgorithmContext.CalculateFeeTypeEnum});
            }
            
            if (parameters == null)
            {
                Logger.LogWarning("does not find parameter from contract, initialize from default ");
                _cacheProvider.PieceWiseFuncCache = _cacheProvider.DefaultWiseFuncCache.ToDictionary(x => x.Key, x => x.Value);
                return _cacheProvider.PieceWiseFuncCache;
            }

            if (_cacheProvider.PieceWiseFuncCache == null)
                _cacheProvider.PieceWiseFuncCache = new Dictionary<int, ICalculateWay>();
            _cacheProvider.PieceWiseFuncCache.Clear();
            var calWayDic = new Dictionary<int, ICalculateWay>();
            foreach (var func in parameters.Coefficients)
            {
                ICalculateWay newCalculateWay = null;
                switch (func.FunctionType)
                {
                    case CalculateFunctionTypeEnum.Liner:
                        newCalculateWay = new LinerCalculateWay();
                        break;
                    case CalculateFunctionTypeEnum.Power:
                        newCalculateWay = new PowerCalculateWay();
                        break;
                }

                if (newCalculateWay == null)
                {
                    Logger.LogWarning($"could not find mapped function type {func.FunctionType}");
                    continue;
                }

                calWayDic[func.PieceKey] = newCalculateWay;
                calWayDic[func.PieceKey].InitParameter(func.CoefficientDic);
            }
            _cacheProvider.PieceWiseFuncCache = calWayDic;
            return _cacheProvider.PieceWiseFuncCache;
        }
    }

    #endregion
}