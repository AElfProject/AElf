using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal abstract class TokenFeeProviderBase
    {
        private readonly ICoefficientsProvider _coefficientsProvider;
        private readonly ICalculateFunctionProvider _calculateFunctionProvider;
        private readonly int _tokenType;
        private PieceCalculateFunction _pieceCalculateFunction;

        protected TokenFeeProviderBase(ICoefficientsProvider coefficientsProvider,
            ICalculateFunctionProvider calculateFunctionProvider, int tokenType)
        {
            _coefficientsProvider = coefficientsProvider;
            _calculateFunctionProvider = calculateFunctionProvider;
            _tokenType = tokenType;
            _pieceCalculateFunction = new PieceCalculateFunction();
        }

        public async Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext,
            IChainContext chainContext)
        {
            var coefficients =
                await _coefficientsProvider.GetCoefficientByTokenTypeAsync(_tokenType, chainContext);
            // First number of each piece coefficients is its piece type.
            var pieceTypeArray = coefficients.SelectMany(a => a).ToList();
            if (_pieceCalculateFunction.IsChangedFunctionType(pieceTypeArray))
            {
                UpdatePieceWiseFunction(coefficients);
            }

            var count = GetCalculateCount(transactionContext);
            var result = _pieceCalculateFunction.CalculateFee(coefficients, count);
            return result;
        }

        public void UpdatePieceWiseFunction(List<int[]> pieceTypeList)
        {
            var pieceCalculateFunction = new PieceCalculateFunction();
            foreach (var pieceCoefficients in pieceTypeList)
            {
                if ((pieceCoefficients.Length - 1) % 3 == 0)
                    pieceCalculateFunction.AddFunction(pieceCoefficients,
                        _calculateFunctionProvider.GetFunction(pieceCoefficients));
            }
            _pieceCalculateFunction = pieceCalculateFunction;
        }

        protected abstract int GetCalculateCount(ITransactionContext transactionContext);
    }
}