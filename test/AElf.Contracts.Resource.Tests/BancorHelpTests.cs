using Shouldly;
using Xunit;

namespace AElf.Contracts.Resource
{
    public class BancorHelpTests
    {
        public Converter CpuConverter;

        public BancorHelpTests()
        {
            CpuConverter = new Converter()
            {
                ElfBalance = 1000_000L,
                ElfWeight = 500_000L,
                ResBalance = 1000_000L,
                ResWeight = 500_000L
            };
        }

        [Theory]
        [InlineData(10L)]
        [InlineData(100L)]
        [InlineData(1000L)]
        [InlineData(10000L)]
        public void BuyResource_Test(long paidElf)
        {
            var resourceAmount1 = BuyOperation(paidElf);
            var resourceAmount2 = BuyOperation(paidElf);
            resourceAmount1.ShouldBeGreaterThanOrEqualTo(resourceAmount2);
        }

        [Theory]
        [InlineData(10L)]
        [InlineData(100L)]
        [InlineData(1000L)]
        [InlineData(10000L)]
        public void SellResource_Test(long paidRes)
        {
            var elfAmount1 = SellOperation(paidRes);
            var elfAmount2 = SellOperation(paidRes);
            elfAmount1.ShouldBeGreaterThanOrEqualTo(elfAmount2);
        }

        [Fact]
        public void Buy_And_Sell_Test()
        {
            var resourceAmount1 = BuyOperation(1000L);
            var elfAmount = SellOperation(resourceAmount1);
            elfAmount.ShouldBeLessThan(1000L);

            var resourceAmount2 = BuyOperation(1000L);
            BuyOperation(1000L);
            var elfAmount2 = SellOperation(resourceAmount2);
            elfAmount2.ShouldBeGreaterThan(1000L);
        }

        [Fact]
        public void Calculate_CrossConnector_NormalCase()
        {
            BancorHelpers.CalculateCrossConnectorReturn(100_000, 200_000, 100_000, 200_000, 1000);

            BancorHelpers.CalculateCrossConnectorReturn(100_000, 200_000, 200_000, 400_000, 1000);
        }
        
        [Fact]
        public void Pow_Test()
        {
            var result1 = BancorHelpers.Pow(1.5m, 1);
            result1.ShouldBe(1.5m);

            BancorHelpers.Pow(1.5m, 2);
        }

        private long BuyOperation(long paidElf)
        {
            var resourcePayout = BancorHelpers.CalculateCrossConnectorReturn(
                CpuConverter.ElfBalance, CpuConverter.ElfWeight,
                CpuConverter.ResBalance, CpuConverter.ResWeight,
                paidElf);
            CpuConverter.ElfBalance += paidElf;
            CpuConverter.ResBalance -= resourcePayout;

            return resourcePayout;
        }

        private long SellOperation(long paidRes)
        {
            var elfPayout = BancorHelpers.CalculateCrossConnectorReturn(
                CpuConverter.ResBalance, CpuConverter.ResWeight,
                CpuConverter.ElfBalance, CpuConverter.ElfWeight,
                paidRes);
            CpuConverter.ElfBalance -= elfPayout;
            CpuConverter.ResBalance += paidRes;

            return elfPayout;
        }
    }
}