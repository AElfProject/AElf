using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractHeightInfoProvider
    {
        IReadOnlyDictionary<Address, long> GetContractInfos();
        bool TryGetValue(Address address, out long height);
        void Set(Address address, long height);
        bool TryRemove(Address address, out long height);
    }
    
    public class SmartContractHeightInfoProvider : ISmartContractHeightInfoProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, long> _smartContractHeightInfos =
            new ConcurrentDictionary<Address, long>();
        
        private readonly IReadOnlyDictionary<Address, long> SmartContractHeightInfos;

        public SmartContractHeightInfoProvider()
        {
            SmartContractHeightInfos = new ReadOnlyDictionary<Address, long>(_smartContractHeightInfos);
        }
        
        public IReadOnlyDictionary<Address, long> GetContractInfos()
        {
            return SmartContractHeightInfos;
        }

        public bool TryGetValue(Address address, out long height)
        {
            return _smartContractHeightInfos.TryGetValue(address, out height);
        }

        public void Set(Address address, long height)
        {
            _smartContractHeightInfos[address] = height;
        }

        public bool TryRemove(Address address, out long height)
        {
            return _smartContractHeightInfos.TryRemove(address, out height);
        }
    }
}