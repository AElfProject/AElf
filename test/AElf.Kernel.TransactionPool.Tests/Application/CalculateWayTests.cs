using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace AElf.Kernel.TransactionPool.Application
{
    public class CalculateWayTests
    {
        private const long Precision = 100000000L;

        [Fact]
        public void PowCalculateWay_Test()
        {
            var param = new Dictionary<string, int>
            {
                {"power", 1},
                {"changespanbase", 2},
                {"weight", 5},
                {"weightbase", 10},
                {"numerator", 100},
                {"denominator", 1}
            };
            var pow = new PowerCalculateWay();
            pow.InitParameter(param);
            
            var parameterDic = pow.GetParameterDic();
            parameterDic.ShouldNotBeNull();
            
            var cost = pow.GetCost(1000);
            cost.ShouldBeGreaterThan(Precision * 1000);
        }

        [Fact]
        public void LinerCalculateWay_Test()
        {
            var param = new Dictionary<string, int>
            {
                {"numerator", 1},
                {"denominator", 2},
                {"constantvalue", 5000}
            };
            var liner = new LinerCalculateWay();
            liner.InitParameter(param);

            var parameterDic = liner.GetParameterDic();
            parameterDic["numerator"].ShouldBe(1);
            parameterDic["denominator"].ShouldBe(2);
            parameterDic["constantvalue"].ShouldBe(5000);

            var cost = liner.GetCost(1000);
            cost.ShouldBe(Precision * 1000 / 2 + 5000);
        }
    }
}