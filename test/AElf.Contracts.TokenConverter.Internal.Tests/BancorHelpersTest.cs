using System;
using Xunit;
using Shouldly;

namespace AElf.Contracts.TokenConverter
{
    public class BancorHelpersTest
    {
        //init connector
        private Connector _elfConnector;

        private Connector _ramConnector;

        public BancorHelpersTest()
        {
            _ramConnector = new Connector
            {
                Symbol = "RAM",
                VirtualBalance = 50_0000,
                Weight = "0.5",
                IsVirtualBalanceEnabled = false,
                IsPurchaseEnabled = true
            }; 
            
            _elfConnector = new Connector
            {
                Symbol = "ELF",
                VirtualBalance = 100_0000,
                Weight = "0.6",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false
            };
        }
        
        [Fact]
        public void Pow_Test()
        {
            var result1 = BancorHelpers.Pow(1.5m, 1);
            result1.ShouldBe(1.5m);

            BancorHelpers.Pow(1.5m, 2);
        }

        [Fact]
        public void GetAmountToPay_GetReturnFromPaid_Failed()
        {
            //fromConnectorBalance <= 0
            Should.Throw<InvalidValueException>(() => BancorHelpers.GetAmountToPayFromReturn(0, 1000, 1000, 1000, 1000));
            //paidAmount <= 0
            Should.Throw<InvalidValueException>(() => BancorHelpers.GetAmountToPayFromReturn(1000, 1000, 1000, 1000, 0));
            //toConnectorBalance <= 0
            Should.Throw<InvalidValueException>(() => BancorHelpers.GetReturnFromPaid(1000, 1000, 0, 1000, 1000));
            //amountToReceive <= 0
            Should.Throw<InvalidValueException>(() => BancorHelpers.GetReturnFromPaid(1000, 1000, 1000, 1000, 0));
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
       
        private long BuyOperation(long paidElf)
        {
            var getAmountToPayout = BancorHelpers.GetAmountToPayFromReturn(
                _elfConnector.VirtualBalance, Decimal.Parse(_elfConnector.Weight),
                _ramConnector.VirtualBalance, Decimal.Parse(_ramConnector.Weight),
                paidElf);
            return getAmountToPayout;
        }

        private long SellOperation(long paidRes)
        {
            var getReturnFromPaid = BancorHelpers.GetReturnFromPaid(
                _ramConnector.VirtualBalance, Decimal.Parse(_ramConnector.Weight),
                _elfConnector.VirtualBalance, Decimal.Parse(_elfConnector.Weight),
                paidRes);
            return getReturnFromPaid;
        }
    }
}