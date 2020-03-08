using AElf.Kernel.FeeCalculation.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.Kernel.TransactionPool.Application
{
    public class CalculateFunction
    {
        private const long Precision = 100000000;

        [Theory]
        [InlineData(2, 1,
            1, 1, 1,
            0, 1, 1)] // y = x + 1
        [InlineData(7, 2,
            2, 1, 1,
            1, 1, 1,
            0, 1, 1)] // y = x^2 + x + 1
        [InlineData(106, 4,
            3, 1, 1,
            2, 2, 1,
            0, 10, 1)] // y = x^3 + 2x^2 + 10
        public void CalculateFunction_Test(long dependent, params int[] parameters)
        {
            // parameters[0] is the independent variable.
            var function = new CalculateFunctionProvider().GetFunction(parameters);
            var cost = function(parameters[0]);
            cost.ShouldBe(dependent * Precision);
        }
    }
}