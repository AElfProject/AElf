using AElf.OS.Node.Infrastructure;

namespace AElf.OS.Node.Application
{
    public class NodeInformationService : INodeInformationService
    {
        private readonly IStaticNodeInformationProvider _staticNodeInformationProvider;
        
        public NodeInformationService(IStaticNodeInformationProvider staticNodeInformationProvider)
        {
            _staticNodeInformationProvider = staticNodeInformationProvider;
        }

        public string GetAppDataPath()
        {
            return _staticNodeInformationProvider.GetAppDataPath();
        }
    }
}