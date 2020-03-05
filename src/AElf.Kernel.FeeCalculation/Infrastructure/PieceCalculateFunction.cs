using System;
using System.Collections.Generic;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public class PieceCalculateFunction
    {
        private List<Func<int[], int, long>> _currentCalculateFunctions;

        public void AddFunction(Func<int[], int, long> function)
        {
            if (_currentCalculateFunctions == null)
            {
                _currentCalculateFunctions = new List<Func<int[], int, long>>();
            }

            _currentCalculateFunctions.Add(function);
        }

        public long CalculateFee(IList<int[]> coefficient, int totalCount, int currentCoefficientIndex = 0)
        {
            if (coefficient.Count != _currentCalculateFunctions.Count)
            {
                return 0;
            }

            var remainCount = totalCount;
            var result = 0L;
            for (var i = 0; i < _currentCalculateFunctions.Count; i++)
            {
                var function = _currentCalculateFunctions[i];
                var pieceCoefficient = coefficient[i];
                var pieceUpperBound = pieceCoefficient[1];
                var count = Math.Min(pieceUpperBound, remainCount);
                result += function(pieceCoefficient, count);
                if (pieceUpperBound > totalCount)
                {
                    break;
                }

                remainCount -= pieceUpperBound;
            }

            return result;
        }
    }
}