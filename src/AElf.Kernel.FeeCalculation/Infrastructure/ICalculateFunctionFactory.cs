using System;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    /// <summary>
    /// To provide basic function for piece-wise function.
    /// </summary>
    public interface ICalculateFunctionFactory
    {
        Func<int, long> GetFunction(int[] parameters);
    }

    public class CalculateFunctionFactory : ICalculateFunctionFactory, ISingletonDependency
    {
        private const decimal Precision = 100000000;

        public Func<int, long> GetFunction(int[] parameters)
        {
            return count => GetExponentialFunc(count, parameters);
        }
        
        // eg. 2x^2 + 3x + 1   (2,2,1,1,3,1,0,1,1)
        private long GetExponentialFunc (int count, params int[] parameters) {
            long pieceCost = 0;
            var currentIndex = 1;  // parameters[0] => pieceKey
            while (currentIndex < parameters.Length - 1) {
                pieceCost += GetUnitExponentialCalculation (count, parameters[currentIndex], parameters[currentIndex + 1],
                    parameters[currentIndex + 2]);
                currentIndex += 3;
            }

            return pieceCost;
        }
        
        // (1,2,3)  =>  x^1 * (2/3)
        private long GetUnitExponentialCalculation(int count, params int[] parameters)
        {
            if (parameters[2] == 0)   // denominator cannot be 0
                parameters[2] = 1;
            if (parameters[0] == 0)
                return (long)(Precision * parameters[1] / parameters[2]); 
            if (parameters[0] == 1)
                return (long)(Precision * count * parameters[1] / parameters[2]);
            return (long) (Precision * (decimal)(Math.Pow (count, parameters[0])) * parameters[1]/ parameters[2]);
        }
    }
}