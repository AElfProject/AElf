using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Security;

namespace AElf.SmartContract
{
    public class SmartContractRunnerContainer : ISmartContractRunnerContainer
    {
        private readonly ConcurrentDictionary<int, ISmartContractRunner> _runners =
            new ConcurrentDictionary<int, ISmartContractRunner>();

        private readonly IServiceProvider _serviceProvider;
        private bool _registered;

        public SmartContractRunnerContainer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _registered = false;
            MaybeRegisterRunners();
        }

        private void MaybeRegisterRunners()
        {
            lock (this)
            {
                if (_registered)
                {
                    return;
                }

                foreach (var runner in _serviceProvider.GetServices<ISmartContractRunner>())
                {
                    try
                    {
                        AddRunner(runner.Category, runner);
                    }
                    catch (InvalidParameterException)
                    {
                        // Already added externally
                    }
                }

                _registered = true;
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