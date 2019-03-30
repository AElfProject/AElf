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
                VirtualBalance = 100_0000,
                Weight = 100_0000,
                IsVirtualBalanceEnabled = false,
                IsPurchaseEnabled = true
            }; 
            
            _elfConnector = new Connector
            {
                Symbol = "ELF",
                VirtualBalance = 100_0000,
                Weight = 100_0000,
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false
            };
        }
    }
}