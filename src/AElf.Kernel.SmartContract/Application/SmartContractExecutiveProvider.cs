using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractExecutiveProvider
    {
        IReadOnlyDictionary<Address, ConcurrentBag<IExecutive>> GetExecutivePools();
        ConcurrentBag<IExecutive> GetPool(Address address);
        bool TryGetValue(Address address, out ConcurrentBag<IExecutive> executiveBag);
        bool TryRemove(Address address, out ConcurrentBag<IExecutive> executiveBag);
    }

    public class SmartContractExecutiveProvider : ISmartContractExecutiveProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, ConcurrentBag<IExecutive>>
            _executivePools = new ConcurrentDictionary<Address, ConcurrentBag<IExecutive>>();

        private readonly IReadOnlyDictionary<Address, ConcurrentBag<IExecutive>> ExecutivePools;

        public ILogger<SmartContractExecutiveProvider> Logger { get; set; }

        public SmartContractExecutiveProvider()
        {
            ExecutivePools =
                new ReadOnlyDictionary<Address, ConcurrentBag<IExecutive>>(_executivePools);
            Logger = NullLogger<SmartContractExecutiveProvider>.Instance;
        }

        public IReadOnlyDictionary<Address, ConcurrentBag<IExecutive>> GetExecutivePools()
        {
            return ExecutivePools;
        }

        public ConcurrentBag<IExecutive> GetPool(Address address)
        {
            if (!_executivePools.TryGetValue(address, out var pool))
            {
                pool = new ConcurrentBag<IExecutive>();
                _executivePools[address] = pool;
            }

            return pool;
        }

        public bool TryGetValue(Address address, out ConcurrentBag<IExecutive> executiveBag)
        {
            return _executivePools.TryGetValue(address, out executiveBag);
        }

        public bool TryRemove(Address address, out ConcurrentBag<IExecutive> executiveBag)
        {
            return _executivePools.TryRemove(address, out executiveBag);
        }
    }
}