using Shouldly;
using Xunit;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public class CalculateFunctionTests
    {
        private const long Precision = 100000000L;

        [Fact]
        public void PowCalculateFunction_Test()
        {
            var param = new[] {int.MaxValue, 100, 1, 1, 2, 5, 10};
            var function = new CalculateFunctionProvider();
            var cost = function.PowerFunction(param, 1000);
            cost.ShouldBeGreaterThan(Precision * 1000);
        }

        [Fact]
        public void LinerCalculateFunction_Test()
        {
            var param = new[] {int.MaxValue, 1, 2, 5000};
            var function = new CalculateFunctionProvider();
            var cost = function.LinerFunction(param, 1000);
            cost.ShouldBe(Precision * 1000 / 2 + 5000);
        }
    }
}