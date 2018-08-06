using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;

namespace AElf.ChainController
{
    public class ChainService
    {
        private readonly IChainManagerBasic _chainManager;
        private readonly IBlockManagerBasic _blockManager;
        private readonly ICanonicalHashStore _canonicalHashStore;
        
        public ChainService(IChainManagerBasic chainManager, IBlockManagerBasic blockManager, ICanonicalHashStore canonicalHashStore)
        {
            _chainManager = chainManager;
            _blockManager = blockManager;
            _canonicalHashStore = canonicalHashStore;
        }

        public IBlockChain GetBlockChain(Hash chainId)
        {
            return new BlockChain(chainId, _chainManager, _blockManager, _canonicalHashStore);
        }

        public ILightChain GetLightChain(Hash chainId)
        {
            return new LightChain(chainId, _chainManager, _blockManager, _canonicalHashStore);
        }
    }
}