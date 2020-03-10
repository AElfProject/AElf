using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.CrossChain
{
    public class CrossChainTransactionRecognizer : SystemTransactionRecognizerBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public CrossChainTransactionRecognizer(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public override bool IsSystemTransaction(Transaction transaction)
        {
            return CheckSystemContractAddress(transaction.To, _smartContractAddressService.GetAddressByContractName(
                       CrossChainSmartContractAddressNameProvider.Name)) &&
                   CheckSystemContractMethod(transaction.MethodName, CrossChainContractPrivilegeMethodNameProvider.PrivilegeMethodNames.ToArray());
        }
    }
}