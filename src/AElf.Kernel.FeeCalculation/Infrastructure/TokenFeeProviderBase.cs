using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal abstract class TokenFeeProviderBase
    {
        private readonly ICoefficientsProvider _coefficientsProvider;
        private readonly ICalculateFunctionProvider _calculateFunctionProvider;
        private readonly int _tokenType;

        // TODO: Temp solution to figure out what happened during testing.
        // If different calculated fee results are due to concurrency problem,
        // need to call UpdatePieceWiseFunction more advance as a final solution.
        private readonly ConcurrentBag<PieceCalculateFunction> _pieceCalculateFunction;

        public ILogger<TokenFeeProviderBase> Logger { get; set; }

        protected TokenFeeProviderBase(ICoefficientsProvider coefficientsProvider,
            ICalculateFunctionProvider calculateFunctionProvider, int tokenType)
        {
            _coefficientsProvider = coefficientsProvider;
            _calculateFunctionProvider = calculateFunctionProvider;
            _tokenType = tokenType;
            _pieceCalculateFunction = new ConcurrentBag<PieceCalculateFunction>
            {
                // Add one instance at first.
                new PieceCalculateFunction()
            };

            Logger = NullLogger<TokenFeeProviderBase>.Instance;
        }

        public async Task<long> CalculateTokenFeeAsync(ITransactionContext transactionContext,
            IChainContext chainContext)
        {
            Logger.LogInformation("Entering CalculateTokenFeeAsync.");
            var coefficients =
                await _coefficientsProvider.GetCoefficientByTokenTypeAsync(_tokenType, chainContext);
            // First number of each piece coefficients is its piece type.
            var pieceTypeArray = coefficients.SelectMany(a => a).ToList();

            Logger.LogInformation("Trying to take PieceCalculationFunction instance from bag.");

            if (!_pieceCalculateFunction.TryTake(out var function))
            {
                function = new PieceCalculateFunction();
            }

            if (function.IsChangedFunctionType(pieceTypeArray))
            {
                UpdatePieceWiseFunction(function, coefficients);
            }

            var count = GetCalculateCount(transactionContext);
            var result = function.CalculateFee(coefficients, count);
            _pieceCalculateFunction.Add(function);
            Logger.LogInformation("Leaving CalculateTokenFeeAsync.");
            return result;
        }

        private void UpdatePieceWiseFunction(PieceCalculateFunction pieceCalculateFunction,
            IEnumerable<int[]> pieceTypeList)
        {
            foreach (var pieceCoefficients in pieceTypeList)
            {
                if ((pieceCoefficients.Length - 1) % 3 == 0)
                    pieceCalculateFunction.AddFunction(pieceCoefficients,
                        _calculateFunctionProvider.GetFunction(pieceCoefficients));
            }
        }

        protected abstract int GetCalculateCount(ITransactionContext transactionContext);
    }
}