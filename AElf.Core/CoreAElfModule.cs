using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Modularity;

namespace AElf
{
    public class CoreAElfModule : AElfModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddConventionalRegistrar(new AElfDefaultConventionalRegistrar());
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient(typeof(IServiceContainer<>),
                typeof(ServiceContainerFactory<>));
        }
    }

    public static class ServiceProviderExtensions
    {
        public static IEnumerable<T> GetServices<T>(this IServiceProvider provider, IEnumerable<Type> types)
        {
            return types.Select(type => (T) provider.GetService(type));
        }

        /*public static IEnumerable<T> GetServices<T>(this IServiceProvider provider, params Type[] types)
        {
            return provider.GetServices<T>((IEnumerable<Type>) types);
        }*/
    }

    public interface IServiceContainer<T> : IEnumerable<T>
    {
    }


    public class ServiceContainerFactoryOptions<T>
    {
        public List<Type> Types { get; set; } = new List<Type>();
    }

    public class ServiceContainerFactory<T> : IServiceContainer<T>
    {
        private readonly IEnumerable<T> _services;

        public ServiceContainerFactory(IOptionsSnapshot<ServiceContainerFactoryOptions<T>> options,
            IServiceProvider serviceProvider)
        {
            _services = serviceProvider.GetServices<T>(options.Value.Types);
        }


        public IEnumerator<T> GetEnumerator()
        {
            return _services.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}