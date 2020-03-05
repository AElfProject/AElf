using System;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
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
            var outcome = Precision * count * coefficient[1] / coefficient[2] + coefficient[3];
            return (long) outcome;
        }

        public long PowerFunction(int[] coefficient, int count)
        {
            var outcome = Precision * (decimal) Math.Pow((double) count / coefficient[4], coefficient[3]) *
                          coefficient[5] / coefficient[6] +
                          Precision * coefficient[1] * count / coefficient[2];
            return (long) outcome;
        }
    }
}