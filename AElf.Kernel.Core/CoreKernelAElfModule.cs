using AElf.Common;
using AElf.Database;
using AElf.Kernel.Types;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(typeof(TypesAElfModule),typeof(DatabaseAElfModule),typeof(CoreAElfModule))]
    public class CoreKernelAElfModule: AElfModule
    {

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddAssemblyOf<CoreKernelAElfModule>();

            /*
            services.AddTransient<IByteSerializer, AElf.Common.Serializers.ProtobufSerializer>();
            
            services.AddTransient(
                typeof(IEqualityIndex<>), 
                typeof(EqualityIndex<,>));
            
            services.AddTransient(
                typeof(IComparisionIndex<>), 
                typeof(ComparisionIndex<,>));
            
            services.AddTransient(typeof(IStateStore<>), typeof(StateStore<>));
            services.AddTransient(typeof(IBlockchainStore<>), typeof(BlockchainStore<>));

            
            
            services.AddKeyValueDbContext<BlockchainKeyValueDbContext>(p => p.UseRedisDatabase());
            services.AddKeyValueDbContext<StateKeyValueDbContext>(p => p.UseRedisDatabase());

            */

        }
    }
}