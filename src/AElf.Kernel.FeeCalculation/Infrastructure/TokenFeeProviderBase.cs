using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal abstract class TokenFeeProviderBase
    {
        private readonly ICoefficientsCacheProvider _coefficientsCacheProvider;
        private readonly ICalculateFunctionProvider _calculateFunctionProvider;
        private readonly int _tokenType;
        protected PieceCalculateFunction PieceCalculateFunction;

        protected TokenFeeProviderBase(ICoefficientsCacheProvider coefficientsCacheProvider,
            ICalculateFunctionProvider calculateFunctionProvider, int tokenType)
        {
            _coefficientsCacheProvider = coefficientsCacheProvider;
            _calculateFunctionProvider = calculateFunctionProvider;
            _tokenType = tokenType;
            PieceCalculateFunction = new PieceCalculateFunction();
        }

        public async Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext,
            IChainContext chainContext)
        {
            var coefficients =
                await _coefficientsCacheProvider.GetCoefficientByTokenTypeAsync(_tokenType, chainContext);
            // First number of each piece coefficients is its piece type.
            var pieceTypeArray = coefficients.SelectMany(a => a);
            if (PieceCalculateFunction.IsChangedFunctionType(pieceTypeArray))
            {
                UpdatePieceWiseFunction(coefficients);
            }

            var count = GetCalculateCount(transactionContext);
            var result = PieceCalculateFunction.CalculateFee(coefficients, count);
            return result;
        }

        public void UpdatePieceWiseFunction(IList<int[]> pieceTypeList)
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            foreach (var pieceCoefficients in pieceTypeList)
            {
                if((pieceCoefficients.Length - 1) % 3 == 0)
                    PieceCalculateFunction.AddFunction(pieceCoefficients, _calculateFunctionProvider.GetFunction(pieceCoefficients));
            }
        }

        protected abstract int GetCalculateCount(ITransactionContext transactionContext);
    }
}