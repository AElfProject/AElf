using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public class CalculateFunction
    {
        private readonly List<Func<int, long>> _currentCalculateFunctions = new List<Func<int, long>>();
        private readonly List<int[]> _currentCalculateCoefficients = new List<int[]>();

        public CalculateFeeCoefficients CalculateFeeCoefficients { get; set; } = new CalculateFeeCoefficients();

        public void AddFunction(int[] coefficients, Func<int, long> function)
        {
            _currentCalculateCoefficients.Add(coefficients);
            _currentCalculateFunctions.Add(function);

            CalculateFeeCoefficients.PieceCoefficientsList.Add(new CalculateFeePieceCoefficients
            {
                Value = {coefficients}
            });
        }

        public long CalculateFee(int totalCount)
        {
            if (_currentCalculateCoefficients.Count != _currentCalculateFunctions.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(_currentCalculateCoefficients),
                    "Coefficients count not match.");
            }

            var remainCount = totalCount;
            var result = 0L;
            var pieceStart = 0;
            for (var i = 0; i < _currentCalculateFunctions.Count; i++)
            {
                var function = _currentCalculateFunctions[i];
                var pieceCoefficient = _currentCalculateCoefficients[i];
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