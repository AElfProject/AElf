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
        Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, ChainContext chainContext);
        void SetCoefficientByTokenType(int tokenType, ChainContext chainContext);
    }
    
    public class CoefficientsCacheProvider : ICoefficientsCacheProvider, ISingletonDependency
    {
        private readonly IBlockchainStateService _blockChainStateService;
        private readonly Dictionary<int, IList<int[]>> _coefficientsDicCache;

        public CoefficientsCacheProvider(IBlockchainStateService blockChainStateService)
        {
            _blockChainStateService = blockChainStateService;
            _coefficientsDicCache = new Dictionary<int, IList<int[]>>();
        }
        
        public async Task<IList<int[]>> GetCoefficientByTokenTypeAsync(int tokenType, ChainContext chainContext)
        {
            if(_coefficientsDicCache.TryGetValue(tokenType, out var coefficientsInCache))
                    return coefficientsInCache;
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
        public void SetCoefficientByTokenType(int tokenType, ChainContext chainContext)
        {
        }
    }
}