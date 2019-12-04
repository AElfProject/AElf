using System.Collections.Generic;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IChainBlockLinkService
    {
        ChainBlockLink GetCachedChainBlockLink(Hash blockHash);
        List<ChainBlockLink> GetCachedChainBlockLinks();
        void RemoveCachedChainBlockLink(Hash blockHash);
    }

    public class ChainBlockLinkService : IChainBlockLinkService, ITransientDependency
    {
        private readonly IChainManager _chainManager;

        public ChainBlockLinkService(IChainManager chainManager)
        {
            _chainManager = chainManager;
        }

        public ChainBlockLink GetCachedChainBlockLink(Hash blockHash)
        {
            return _chainManager.GetCachedChainBlockLink(blockHash);
        }

        public List<ChainBlockLink> GetCachedChainBlockLinks()
        {
            return _chainManager.GetCachedChainBlockLinks();
        }

        public void RemoveCachedChainBlockLink(Hash blockHash)
        {
            _chainManager.RemoveCachedChainBlockLink(blockHash);
        }
    }
}
