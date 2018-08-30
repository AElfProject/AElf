using AElf.Kernel.Node;
using AElf.Node.AElfChain;
using Autofac;

namespace AElf.Node
{
    public class NodeAutofacModule: Module
    {
        protected override void Load(ContainerBuilder builder)
        {            
            var assembly = typeof(Node).Assembly;
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            builder.RegisterType<Node>().As<INode>();
            builder.RegisterType<P2PHandler>().PropertiesAutowired();
        }
    }
}