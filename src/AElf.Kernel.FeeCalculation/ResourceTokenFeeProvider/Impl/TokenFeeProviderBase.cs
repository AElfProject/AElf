using System.Collections.Generic;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.FeeCalculation.Impl
{
    public abstract class TokenFeeProviderBase
    {
        private readonly ICoefficientsCacheProvider _coefficientsCacheProvider;
        protected PieceCalculateFunction PieceCalculateFunction;
        protected TokenFeeProviderBase(ICoefficientsCacheProvider coefficientsCacheProvider)
        {
            _coefficientsCacheProvider = coefficientsCacheProvider;
        }
        public long CalculateTokenFeeAsync(TransactionContext transactionContext)
        {
            if(PieceCalculateFunction == null)
                InitializeFunction();
            var count = transactionContext.Transaction.Size();
            var coefficients = _coefficientsCacheProvider.GetCoefficientByTokenName(GetTokenName());
            return  PieceCalculateFunction.CalculateFee(coefficients.AllCoefficients, count);
        }
        protected abstract void InitializeFunction();
        protected abstract string GetTokenName();

        protected long LinerFunction(int[] coefficient, int count)
        {
            return 0;
        }
        protected long PowerFunction(int[] coefficient, int count)
        {
            return 0;
        }
    }
}