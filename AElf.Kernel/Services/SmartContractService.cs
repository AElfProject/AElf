using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Kernel.Managers;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel.Services
{

    public class SmartContractService : ISmartContractService
    {
        private readonly ISmartContractManager _smartContractManager;
        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;
        private readonly ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>> _executivePools = new ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>();
        private readonly IWorldStateManager _worldStateManager;

        public SmartContractService(ISmartContractManager smartContractManager, ISmartContractRunnerFactory smartContractRunnerFactory, IWorldStateManager worldStateManager)
        {
            _smartContractManager = smartContractManager;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _worldStateManager = worldStateManager;
        }

        private ConcurrentBag<IExecutive> GetPoolFor(Hash account)
        {
            if (!_executivePools.TryGetValue(account, out var pool))
            {
                // Virtually never happens
                pool = new ConcurrentBag<IExecutive>();
            }
            return pool;
        }

        public async Task<IExecutive> GetExecutiveAsync(Hash account, Hash chainId)
        {
            var pool = GetPoolFor(account);
            IExecutive executive = null;
            if (pool.TryTake(out executive))
                return executive;

            // get registration
            var reg = await _smartContractManager.GetAsync(account);

            // get runnner
            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);

            if (runner == null)
            {
                throw new NotSupportedException($"Runner for category {reg.Category} is not registered.");
            }

            // get account dataprovider
            var dataProvider = (await _worldStateManager.OfChain(chainId)).GetAccountDataProvider(account).GetDataProvider();

            // run smartcontract executive info and return executive

            executive = await runner.RunAsync(reg);
            executive.SetSmartContractContext(new SmartContractContext()
            {
                ChainId = chainId,
                ContractAddress = account,
                DataProvider = dataProvider,
                SmartContractService = this
            });

            return executive;
        }

        public async Task PutExecutiveAsync(Hash account, IExecutive executive)
        {
            // TODO: Maybe reset TransactionContext
            GetPoolFor(account).Add(executive);
            await Task.CompletedTask;
        }

        public async Task DeployContractAsync(Hash account, SmartContractRegistration registration)
        {
            await _smartContractManager.InsertAsync(account, registration);
        }
    }
}
