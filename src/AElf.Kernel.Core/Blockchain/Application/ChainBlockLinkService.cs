using System.Collections.Generic;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IChainBlockLinkService
    {
        List<ChainBlockLink> GetCachedChainBlockLinks();
        void CleanCachedChainBlockLinks(long height);
    }

    public class ChainBlockLinkService : IChainBlockLinkService, ITransientDependency
    {
        private readonly IChainManager _chainManager;

        public ChainBlockLinkService(IChainManager chainManager)
        {
            _chainManager = chainManager;
        }

        public List<ChainBlockLink> GetCachedChainBlockLinks()
        {
            return _chainManager.GetCachedChainBlockLinks();
        }

        public void CleanCachedChainBlockLinks(long height)
        {
            _chainManager.CleanCachedChainBlockLinks(height);
        }
    }
}
