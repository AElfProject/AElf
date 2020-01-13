using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface IExecutiveProvider
    {
        ConcurrentBag<IExecutive> GetPool(Address address, Hash codeHash);
        void PutExecutive(Address address, IExecutive executive);
        void ClearExecutives(Address address, IEnumerable<Hash> codeHashes);
        void CleanIdleExecutive();
    }

    public class ExecutiveProvider : IExecutiveProvider, ISingletonDependency
    {
        private const int ExecutiveExpirationTime = 3600; // 1 Hour
        private const int ExecutiveClearLimit = 10;
        
        private readonly ConcurrentDictionary<Address, ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>>
            _executivePools =
                new ConcurrentDictionary<Address, ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>>();

        public ILogger<ExecutiveProvider> Logger { get; set; }

        public ExecutiveProvider()
        {
            Logger = NullLogger<ExecutiveProvider>.Instance;
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
        
        public void PutExecutive(Address address, IExecutive executive)
        {
            if (_executivePools.TryGetValue(address, out var dictionary))
            {
                if (dictionary.TryGetValue(executive.ContractHash, out var pool))
                {
                    executive.LastUsedTime = TimestampHelper.GetUtcNow();
                    pool.Add(executive);
                    return;
                }

                Logger.LogDebug($"Lost an executive (no registration {address})");
            }
            else
            {
                Logger.LogDebug($"Lost an executive (no pool {address})");
            }
        }

        public void CleanIdleExecutive()
        {
            foreach (var executivePool in _executivePools)
            {
                foreach (var executiveBag in executivePool.Value.Values)
                {
                    if (executiveBag.Count > ExecutiveClearLimit && executiveBag.Min(o => o.LastUsedTime) <
                        TimestampHelper.GetUtcNow() - TimestampHelper.DurationFromSeconds(ExecutiveExpirationTime))
                    {
                        if (executiveBag.TryTake(out _))
                        {
                            Logger.LogDebug($"Cleaned an idle executive for address {executivePool.Key}.");
                        }
                    }
                }
            }
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