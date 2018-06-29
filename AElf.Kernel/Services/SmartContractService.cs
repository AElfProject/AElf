using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Kernel.Managers;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Types;
using Google.Protobuf;

namespace AElf.Kernel.Services
{

    public class SmartContractService : ISmartContractService
    {
        private readonly ISmartContractManager _smartContractManager;
        private readonly ISmartContractRunnerFactory _smartContractRunnerFactory;
        private readonly ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>> _executivePools = new ConcurrentDictionary<Hash, ConcurrentBag<IExecutive>>();
        private readonly IWorldStateDictator _worldStateDictator;
        private readonly IFunctionMetadataService _functionMetadataService;

        public SmartContractService(ISmartContractManager smartContractManager, ISmartContractRunnerFactory smartContractRunnerFactory, IWorldStateDictator worldStateDictator, IFunctionMetadataService functionMetadataService)
        {
            _smartContractManager = smartContractManager;
            _smartContractRunnerFactory = smartContractRunnerFactory;
            _worldStateDictator = worldStateDictator;
            _functionMetadataService = functionMetadataService;
        }

        private ConcurrentBag<IExecutive> GetPoolFor(Hash account)
        {
            if (!_executivePools.TryGetValue(account, out var pool))
            {
                pool = new ConcurrentBag<IExecutive>();
                _executivePools[account] = pool;
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
            var dataProvider =
                new CachedDataProvider((await _worldStateDictator.SetChainId(chainId).GetAccountDataProvider(account)).GetDataProvider());

            // run smartcontract executive info and return executive

            executive = await runner.RunAsync(reg);

            executive.SetWorldStateManager(_worldStateDictator);
            
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

        public Type GetContractType(SmartContractRegistration registration)
        {
            var runner = _smartContractRunnerFactory.GetRunner(registration.Category);
            if (runner == null)
            {
                throw new NotSupportedException($"Runner for category {registration.Category} is not registered.");
            }
            return runner.GetContractType(registration);
        }
        
        /// <inheritdoc/>
        public async Task DeployContractAsync(Hash chainId, Hash account, SmartContractRegistration registration, bool isPrivileged)
        {
            // get runnner
            var runner = _smartContractRunnerFactory.GetRunner(registration.Category);
            runner.CodeCheck(registration.ContractBytes.ToByteArray(), isPrivileged);

            var contractType = GetContractType(registration);
            //TODO: due to (1) unclear with how to get the contract reference info and (2) function metadata service don't have update logic, we pass empty reference map as parameter and don't support contract call each other for now 
            await _functionMetadataService.DeployContract(chainId, contractType, account, new Dictionary<string, Hash>());

            await _smartContractManager.InsertAsync(account, registration);
        }

        public async Task<IMessage> GetAbiAsync(Hash account)
        {
            var reg = await _smartContractManager.GetAsync(account);
            var runner = _smartContractRunnerFactory.GetRunner(reg.Category);
            return runner.GetAbi(reg);
        }
    }
}
