using Volo.Abp.Modularity;

namespace AElf.CrossChain.Grpc
{
    public class GrpcCrossChainClientTestModule : GrpcCrossChainTestModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            
            Configure<GrpcCrossChainConfigOption>(option =>
            {
                option.LocalClient = true;
                option.LocalServer = false;
            });
        }
    }
}