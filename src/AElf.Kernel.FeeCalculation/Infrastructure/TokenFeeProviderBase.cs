using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
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
        }

        public async Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext,
            IChainContext chainContext)
        {
            if (PieceCalculateFunction == null)
                return 0; // Not calculate fee if function not initialed yet.
            var count = GetCalculateCount(transactionContext);
            var coefficients =
                await _coefficientsCacheProvider.GetCoefficientByTokenTypeAsync(_tokenType, chainContext);
            return PieceCalculateFunction.CalculateFee(coefficients, count);
        }

        /// <summary>
        /// Piece Type:
        /// 0 - Liner
        /// 1 - Power
        /// </summary>
        /// <param name="pieceTypeArray"></param>
        public void UpdatePieceWiseFunction(int[] pieceTypeArray)
        {
            foreach (var pieceType in pieceTypeArray)
            {
                if (pieceType == 0)
                {
                    PieceCalculateFunction.AddFunction(_calculateFunctionProvider.LinerFunction);
                }
                else
                {
                    PieceCalculateFunction.AddFunction(_calculateFunctionProvider.PowerFunction);
                }
            }
        }

        protected abstract int GetCalculateCount(ITransactionContext transactionContext);
    }
}