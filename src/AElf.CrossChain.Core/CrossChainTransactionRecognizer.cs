using System.Linq;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.CrossChain
{
    public class CrossChainTransactionRecognizer : ISystemTransactionRecognizer
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public CrossChainTransactionRecognizer(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public bool IsSystemTransaction(Transaction transaction)
        {
            return transaction.To ==
                   _smartContractAddressService.GetAddressByContractName(
                       CrossChainSmartContractAddressNameProvider.Name) &&
                   CrossChainContractPrivilegeMethodNameProvider.PrivilegeMethodNames.Any(methodName =>
                       methodName == transaction.MethodName);
        }
    }
}