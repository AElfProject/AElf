using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    public class CalculateFunctionTest
    {
        private readonly CalculateFunction _calculateFunction;

        public CalculateFunctionTest()
        {
            var feeType = 1;
            _calculateFunction = new CalculateFunction(feeType);
        }


        [Theory]
        [InlineData( 10, 100, 1000, 5, 5)] // 5
        [InlineData( 10, 100, 1000, 10, 10)] // 10
        [InlineData( 10, 100, 1000, 50, 4010)]  // 10 + 40 * 100
        [InlineData( 10, 100, 1000, 100, 9010)] // 10 + （100 - 10）* 100
        [InlineData( 10, 100, 1000, 500, 409010)] // 10 + （100 - 10）* 100 +（500 -100）* 1000
        [InlineData( 10, 100, 1000, 1000, 909010)] //10 + （100 - 10）* 100 +（1000 -100）* 1000
        [InlineData( 10, 100, 1000, 1001, 909010)] //10 + （100 - 10）* 100 +（1000 -100）* 1000
        public async Task CalculateFunction_Piece_Wise_Test(int piece1, int piece2, int piece3, int input, long outCome)
        {
            _calculateFunction.AddFunction(new []{piece1}, Calculate1);
            _calculateFunction.AddFunction(new []{piece2}, Calculate2);
            _calculateFunction.AddFunction(new []{piece3}, Calculate3);

            var calculateOutcome = _calculateFunction.CalculateFee(input);
            calculateOutcome.ShouldBe(outCome);
        }
        
        [Fact]
        public async Task CalculateFunction_With_Miss_Match_Functions_Test()
        {
            _calculateFunction.AddFunction(new []{1}, Calculate1);
            _calculateFunction.AddFunction(new []{10}, Calculate2);
            _calculateFunction.AddFunction(new []{100}, Calculate3);
            _calculateFunction.CalculateFeeCoefficients.PieceCoefficientsList.RemoveAt(0);
            string errorMsg = null;
            try
            {
                _calculateFunction.CalculateFee(1000);
            }
            catch(ArgumentOutOfRangeException ex)
            {
                errorMsg = ex.Message;
            }
            errorMsg.ShouldContain("Coefficients count not match");
        }

        private long Calculate1(int count)
        {
            return count;
        }
        
        private long Calculate2(int count)
        {
            return (long)count * 100;
        }
        
        private long Calculate3(int count)
        {
            return (long)count * 1000;
        }
    }
}