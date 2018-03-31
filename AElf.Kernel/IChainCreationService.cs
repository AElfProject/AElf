using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    /// <summary>
    /// Create a new chain never existing
    /// </summary>
    public interface IChainCreationService
    {
        Task<IChain> CreateNewChainAsync(Hash chainId, ISmartContractZero smartContract);
    }
    
    public class ChainCreationService: IChainCreationService
    {
        private readonly IChainManager _chainManager;
        private readonly ITransactionManager _transactionManager;

        public ChainCreationService(IChainManager chainManager, ITransactionManager transactionManager)
        {
            _chainManager = chainManager;
            _transactionManager = transactionManager;
        }


        public async Task<IChain> CreateNewChainAsync(Hash chainId, ISmartContractZero smartContract)
        {
            var chain = await _chainManager.AddChainAsync(chainId);
            var builder = new GenesisBlockBuilder();
            builder.Build(smartContract);
            await _transactionManager.AddTransactionAsync(builder.Tx);
            await _chainManager.AppendBlockToChainAsync(chain, builder.Block);
            return chain;
        }
    }
}