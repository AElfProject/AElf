using System;
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
        public int[] PieceTypeArray { get; set; }

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
            var pieceTypeArray = coefficients.Select(a => a[0]);
            if (PieceTypeArray == null || _coefficientsCacheProvider.GetUpdateNotification(_tokenType))
            {
                UpdatePieceWiseFunction(pieceTypeArray.ToArray());
            }

            var count = GetCalculateCount(transactionContext);
            return PieceCalculateFunction.CalculateFee(coefficients, count);
        }

        public void UpdatePieceWiseFunction(int[] pieceTypeArray)
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            foreach (var pieceType in pieceTypeArray)
            {
                if (pieceType == 0)
                {
                    PieceCalculateFunction.AddFunction(_calculateFunctionProvider.LinerFunction);
                }
                else if (pieceType == 1)
                {
                    PieceCalculateFunction.AddFunction(_calculateFunctionProvider.PowerFunction);
                }
                else
                {
                    throw new Exception("");
                }
            }
        }

        protected abstract int GetCalculateCount(ITransactionContext transactionContext);
    }
}