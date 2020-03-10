using AElf.Contracts.MultiToken;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public class ClaimFeeTransactionRecognizer : ISystemTransactionRecognizer
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ClaimFeeTransactionRecognizer(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public bool IsSystemTransaction(Transaction transaction)
        {
            
            // System tx
            nameof(TokenContractContainer.TokenContractStub.ClaimTransactionFees),
            nameof(TokenContractContainer.TokenContractStub.DonateResourceToken),
        }

        private bool IsSystemTransactionMethod(string methodName)
        {
            return new Li
        }
        
    }
}