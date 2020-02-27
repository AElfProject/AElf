using System;
using System.Collections.Generic;
using AElf.Kernel.FeeCalculation.Impl;

namespace AElf.Kernel.FeeCalculation
{
    public class PieceCalculateFunction
    {
        private PieceCalculateFunction _next;
        private Func<int[], int, long> _currentCalculateFunction;
        public PieceCalculateFunction(Func<int[], int, long> function)
        {
            _currentCalculateFunction = function;
        }
        public PieceCalculateFunction(){}

        public PieceCalculateFunction AddFunction(Func<int[], int, long> function){
            if(_currentCalculateFunction == null)
            {
                _currentCalculateFunction = function;
                return this;
            }
            _next = new PieceCalculateFunction(function);
            return _next;
        }

        public long CalculateFee(IList<Coefficient> coefficient, int totalCount)
        {
            if (coefficient.Count == 0) return 0;
            var currentCoefficient = coefficient[0];
            var parameters = currentCoefficient.Parameters;
            var piece = parameters[0];
            if(piece >= totalCount || _next == null || coefficient.Count == 1)
            {
                // totalCount会随着CalculateFee调用不断变小，或不存在下一段分段函数，或不存在更多系数列表，都可以最终阻断CalculateFee的调用。以此实现分段函数
                return _currentCalculateFunction(parameters, totalCount);
            }
            coefficient.Remove(currentCoefficient);
            var nextCount = totalCount - piece;
            nextCount = nextCount > 0 ? nextCount : 0;
            return _currentCalculateFunction(parameters, piece) + _next.CalculateFee(coefficient, nextCount);
        }

    }
}