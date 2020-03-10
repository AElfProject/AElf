using AElf.Contracts.MultiToken;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public class DonateTransactionRecognizer : SystemTransactionRecognizerBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public DonateTransactionRecognizer(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public override bool IsSystemTransaction(Transaction transaction)
        {
            return CheckSystemContractAddress(transaction.To,
                       _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider
                           .Name)) &&
                   CheckSystemContractMethod(transaction.MethodName,
                       nameof(TokenContractContainer.TokenContractStub.DonateResourceToken));
        }
    }
}