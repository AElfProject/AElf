using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public class PieceCalculateFunction
    {
        private List<Func<int, long>> _currentCalculateFunctions;
        private List<int> _latestUpdateFunctionType;

        public ILogger<PieceCalculateFunction> Logger { get; set; }

        public PieceCalculateFunction()
        {
            Logger = NullLogger<PieceCalculateFunction>.Instance;
        }

        public bool IsChangedFunctionType(List<int> currentFunctionType)
        {
            if (_latestUpdateFunctionType == null || _latestUpdateFunctionType.Count != currentFunctionType.Count)
                return true;
            return currentFunctionType.Where((t, i) => t != _latestUpdateFunctionType[i]).Any();
        }

        public void AddFunction(int[] coefficients, Func<int, long> function)
        {
            if (_currentCalculateFunctions == null)
            {
                _currentCalculateFunctions = new List<Func<int, long>>();
                _latestUpdateFunctionType = new List<int>();
            }

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