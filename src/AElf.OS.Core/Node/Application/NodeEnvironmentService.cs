using AElf.OS.Node.Infrastructure;

namespace AElf.OS.Node.Application
{
    public class NodeEnvironmentService : INodeEnvironmentService
    {
        private readonly INodeEnvironmentProvider _nodeEnvironmentProvider;
        
        public NodeEnvironmentService(INodeEnvironmentProvider nodeEnvironmentProvider)
        {
            _nodeEnvironmentProvider = nodeEnvironmentProvider;
        }

        public string GetAppDataPath()
        {
            return _nodeEnvironmentProvider.GetAppDataPath();
        }
    }
}