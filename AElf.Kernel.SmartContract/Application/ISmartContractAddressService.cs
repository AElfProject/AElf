using System.Collections.Concurrent;
using AElf.Common;
using AElf.Kernel.SmartContract.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractAddressService
    {
        Address GetAddressByContractName(Hash name);

        Address GetConsensusContractAddress();

        void SetAddress(Hash name, Address address);
    }

    public class SmartContractAddressService : ISmartContractAddressService, ISingletonDependency
    {
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;


        private readonly ConcurrentDictionary<Hash, Address> _hashToAddressMap = new ConcurrentDictionary<Hash, Address>();

        public SmartContractAddressService(IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider)
        {
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
        }


        public Address GetAddressByContractName(Hash name)
        {
            _hashToAddressMap.TryGetValue(name, out var address);
            return address;
        }

        public Address GetConsensusContractAddress()
        {
            return _defaultContractZeroCodeProvider.ContractZeroAddress;
        }

        public void SetAddress(Hash name, Address address)
        {
            _hashToAddressMap.TryAdd(name, address);
        }
    }
}