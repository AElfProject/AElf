using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public class PieceCalculateFunction
    {
        private readonly List<Func<int, long>> _currentCalculateFunctions = new List<Func<int, long>>();
        private readonly List<int> _latestUpdateFunctionType = new List<int>();

        public ILogger<PieceCalculateFunction> Logger { get; set; }

        public PieceCalculateFunction()
        {
            Logger = NullLogger<PieceCalculateFunction>.Instance;
        }

        public bool IsChangedFunctionType(List<int> currentFunctionType)
        {
            // Change function coefficients if:
            var result = !_latestUpdateFunctionType.Any() // _latestUpdateFunctionType is empty,
                         || _latestUpdateFunctionType.Count !=
                         currentFunctionType.Count // or count isn't match new coefficients
                         || currentFunctionType.Where((t, i) => t != _latestUpdateFunctionType[i])
                             .Any(); // or have any mismatch element.
            if (result)
            {
                Logger.LogInformation("Gonna change function coefficients.");
            }

            return result;
        }

        public void AddFunction(int[] coefficients, Func<int, long> function)
        {
            _latestUpdateFunctionType.AddRange(coefficients);
            _currentCalculateFunctions.Add(function);
        }

        public long CalculateFee(IList<int[]> coefficient, int totalCount)
        {
            if (coefficient.Count != _currentCalculateFunctions.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(coefficient), "Coefficients count not match.");
            }

            var remainCount = totalCount;
            var result = 0L;
            var pieceStart = 0;
            for (var i = 0; i < _currentCalculateFunctions.Count; i++)
            {
                var function = _currentCalculateFunctions[i];
                var pieceCoefficient = coefficient[i];
                var pieceUpperBound = pieceCoefficient[0];
                var interval = pieceUpperBound - pieceStart;
                pieceStart = pieceUpperBound;
                var count = Math.Min(interval, remainCount);
                result += function(count);
                if (pieceUpperBound > totalCount)
                {
                    break;
                }

                remainCount -= interval;
            }

            return result;
        }
    }
}