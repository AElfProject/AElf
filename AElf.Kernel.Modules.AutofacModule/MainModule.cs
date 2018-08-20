using Autofac;
using AElf.SmartContract;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MainModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //TODO : REVIEW - probably not a good idea

            var assembly1 = typeof(IStateDictator).Assembly;

            builder.RegisterInstance<IHash>(new Hash()).As<Hash>();

            builder.RegisterAssemblyTypes(assembly1).AsImplementedInterfaces();

            var assembly2 = typeof(ISerializer<>).Assembly;
            builder.RegisterAssemblyTypes(assembly2).AsImplementedInterfaces();

            var assembly3 = typeof(StateDictator).Assembly;
            builder.RegisterAssemblyTypes(assembly3).AsImplementedInterfaces();

            var assembly4 = typeof(ChainController.BlockVaildationService).Assembly;
            builder.RegisterAssemblyTypes(assembly4).AsImplementedInterfaces();

            var assembly5 = typeof(Execution.ParallelTransactionExecutingService).Assembly;
            builder.RegisterAssemblyTypes(assembly5).AsImplementedInterfaces();
            
            var assembly6 = typeof(Node.MainChainNode).Assembly;
            builder.RegisterAssemblyTypes(assembly6).AsImplementedInterfaces();
            
            var assembly7 = typeof(BlockHeader).Assembly;
            builder.RegisterAssemblyTypes(assembly7).AsImplementedInterfaces();

            builder.RegisterType(typeof(Hash)).As(typeof(IHash));

            builder.RegisterGeneric(typeof(Serializer<>)).As(typeof(ISerializer<>));

                        

            
            base.Load(builder);
        }
    }
}