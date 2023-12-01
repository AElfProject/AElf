using AElf.Kernel;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.Types;
using AElf.WebApp.Application.Chain.Infrastructure;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace AElf.WebApp.Application.Chain;

[DependsOn(
    typeof(CoreKernelAElfModule),
    typeof(CoreApplicationWebAppAElfModule),
    typeof(AbpAutoMapperModule)
)]
public class ChainApplicationWebAppAElfModule : AElfModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddAutoMapperObjectMapper<ChainApplicationWebAppAElfModule>();

        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<ChainApplicationWebAppAElfModule>(); });

        context.Services
            .AddSingleton<ITransactionResultStatusCacheProvider, TransactionResultStatusCacheProvider>();

        context.Services.AddStoreKeyPrefixProvide<TransactionFailedResult>("tf");
    }
}

public static class StoreKeyPrefixProviderServiceCollectionExtensions
{
    public static IServiceCollection AddStoreKeyPrefixProvide<T>(
        this IServiceCollection serviceCollection, string prefix)
        where T : IMessage<T>, new()
    {
        serviceCollection.AddTransient<IStoreKeyPrefixProvider<T>>(c =>
            new FastStoreKeyPrefixProvider<T>(prefix));

        return serviceCollection;
    }
}