using System.Collections.Concurrent;
using System.Collections.Generic;
using Org.BouncyCastle.Security;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public class SmartContractRunnerContainer : ISmartContractRunnerContainer
    {
        private readonly ConcurrentDictionary<int, ISmartContractRunner> _runners =
            new ConcurrentDictionary<int, ISmartContractRunner>();

        public SmartContractRunnerContainer(IEnumerable<ISmartContractRunner> runners)
        {
            foreach (var r in runners)
            {
                _runners[r.Category] = r;
            }
        }

        public ISmartContractRunner GetRunner(int category)
        {
            if (_runners.TryGetValue(category, out var runner))
            {
                return runner;
            }

            throw new InvalidParameterException($"The runner for category {category} is not registered.");
        }

        public void AddRunner(int category, ISmartContractRunner runner)
        {
            if (!_runners.TryAdd(category, runner))
            {
                throw new InvalidParameterException($"The runner for category {category} is already registered.");
            }
        }

        public void UpdateRunner(int category, ISmartContractRunner runner)
        {
            if (_runners.ContainsKey(category))
            {
                _runners.AddOrUpdate(category, runner, (key, oldVal) => runner);
            }
        }
    }
}