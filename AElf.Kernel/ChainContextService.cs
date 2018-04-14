using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class ChainContextService : IChainContextService
    {
        private readonly ConcurrentDictionary<IHash, IChainContext> _chainContexts =
            new ConcurrentDictionary<IHash, IChainContext>();

        private readonly ISmartContractManager _smartContractManager;

        private readonly ISmartContractRunner _contractRunner;
        
        public ChainContextService(ISmartContractManager smartContractManager, ISmartContractRunner contractRunner)
        {
            _smartContractManager = smartContractManager;
            _contractRunner = contractRunner;
        }
    

        public IChainContext GetChainContext(Hash chainId)
        {
            if (_chainContexts.TryGetValue(chainId, out var ctx))
                return ctx;
            
            var result= Task.Factory.StartNew(async () =>
            {
                var zero = await _smartContractManager.GetAsync(chainId, Hash.Zero);
                var smc = await _contractRunner.RunAsync(zero);
                var context = new ChainContext((ISmartContractZero) smc, chainId);
                _chainContexts[chainId] = context;
                return context;
            }).Unwrap().Result;

            return result;
        }
    }
}