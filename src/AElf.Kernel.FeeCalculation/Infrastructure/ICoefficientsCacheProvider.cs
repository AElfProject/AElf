using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public interface ICoefficientsCacheProvider : ISyncCacheService
    {
        Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext);
        void UpdateLatestModifiedHeight(long height);
    }

    public class CoefficientsCacheProvider : ICoefficientsCacheProvider, ISyncCacheProvider, ISingletonDependency
    {
        private readonly IBlockchainStateService _blockChainStateService;
        private readonly IServiceContainer<IResourceTokenFeeProvider> _resourceTokenFeeProviders;
        private readonly IPrimaryTokenFeeProvider _primaryTokenFeeProvider;
        private readonly Dictionary<int, IList<int[]>> _coefficientsDicCache;
        private long _latestModifiedHeight;

        public CoefficientsCacheProvider(IBlockchainStateService blockChainStateService,
            IServiceContainer<IResourceTokenFeeProvider> resourceTokenFeeProviders,
            IPrimaryTokenFeeProvider primaryTokenFeeProvider)
        {
            _blockChainStateService = blockChainStateService;
            _resourceTokenFeeProviders = resourceTokenFeeProviders;
            _primaryTokenFeeProvider = primaryTokenFeeProvider;
            _coefficientsDicCache = new Dictionary<int, IList<int[]>>();
            _latestModifiedHeight = 0;
        }

        public async Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext)
        {
            if (_latestModifiedHeight == 0)
            {
                if (_coefficientsDicCache.TryGetValue(tokenType, out var coefficientsInCache))
                    return coefficientsInCache;
                coefficientsInCache = await GetFromBlockChainStateAsync(tokenType, chainContext);
                _coefficientsDicCache[tokenType] = coefficientsInCache;
                return coefficientsInCache;
            }

            return await GetFromBlockChainStateAsync(tokenType, chainContext);
        }

        public void UpdateLatestModifiedHeight(long height)
        {
            _latestModifiedHeight = height;
        }

        public async Task SyncCacheAsync(IChainContext chainContext)
        {
            var currentLibHeight = chainContext.BlockHeight;
            if (_latestModifiedHeight <= currentLibHeight)
            {
                var allCalculateFeeCoefficients =
                    await _blockChainStateService.GetBlockExecutedDataAsync<AllCalculateFeeCoefficients>(chainContext);
                var tokenTypeList = _coefficientsDicCache.Select(x => x.Key).ToArray();
                foreach (var tokenType in tokenTypeList)
                {
                    var targetTokeData =
                        allCalculateFeeCoefficients.Value.FirstOrDefault(x => x.FeeTokenType == tokenType);
                    if (targetTokeData == null) continue;
                    _coefficientsDicCache[tokenType] = targetTokeData.PieceCoefficientsList.AsEnumerable()
                        .Select(x => (int[]) x.Value.AsEnumerable()).ToList();
                    var pieceTypeArray = _coefficientsDicCache[tokenType].Select(a => a[0]).ToArray();
                    if (tokenType == (int) FeeTypeEnum.Tx)
                    {
                        _primaryTokenFeeProvider.UpdatePieceWiseFunction(pieceTypeArray);
                    }
                    else
                    {
                        var targetProvider =
                            _resourceTokenFeeProviders.SingleOrDefault(p =>
                                p.TokenName == tokenType.ToString().ToUpper());
                        if (targetProvider == null) continue;
                        targetProvider.PieceTypeArray = pieceTypeArray;
                        targetProvider.UpdatePieceWiseFunction(pieceTypeArray);
                    }
                }

                _latestModifiedHeight = 0;
            }
        }

        private async Task<IList<int[]>> GetFromBlockChainStateAsync(int tokenType, IChainContext chainContext)
        {
            var allCalculateFeeCoefficients =
                await _blockChainStateService.GetBlockExecutedDataAsync<AllCalculateFeeCoefficients>(
                    chainContext);
            var targetTokeData =
                allCalculateFeeCoefficients.Value.SingleOrDefault(x => x.FeeTokenType == tokenType);
            if (targetTokeData == null) return new List<int[]>();
            var coefficientsArray = targetTokeData.PieceCoefficientsList.AsEnumerable()
                .Select(x => (int[]) x.Value.AsEnumerable()).ToList();
            return coefficientsArray;
        }
    }
}