using System.Collections.Generic;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation
{
    public interface ICoefficientsCacheProvider
    {
        Coefficients GetCoefficientByTokenType(int tokenType);
        void SetCoefficientByTokenType(int tokenType);
    }
    
    public class CoefficientsCacheProvider : ICoefficientsCacheProvider, ISingletonDependency
    {
        private readonly IMockBlockChainStateService _blockChainStateService;
        private readonly Dictionary<int, Coefficients> _coefficientsDicCache;
        private readonly Dictionary<int, int> _updateCountDic;
        private readonly object _lock;

        public CoefficientsCacheProvider(IMockBlockChainStateService blockChainStateService)
        {
            _blockChainStateService = blockChainStateService;
            _coefficientsDicCache = new Dictionary<int, Coefficients>();
            _updateCountDic = new Dictionary<int, int>();
            _lock = new object();
        }
        public Coefficients GetCoefficientByTokenType(int tokenType)
        {
            lock (_lock)
            {
                if(_coefficientsDicCache.TryGetValue(tokenType, out var coefficients) && _updateCountDic[tokenType] == 0)
                    return coefficients;
                coefficients = _blockChainStateService.GetCoefficientByTokenType(tokenType);
                _coefficientsDicCache[tokenType] = coefficients;
                if (!_updateCountDic.ContainsKey(tokenType))
                    _updateCountDic[tokenType] = 0;
                if (_updateCountDic[tokenType] > 0)
                    _updateCountDic[tokenType] -= 1;
                return coefficients;
            }
        }
        public void SetCoefficientByTokenType(int tokenType)
        {
            lock (_lock)
            {
                if(_updateCountDic.ContainsKey(tokenType))
                    _updateCountDic[tokenType] += 1;
                _updateCountDic[tokenType] = 1;
            }
        }
    }
    
    public interface IMockBlockChainStateService
    {
        Coefficients GetCoefficientByTokenType(int tokenType);
        
    }
    
    public class MockBlockChainStateService : IMockBlockChainStateService
    {
        public Coefficients GetCoefficientByTokenType(int tokenType)
        {
            return new Coefficients();
        }
    }
    
    public class Coefficients
    {
        public Coefficient[] AllCoefficients { get; set; }
    }
    public class Coefficient
    {
        public int[] Parameters { get; set; }
    }
}