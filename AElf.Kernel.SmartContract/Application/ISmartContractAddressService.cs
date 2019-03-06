using AElf.Common;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractAddressService
    {
        Address GetAddressByContractName(Hash name);

        Address GetConsensusContractAddress();
    }

    public class SmartContractAddressService : ISmartContractAddressService
    {
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;

        public SmartContractAddressService(IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider)
        {
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
        }


        public Address GetAddressByContractName(Hash name)
        {
            throw new System.NotImplementedException();
        }

        public Address GetConsensusContractAddress()
        {
            return _defaultContractZeroCodeProvider.ContractZeroAddress;
        }
    }
}