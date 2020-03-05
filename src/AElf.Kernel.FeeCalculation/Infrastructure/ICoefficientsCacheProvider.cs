using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public interface ICoefficientsCacheProvider
    {
        Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext);
        void SetCoefficientByTokenType(int tokenType);
    }

    public class CoefficientsCacheProvider : ICoefficientsCacheProvider, ISyncCacheProvider, ISingletonDependency
    {
        private readonly IBlockchainStateService _blockChainStateService;
        private readonly Dictionary<int, IList<int[]>> _coefficientsDicCache;
        private Dictionary<int, bool> _needReloadDic;

        public CoefficientsCacheProvider(IBlockchainStateService blockChainStateService)
        {
            _blockChainStateService = blockChainStateService;
            _coefficientsDicCache = new Dictionary<int, IList<int[]>>();
            _needReloadDic = new Dictionary<int, bool>();
        }

        public async Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext)
        {
            if (!_needReloadDic.TryGetValue(tokenType, out _))
                _needReloadDic[tokenType] = true;
            if (!_needReloadDic[tokenType])
            {
                if (_coefficientsDicCache.TryGetValue(tokenType, out var coefficientsInCache))
                    return coefficientsInCache;
                coefficientsInCache = await GetFromBlockChainStateAsync(tokenType, chainContext);
                _coefficientsDicCache[tokenType] = coefficientsInCache;
                return coefficientsInCache;
            }

            return await GetFromBlockChainStateAsync(tokenType, chainContext);
        }

        public void SetCoefficientByTokenType(int tokenType)
        {
            _needReloadDic[tokenType] = true;
        }

        public async Task SyncCache(IChainContext chainContext)
        {
            AllCalculateFeeCoefficients coefficientOfAllTokenType = null;
            foreach (var kp in _needReloadDic.Where(kp => kp.Value))
            {
                if (coefficientOfAllTokenType == null)
                    coefficientOfAllTokenType =
                        await _blockChainStateService.GetBlockExecutedDataAsync<AllCalculateFeeCoefficients>(
                            chainContext);
                var targetTokeData =
                    coefficientOfAllTokenType.Value.FirstOrDefault(x => x.FeeTokenType == kp.Key);
                _coefficientsDicCache[kp.Key] = targetTokeData.PieceCoefficientsList.AsEnumerable()
                    .Select(x => (int[]) (x.Value.AsEnumerable())).ToList();
            }

            _needReloadDic = _needReloadDic.ToDictionary(x => x.Key, x => false);
        }

        private async Task<IList<int[]>> GetFromBlockChainStateAsync(int tokenType, IChainContext chainContext)
        {
            var coefficientOfAllTokenType =
                await _blockChainStateService.GetBlockExecutedDataAsync<AllCalculateFeeCoefficients>(
                    chainContext);
            var targetTokeData =
                coefficientOfAllTokenType.Value.FirstOrDefault(x => x.FeeTokenType == tokenType);
            var coefficientsArray = targetTokeData.PieceCoefficientsList.AsEnumerable()
                .Select(x => (int[]) (x.Value.AsEnumerable())).ToList();
            return coefficientsArray;
        }
    }
}