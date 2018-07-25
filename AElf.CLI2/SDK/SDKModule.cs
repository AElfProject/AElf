using Autofac;

namespace AElf.CLI2.SDK
{
    public class SdkModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AElfSdk>().As<IAElfSdk>();
            base.Load(builder);
        }
    }
}