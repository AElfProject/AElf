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
                _needReLoadDic[tokenType] = false;
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
            CalculateFeeCoefficientOfContract coefficientFromContract = null;
            foreach (var kp in _needReLoadDic.Where(kp => kp.Value))
            {
                if (kp.Key == (int) FeeTypeEnum.Tx)
                {
                    _coefficientsDicCache[kp.Key] = await GetFromBlockChainStateAsync(kp.Key, chainContext);
                }
                else
                {
                    if (coefficientFromContract == null)
                        coefficientFromContract = await _blockChainStateService.GetBlockExecutedDataAsync<CalculateFeeCoefficientOfContract>(chainContext);
                    _coefficientsDicCache[kp.Key] = coefficientFromContract.CoefficientDicOfContract[kp.Key].Coefficients.AsEnumerable()
                        .Select(x => (int[])(x.CoefficientArray.AsEnumerable())).ToList();
                }
            }
            _needReLoadDic = _needReLoadDic.ToDictionary(x => x.Key, x => true);
        }

        private async Task<IList<int[]>> GetFromBlockChainStateAsync(int tokenType, IChainContext chainContext)
        {
            IList<int[]> coefficientsArray;
            if (tokenType == (int) FeeTypeEnum.Tx)
            {
                var coefficientOfTx = await _blockChainStateService.GetBlockExecutedDataAsync<CalculateFeeCoefficientOfSender>(chainContext);
                coefficientsArray = coefficientOfTx.CoefficientOfSender.Coefficients.AsEnumerable()
                    .Select(x => (int[])(x.CoefficientArray.AsEnumerable())).ToList();
            }
            else
            {
                var coefficients = await _blockChainStateService.GetBlockExecutedDataAsync<CalculateFeeCoefficientOfContract>(chainContext);
                var coefficientOfToken = coefficients.CoefficientDicOfContract[tokenType];
                coefficientsArray = coefficientOfToken.Coefficients.AsEnumerable()
                    .Select(x => (int[])(x.CoefficientArray.AsEnumerable())).ToList();
            }
            return coefficientsArray;
        }
    }
}