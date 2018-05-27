using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Managers;

namespace AElf.Kernel.Services
{
    public class ChainContextService : IChainContextService
    {
        private readonly ConcurrentDictionary<IHash, IChainContext> _chainContexts =
            new ConcurrentDictionary<IHash, IChainContext>();

        private readonly IWorldStateManager _worldStateManager;

        private readonly ISmartContractRunnerFactory _contractRunnerFactory;
        
        public ChainContextService(
            ISmartContractRunnerFactory contractRunnerFactory, IWorldStateManager worldStateManager)
        {
            _contractRunnerFactory = contractRunnerFactory;
            _worldStateManager = worldStateManager;
        }
    

        public IChainContext GetChainContext(Hash chainId)
        {
            if (_chainContexts.TryGetValue(chainId, out var ctx))
                return ctx;
            
            var result= Task.Factory.StartNew(async () =>
            {
                // create smart contract zero
                var zero = new SmartContractZero(_contractRunnerFactory, _worldStateManager);
                await _worldStateManager.OfChain(chainId);
                
                // initialize smart contract zero
                var adp = _worldStateManager.GetAccountDataProvider(Path.CalculatePointerForAccountZero(chainId));
                await zero.InitializeAsync(adp);
                
                // create chain context
                var context = new ChainContext(zero, chainId);
                
                // cache
                _chainContexts[chainId] = context;
                
                return context;
            }).Unwrap().Result;

            return result;
        }
    }
}