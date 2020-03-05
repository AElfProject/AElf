using System.Threading.Tasks;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Implementation
{
    public abstract class TokenFeeProviderBase
    {
        private readonly ICoefficientsCacheProvider _coefficientsCacheProvider;
        private readonly int _tokenType;
        protected PieceCalculateFunction PieceCalculateFunction;
        protected TokenFeeProviderBase(ICoefficientsCacheProvider coefficientsCacheProvider, int tokenType)
        {
            _coefficientsCacheProvider = coefficientsCacheProvider;
            _tokenType = tokenType;
        }
        public async Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext, IChainContext chainContext)
        {
            if(PieceCalculateFunction == null)
                InitializeFunction();
            var count = GetCalculateCount(transactionContext);
            var coefficients = await _coefficientsCacheProvider.GetCoefficientByTokenTypeAsync(_tokenType, chainContext);
            return  PieceCalculateFunction.CalculateFee(coefficients, count);
        }
        protected abstract void InitializeFunction();
        protected abstract int GetCalculateCount(ITransactionContext transactionContext);
    }
}