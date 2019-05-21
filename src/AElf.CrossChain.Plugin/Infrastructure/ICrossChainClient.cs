using System.Threading.Tasks;
using AElf.CrossChain.Cache;

namespace AElf.CrossChain.Plugin.Infrastructure
{
    public interface ICrossChainClient
    {
        Task ReqeustCrossChainDataAsync(int chainId);
    }
}