using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation
{
    public interface ICoefficientsCacheProvider
    {
        Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext);
        void SetCoefficientByTokenType(int tokenType);
        Task SyncCache(IChainContext chainContext);
    }

    public class CoefficientsCacheProvider : ICoefficientsCacheProvider, ISingletonDependency
    {
        private readonly IBlockchainStateService _blockChainStateService;
        private readonly Dictionary<int, IList<int[]>> _coefficientsDicCache;
        private Dictionary<int, bool> _needReLoadDic;

        public CoefficientsCacheProvider(IBlockchainStateService blockChainStateService)
        {
            _blockChainStateService = blockChainStateService;
            _coefficientsDicCache = new Dictionary<int, IList<int[]>>();
            _needReLoadDic = new Dictionary<int, bool>();
        }

        public async Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, IChainContext chainContext)
        {
            if (!_needReLoadDic.TryGetValue(tokenType, out var isNeedLoadData))
                _needReLoadDic[tokenType] = true;
            if (!_needReLoadDic[tokenType])
            {
                if (_coefficientsDicCache.TryGetValue(tokenType, out var coefficientsInCache))
                    return coefficientsInCache;
                coefficientsInCache = await GetFromBlockChainStateAsync(tokenType, chainContext);
                _coefficientsDicCache[tokenType] = coefficientsInCache;
                return coefficientsInCache;
            }
            else
                return await GetFromBlockChainStateAsync(tokenType, chainContext);
        }

        public void SetCoefficientByTokenType(int tokenType)
        {
            _needReLoadDic[tokenType] = true;
        }
        
        public async Task SyncCache(IChainContext chainContext)
        {
            CalculateFeeCoefficientOfAllTokenType coefficientOfAllTokenType = null;
            foreach (var kp in _needReLoadDic.Where(kp => kp.Value))
            {
                if (coefficientOfAllTokenType == null)
                    coefficientOfAllTokenType =
                        await _blockChainStateService.GetBlockExecutedDataAsync<CalculateFeeCoefficientOfAllTokenType>(
                            chainContext);
                var targetTokeData =
                    coefficientOfAllTokenType.CoefficientListOfTokenType.FirstOrDefault(x => x.FeeTokenType == kp.Key);
                _coefficientsDicCache[kp.Key] = targetTokeData.Coefficients.AsEnumerable()
                    .Select(x => (int[]) (x.CoefficientArray.AsEnumerable())).ToList();
            }

            _needReLoadDic = _needReLoadDic.ToDictionary(x => x.Key, x => false);
        }

        private async Task<IList<int[]>> GetFromBlockChainStateAsync(int tokenType, IChainContext chainContext)
        {
            var coefficientOfAllTokenType =
                await _blockChainStateService.GetBlockExecutedDataAsync<CalculateFeeCoefficientOfAllTokenType>(
                    chainContext);
            var targetTokeData =
                coefficientOfAllTokenType.CoefficientListOfTokenType.FirstOrDefault(x => x.FeeTokenType == tokenType);
            var coefficientsArray = targetTokeData.Coefficients.AsEnumerable()
                .Select(x => (int[]) (x.CoefficientArray.AsEnumerable())).ToList();
            return coefficientsArray;
        }
    }
}