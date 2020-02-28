using System.Collections.Generic;

namespace AElf.Kernel.FeeCalculation
{
    public interface ICoefficientsCacheProvider
    {
        Coefficients GetCoefficientByTokenName(string tokenName);
        void SetCoefficientByTokenName(string tokenName);
    }
    
    public class CoefficientsCacheProvider : ICoefficientsCacheProvider
    {
        private readonly IMockBlockChainStateService _blockchainStateService;
        private readonly Dictionary<string, Coefficients> _coefficientsDicCache;
        private readonly Dictionary<string, int> _updateCountDic;
        private readonly object _lock;

        public CoefficientsCacheProvider(IMockBlockChainStateService blockchainStateService)
        {
            _blockchainStateService = blockchainStateService;
            _coefficientsDicCache = new Dictionary<string, Coefficients>();
            _updateCountDic = new Dictionary<string, int>();
            _lock = new object();
        }
        public Coefficients GetCoefficientByTokenName(string tokenName)
        {
            lock (_lock)
            {
                if(_coefficientsDicCache.TryGetValue(tokenName, out var coefficients) && _updateCountDic[tokenName] == 0)
                    return coefficients;
                coefficients = _blockchainStateService.GetCoefficientByTokenName(tokenName);
                _coefficientsDicCache[tokenName] = coefficients;
                if (!_updateCountDic.ContainsKey(tokenName))
                    _updateCountDic[tokenName] = 0;
                if (_updateCountDic[tokenName] > 0)
                    _updateCountDic[tokenName] -= 1;
                return coefficients;
            }
        }
        public void SetCoefficientByTokenName(string tokenName)
        {
            lock (_lock)
            {
                if(_updateCountDic.ContainsKey(tokenName))
                    _updateCountDic[tokenName] += 1;
                _updateCountDic[tokenName] = 1;
            }
        }
    }
    
    public interface IMockBlockChainStateService
    {
        Coefficients GetCoefficientByTokenName(string tokenName);
    }
    
    public class MockBlockChainStateService : IMockBlockChainStateService
    {
        public Coefficients GetCoefficientByTokenName(string tokenName)
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