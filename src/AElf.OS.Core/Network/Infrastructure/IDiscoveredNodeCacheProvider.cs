using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface IDiscoveredNodeCacheProvider
    {
        void Add(string nodeEndpoint);
        
        bool TryTake(out string nodeEndpoint);
    }

    public class DiscoveredNodeCacheProvider : IDiscoveredNodeCacheProvider, ISingletonDependency
    {
        private readonly ConcurrentQueue<string> _queuedNodes;

        public DiscoveredNodeCacheProvider()
        {
            _queuedNodes = new ConcurrentQueue<string>();
        }

        public void Add(string nodeEndpoint)
        {
            _queuedNodes.Enqueue(nodeEndpoint);
        }

        public bool TryTake(out string nodeEndpoint)
        {
            return _queuedNodes.TryDequeue(out nodeEndpoint);
        }
    }
}