using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    /// <summary>
    /// To provide basic function for piece-wise function.
    /// </summary>
    public interface ICalculateFunctionProvider
    {
        Func<int, long> GetFunction(int[] parameters);
    }

    public class CalculateFunctionProvider : ICalculateFunctionProvider, ISingletonDependency
    {
        public ILogger<CalculateFunctionProvider> Logger { get; set; }

        private const decimal Precision = 100000000;

        public CalculateFunctionProvider()
        {
            Logger = NullLogger<CalculateFunctionProvider>.Instance;
        }

        public Func<int, long> GetFunction(int[] parameters)
        {
            return count => GetExponentialFunc(count, parameters);
        }

        // eg. 2x^2 + 3x + 1 -> (2,2,1, 1,3,1, 0,1,1)
        private long GetExponentialFunc(int count, params int[] parameters)
        {
            long cost = 0;

            // Skip parameters[0] which is meant to be piece upper bound.
            var currentIndex = 1;
            while (currentIndex < parameters.Length - 1)
            {
                cost += GetUnitExponentialCalculation(count, parameters[currentIndex],
                    parameters[currentIndex + 1],
                    parameters[currentIndex + 2]);
                currentIndex += 3;
            }

            return cost;
        }

        // (A, B, C)  ->  x^A * (B / C)
        private long GetUnitExponentialCalculation(int count, params int[] parameters)
        {
            if (parameters[2] == 0)
            {
                Logger.LogError("Third parameter can't be 0. Will tune to 1.");
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