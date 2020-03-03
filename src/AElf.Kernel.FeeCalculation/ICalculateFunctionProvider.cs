using System;
using AElf.Sdk.CSharp;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation
{
    public interface ICalculateFunctionProvider
    {
        long LinerFunction(int[] coefficient, int count);
        long PowerFunction(int[] coefficient, int count);
    }

    public class CalculateFunctionProvider : ICalculateFunctionProvider, ISingletonDependency
    {
        private readonly long _precision = 100000000L;

        public long LinerFunction(int[] coefficient, int count)
        {
            return _precision.Mul(count).Mul(coefficient[1]).Div(coefficient[2]).Add(coefficient[3]);
        }

        public long PowerFunction(int[] coefficient, int count)
        {
            return ((long) (Math.Pow((double) count / coefficient[4], coefficient[3]) * _precision)).Mul(coefficient[5])
                .Div(coefficient[6])
                .Add(_precision.Mul(coefficient[1]).Div(coefficient[2]).Mul(count));
        }
    }
}