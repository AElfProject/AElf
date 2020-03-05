using System;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    /// <summary>
    /// To provide basic function for piece-wise function.
    /// </summary>
    public interface ICalculateFunctionProvider
    {
        long LinerFunction(int[] coefficient, int count);
        long PowerFunction(int[] coefficient, int count);
    }

    public class CalculateFunctionProvider : ICalculateFunctionProvider, ISingletonDependency
    {
        private const decimal Precision = 100000000;

        public long LinerFunction(int[] coefficient, int count)
        {
            if (coefficient.Length != 5)
                throw new ArgumentException($"Invalid coefficient count, should be 5, but is {coefficient.Length}");
            var outcome = Precision * count * coefficient[2] / coefficient[3] + coefficient[4];
            return (long) outcome;
        }

        public long PowerFunction(int[] coefficient, int count)
        {
            if (coefficient.Length != 8)
                throw new ArgumentException($"Invalid coefficient count, should be 8, but is {coefficient.Length}");
            var outcome = Precision * (decimal) Math.Pow((double) count / coefficient[5], coefficient[4]) *
                          coefficient[6] / coefficient[7] +
                          Precision * coefficient[2] * count / coefficient[3];
            return (long) outcome;
        }
    }
}