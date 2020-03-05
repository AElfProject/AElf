using System;
using System.Collections.Generic;
using System.Linq;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public class PieceCalculateFunction
    {
        private PieceCalculateFunction _next;
        private Func<int[], int, long> _currentCalculateFunction;

        public PieceCalculateFunction(Func<int[], int, long> function)
        {
            AddFunction(function);
        }

        public PieceCalculateFunction()
        {

        }

        public void AddFunction(Func<int[], int, long> function)
        {
            if (_currentCalculateFunction == null)
            {
                _currentCalculateFunction = function;
            }
            else
            {
                _next = new PieceCalculateFunction(function);
            }
        }

        public long CalculateFee(IList<int[]> coefficient, int totalCount, int currentCoefficientIndex = 0)
        {
            if (!coefficient.Any()) return 0;
            var currentCoefficient = coefficient[currentCoefficientIndex];
            var piece = currentCoefficient[1];
            if (piece >= totalCount || _next == null || coefficient.Count == 1 ||
                currentCoefficientIndex >= coefficient.Count)
            {
                // piece will increase during calling this method recursively,
                // finally piece will greater than or equal to totalCount, thus terminate the recursion.
                // In this is the way we implemented piece-wise function.
                return _currentCalculateFunction(currentCoefficient, totalCount);
            }

            var nextCoefficientIndex = currentCoefficientIndex + 1;
            var nextCount = totalCount - piece;
            nextCount = Math.Max(nextCount, 0);
            return _currentCalculateFunction(currentCoefficient, piece) +
                   _next.CalculateFee(coefficient, nextCount, nextCoefficientIndex);
        }
    }
}