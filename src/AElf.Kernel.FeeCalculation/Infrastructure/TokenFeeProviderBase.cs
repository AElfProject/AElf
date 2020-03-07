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
        private readonly ICalculateFunctionFactory _calculateFunctionFactory;
        private readonly int _tokenType;
        protected PieceCalculateFunction PieceCalculateFunction;
        public int[] PieceTypeArray { get; set; }

        protected TokenFeeProviderBase(ICoefficientsCacheProvider coefficientsCacheProvider,
            ICalculateFunctionFactory calculateFunctionFactory, int tokenType)
        {
            _coefficientsCacheProvider = coefficientsCacheProvider;
            _calculateFunctionFactory = calculateFunctionFactory;
            _tokenType = tokenType;
            PieceCalculateFunction = new PieceCalculateFunction();
        }

        public async Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext,
            IChainContext chainContext)
        {
            var coefficients =
                await _coefficientsCacheProvider.GetCoefficientByTokenTypeAsync(_tokenType, chainContext);
            // First number of each piece coefficients is its piece type.
            var pieceTypeArray = coefficients.Select(a => a[0]);
            if (PieceTypeArray == null || PieceCalculateFunction.IsChangedFunctionType(pieceTypeArray))
            {
                UpdatePieceWiseFunction(coefficients);
            }

            var count = GetCalculateCount(transactionContext);
            return PieceCalculateFunction.CalculateFee(coefficients, count);
        }

        public void UpdatePieceWiseFunction(IList<int[]> pieceTypeList)
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            foreach (var pieceCoefficients in pieceTypeList)
            {
                PieceCalculateFunction.AddFunction(pieceCoefficients[0], _calculateFunctionFactory.GetFunction(pieceCoefficients));
            }
        }

        protected abstract int GetCalculateCount(ITransactionContext transactionContext);
    }
}