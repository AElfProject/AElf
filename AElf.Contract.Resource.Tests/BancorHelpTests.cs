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
                ElfBalance = 1000_000UL,
                ElfWeight = 500_000UL,
                ResBalance = 1000_000UL,
                ResWeight = 500_000UL
            };
        }

        [Theory]
        [InlineData(10UL)]
        [InlineData(100UL)]
        [InlineData(1000UL)]
        [InlineData(10000UL)]
        public void BuyResource_Test(ulong paidElf)
        {
            var resourceAmount1 = BuyOperation(paidElf);
            var resourceAmount2 = BuyOperation(paidElf);
            resourceAmount1.ShouldBeGreaterThanOrEqualTo(resourceAmount2);
        }

        [Theory]
        [InlineData(10UL)]
        [InlineData(100UL)]
        [InlineData(1000UL)]
        [InlineData(10000UL)]
        public void SellResource_Test(ulong paidRes)
        {
            var elfAmount1 = SellOperation(paidRes);
            var elfAmount2 = SellOperation(paidRes);
            elfAmount1.ShouldBeGreaterThanOrEqualTo(elfAmount2);
        }

        [Fact]
        public void Buy_And_Sell_Test()
        {
            var resourceAmount1 = BuyOperation(1000UL);
            var elfAmount = SellOperation(resourceAmount1);
            elfAmount.ShouldBeLessThan(1000UL);

            var resourceAmount2 = BuyOperation(1000UL);
            BuyOperation(1000UL);
            var elfAmount2 = SellOperation(resourceAmount2);
            elfAmount2.ShouldBeGreaterThan(1000UL);
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

        private ulong BuyOperation(ulong paidElf)
        {
            var resourcePayout = BancorHelpers.CalculateCrossConnectorReturn(
                CpuConverter.ElfBalance, CpuConverter.ElfWeight,
                CpuConverter.ResBalance, CpuConverter.ResWeight,
                paidElf);
            CpuConverter.ElfBalance += paidElf;
            CpuConverter.ResBalance -= resourcePayout;

            return resourcePayout;
        }

        private ulong SellOperation(ulong paidRes)
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