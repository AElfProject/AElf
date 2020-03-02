using System;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Sdk.CSharp;

namespace AElf.Kernel.FeeCalculation.ResourceTokenFeeProvider.Impl
{
    public abstract class TokenFeeProviderBase
    {
        private readonly ICoefficientsCacheProvider _coefficientsCacheProvider;
        private readonly int _tokenType;
        private readonly long _precision = 100000000L;
        protected PieceCalculateFunction PieceCalculateFunction;
        protected TokenFeeProviderBase(ICoefficientsCacheProvider coefficientsCacheProvider, int tokenType)
        {
            _coefficientsCacheProvider = coefficientsCacheProvider;
            _tokenType = tokenType;
        }
        public async Task<long> CalculateTokenFeeAsync(TransactionContext transactionContext, ChainContext chainContext)
        {
            if(PieceCalculateFunction == null)
                InitializeFunction();
            var count = transactionContext.Transaction.Size();
            var coefficients = await _coefficientsCacheProvider.GetCoefficientByTokenTypeAsync(_tokenType, chainContext);
            return  PieceCalculateFunction.CalculateFee(coefficients, count);
        }
        protected abstract void InitializeFunction();

        protected long LinerFunction(int[] coefficient, int count)
        {
            return _precision.Mul(count).Mul(coefficient[1]).Div(coefficient[2]).Add(coefficient[3]);
        }

        protected long PowerFunction(int[] coefficient, int count)
        {
            return ((long) (Math.Pow((double) count / coefficient[4], coefficient[3]) * _precision)).Mul(coefficient[5]).Div(coefficient[6])
                .Add(_precision.Mul(coefficient[1]).Div(coefficient[2]).Mul(count));
        }
    }
}