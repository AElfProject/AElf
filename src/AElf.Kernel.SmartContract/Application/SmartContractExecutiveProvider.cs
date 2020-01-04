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
        IReadOnlyDictionary<Address, ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>> GetExecutivePools();
        ConcurrentBag<IExecutive> GetPool(Address address, Hash codeHash);
        bool TryGetExecutiveDictionary(Address address,
            out ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>> dictionary);
        void ClearExecutives(Address address, IEnumerable<Hash> codeHashes);
    }

    public class SmartContractExecutiveProvider : ISmartContractExecutiveProvider, ISingletonDependency
    {
        private readonly ConcurrentDictionary<Address, ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>>
            _executivePools =
                new ConcurrentDictionary<Address, ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>>();

        private IReadOnlyDictionary<Address, ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>> ExecutivePools;

        public ILogger<SmartContractExecutiveProvider> Logger { get; set; }

        public SmartContractExecutiveProvider()
        {
            ExecutivePools =
                new ReadOnlyDictionary<Address, ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>>(_executivePools);
            Logger = NullLogger<SmartContractExecutiveProvider>.Instance;
        }

        public IReadOnlyDictionary<Address, ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>> GetExecutivePools()
        {
            return ExecutivePools;
        }

        public ConcurrentBag<IExecutive> GetPool(Address address, Hash codeHash)
        {
            if (!_executivePools.TryGetValue(address, out var dictionary))
            {
                dictionary = new ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>();
                _executivePools[address] = dictionary;
            }

            if (!dictionary.TryGetValue(codeHash, out var pool))
            {
                pool = new ConcurrentBag<IExecutive>();
                dictionary[codeHash] = pool;
            }

            return pool;
        }

        public bool TryGetExecutiveDictionary(Address address,
            out ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>> dictionary)
        {
            return _executivePools.TryGetValue(address, out dictionary);
        }

        public void ClearExecutives(Address address, IEnumerable<Hash> codeHashes)
        {
            if (!_executivePools.TryGetValue(address, out var dictionary)) return;
            foreach (var codeHash in codeHashes)
            {
                if (dictionary.TryRemove(codeHash, out _))
                    Logger.LogDebug($"Removed executive for address {address} and code hash {codeHash}.");
            }
        }
    }
}