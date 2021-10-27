using System;
using System.Collections.Generic;
using AElf.Contracts.MultiToken;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public class CalculateFunction
    {
        private readonly List<Func<int, long>> _currentCalculateFunctions = new List<Func<int, long>>();

        /// <summary>
        /// Use this property to cache CalculateFeeCoefficients message.
        /// </summary>
        internal CalculateFeeCoefficients CalculateFeeCoefficients { get; set; }

        public CalculateFunction(int feeType)
        {
            CalculateFeeCoefficients = new CalculateFeeCoefficients
            {
                FeeTokenType = feeType
            };
        }

        public void AddFunction(IEnumerable<int> coefficients, Func<int, long> function)
        {
            _currentCalculateFunctions.Add(function);

            CalculateFeeCoefficients.PieceCoefficientsList.Add(new CalculateFeePieceCoefficients
            {
                Value = {coefficients}
            });
        }

        public long CalculateFee(int totalCount)
        {
            if (CalculateFeeCoefficients.PieceCoefficientsList.Count != _currentCalculateFunctions.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(_currentCalculateFunctions),
                    "Coefficients count not match.");
            }

            var remainCount = totalCount;
            var result = 0L;
            var pieceStart = 0;
            for (var i = 0; i < _currentCalculateFunctions.Count; i++)
            {
                var function = _currentCalculateFunctions[i];
                var pieceCoefficient = CalculateFeeCoefficients.PieceCoefficientsList[i].Value;
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