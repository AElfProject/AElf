using System;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Infrastructure;

namespace AElf.Kernel.FeeCalculation.Extensions
{
    public static class CalculateFeeCoefficientsExtensions
    {
        internal static CalculateFunction ToCalculateFunction(
            this CalculateFeeCoefficients calculateFeeCoefficients)
        {
            var pieceCalculateFunction = new CalculateFunction(calculateFeeCoefficients.FeeTokenType);
            foreach (var pieceCoefficients in calculateFeeCoefficients.PieceCoefficientsList.Where(pc =>
                (pc.Value.Count - 1) % 3 == 0))
            {
                pieceCalculateFunction.AddFunction(pieceCoefficients.Value.ToArray(),
                    GetFunction(pieceCoefficients.Value.ToArray()));
            }

            return pieceCalculateFunction;
        }

        private const decimal Precision = 100000000;

        private static Func<int, long> GetFunction(int[] parameters)
        {
            return count => GetExponentialFunc(count, parameters);
        }

        // eg. 2x^2 + 3x + 1 -> (2,2,1, 1,3,1, 0,1,1)
        private static long GetExponentialFunc(int count, params int[] parameters)
        {
            long cost = 0;

            // Skip parameters[0] which is meant to be piece upper bound.
            var currentIndex = 1;
            while (currentIndex < parameters.Length)
            {
                cost += GetUnitExponentialCalculation(count, parameters[currentIndex],
                    parameters[currentIndex + 1],
                    parameters[currentIndex + 2]);
                currentIndex += 3;
            }

            return cost;
        }

        // (A, B, C)  ->  x^A * (B / C)
        private static long GetUnitExponentialCalculation(int count, params int[] parameters)
        {
            if (parameters[2] == 0)
            {
                parameters[2] = 1;
            }

            decimal decimalResult;
            var power = parameters[0];
            decimal divisor = parameters[1];
            decimal dividend = parameters[2];
            if (power == 0)
            {
                // This piece is (B / C)
                decimalResult = divisor / dividend;
            }
            else
            {
                // Calculate x^A at first.
                var powerResult = (decimal) Math.Pow(count, power);
                decimalResult = powerResult * divisor / dividend;
            }

            return (long) (decimalResult * Precision);
        }
    }
}