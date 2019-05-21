using System.Threading.Tasks;

namespace AElf.CrossChain.Plugin.Infrastructure
{
    public interface ICrossChainClientProvider
    {
    }

    public interface ICrossChainClientDto
    {
        string RemoteServerHost { set; }
        int RemoteServerPort { set; }
        int RemoteChainId { get; set; }
        int LocalChainId { get; set; }
        bool IsClientToParentChain { get; set; }
    }
}