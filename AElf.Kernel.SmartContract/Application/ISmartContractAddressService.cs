using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AElf.Common;
using AElf.Kernel.SmartContract.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractAddressService
    {
        Address GetAddressByContractName(Hash name);

        void SetAddress(Hash name, Address address);

        Address GetZeroSmartContractAddress();

        ReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping();
    }

    public class SmartContractAddressService : ISmartContractAddressService, ISingletonDependency
    {
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;


        private readonly ConcurrentDictionary<Hash, Address> _hashToAddressMap =
            new ConcurrentDictionary<Hash, Address>();

        public SmartContractAddressService(IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider)
        {
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
        }

        public Address GetAddressByContractName(Hash name)
        {
            _hashToAddressMap.TryGetValue(name, out var address);
            return address;
        }

        public void SetAddress(Hash name, Address address)
        {
            _hashToAddressMap.TryAdd(name, address);
        }

        public Address GetZeroSmartContractAddress()
        {
            return _defaultContractZeroCodeProvider.ContractZeroAddress;
        }

        public ReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping()
        {
            return new ReadOnlyDictionary<Hash, Address>(_hashToAddressMap);
        }
    }
}