using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public class PieceCalculateFunction
    {
        private List<Func<int, long>> _currentCalculateFunctions;
        private List<int> _latestUpdateFunctionType;

        public bool IsChangedFunctionType(IEnumerable<int> currentFunctionType)
        {
            if (_latestUpdateFunctionType == null)
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
                var pieceUpperBound = pieceCoefficient[0];
                var count = Math.Min(pieceUpperBound, remainCount);
                result += function(count);
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