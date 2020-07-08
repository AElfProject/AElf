using System.Collections.Concurrent;
using Volo.Abp.DependencyInjection;

namespace AElf.OS.Network.Infrastructure
{
    public interface IDiscoveredNodeCacheProvider
    {
        void Add(string nodeEndPoint);
        
        bool TryTake(out string nodeEndPoint);
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

        public bool TryTake(out string nodeEndPoint)
        {
            return _queuedNodes.TryDequeue(out nodeEndPoint);
        }
    }
}