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

        private readonly ISmartContractManager _smartContractManager;

        private readonly ISmartContractRunnerFactory _contractRunnerFactory;
        
        public ChainContextService(ISmartContractManager smartContractManager, ISmartContractRunnerFactory contractRunnerFactory)
        {
            _smartContractManager = smartContractManager;
            _contractRunnerFactory = contractRunnerFactory;
        }
    

        public IChainContext GetChainContext(Hash chainId)
        {
            if (_chainContexts.TryGetValue(chainId, out var ctx))
                return ctx;
            
            var result= Task.Factory.StartNew(async () =>
            {
                var zero = await _smartContractManager.GetAsync(Hash.Zero);
                var runner = _contractRunnerFactory.GetRunner(zero.Category);
                var smc =await runner.RunAsync(zero);
                var context = new ChainContext((ISmartContractZero) smc, chainId);
                _chainContexts[chainId] = context;
                return context;
            }).Unwrap().Result;

            return result;
        }
    }
}