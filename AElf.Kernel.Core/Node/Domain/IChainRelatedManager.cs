using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using AElf.Kernel.Node.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Node.Domain
{
    public interface IChainRelatedComponentManager<T>
        where T : IChainRelatedComponent
    {
        T Get(int chainId);
        Task<T> CreateAsync(int chainId);
    }

    public class ChainRelatedComponentManager<T> : IChainRelatedComponentManager<T>, ISingletonDependency
        where T : IChainRelatedComponent
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly ConcurrentDictionary<int, T> _components = new ConcurrentDictionary<int, T>();

        public ChainRelatedComponentManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public T Get(int chainId)
        {
            _components.TryGetValue(chainId, out var obj);
            return obj;
        }

        public async Task<T> CreateAsync(int chainId)
        {
            var obj = _serviceProvider.GetService<T>();
            await obj.StartAsync(chainId);

            return !_components.TryAdd(chainId, obj) ? _components[chainId] : obj;
        }
    }
}