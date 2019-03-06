using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface ISmartContractAddressService
    {
        Task<Address> GetAddressByContractName(Hash name);

        Address GetConsensusContractAddress();
    }

    public class SmartContractAddressService : ISmartContractAddressService
    {
        private IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;

        public SmartContractAddressService(IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider)
        {
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
        }

        public async Task<Address> GetAddressByContractName(Hash name)
        {
            return null;
        }

        public Address GetConsensusContractAddress()
        {
            return _defaultContractZeroCodeProvider.ContractZeroAddress;
        }
    }
}