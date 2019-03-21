using Volo.Abp.Modularity;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainServerTestModule : GrpcCrossChainTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<GrpcCrossChainConfigOption>(option =>
            {
                option.LocalClient = false;
                option.LocalServer = true;
            });
            base.ConfigureServices(context);
        }
    }
}