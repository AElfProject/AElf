using System;
using AElf.Sdk.CSharp;
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
        private const long Precision = 100000000L;

        public long LinerFunction(int[] coefficient, int count)
        {
            return Precision.Mul(count).Mul(coefficient[1]).Div(coefficient[2]).Add(coefficient[3]);
        }

        public long PowerFunction(int[] coefficient, int count)
        {
            return ((long) (Math.Pow((double) count / coefficient[4], coefficient[3]) * Precision)).Mul(coefficient[5])
                .Div(coefficient[6])
                .Add(Precision.Mul(coefficient[1]).Div(coefficient[2]).Mul(count));
        }
    }
}