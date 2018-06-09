using AElf.Kernel.Node;
using AElf.Kernel.Node.Config;
using Autofac;

namespace AElf.Kernel.Modules.AutofacModule
{
    public class MainChainNodeModule : Module
    {
        private INodeConfig _nodeConfig;
        public MainChainNodeModule(INodeConfig nodeConfig)
        {
            _nodeConfig = nodeConfig;
        }

        protected override void Load(ContainerBuilder builder)
        {
            if (_nodeConfig != null)
            {
                builder.RegisterInstance(_nodeConfig).As<INodeConfig>();
            }
            builder.RegisterType<MainChainNode>().As<IAElfNode>();
        }
    }
}