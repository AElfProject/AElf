namespace AElf.Kernel.SmartContract.Application
{
    public class TransactionSizeFeePriceProviderTests : SmartContractTestBase
    {
        private readonly ICalculateFeeService _calculateFeeService;

        public TransactionSizeFeePriceProviderTests()
        {
            _calculateFeeService = GetRequiredService<ICalculateFeeService>();
        }

        //wait Liwei new change and update case
    }
}