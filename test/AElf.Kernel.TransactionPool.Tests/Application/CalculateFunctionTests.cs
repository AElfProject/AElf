using AElf.Kernel.FeeCalculation.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.Kernel.TransactionPool.Application
{
    public class CalculateFunction
    {
        private const long Precision = 100000000L;

        [Fact]
        public void PowCalculateFunction_Test()
        {
            var param = new int[]{1, int.MaxValue, 100, 1, 1, 2, 5, 10};
            var function = new CalculateFunctionProvider();
            var cost = function.PowerFunction(param, 1000);
            cost.ShouldBeGreaterThan(Precision * 1000);
        }

        [Fact]
        public void LinerCalculateFunction_Test()
        {
            var param = new int[]{0, int.MaxValue, 1, 2, 5000};
            var function = new CalculateFunctionProvider();
            var cost = function.LinerFunction(param, 1000);
            cost.ShouldBe(Precision * 1000 / 2 + 5000);
        }
    }
}