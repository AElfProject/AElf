using System;
using System.Threading.Tasks;

namespace AElf.CrossChain
{
    public interface ICrossChainServer : IDisposable
    {
        Task StartAsync(int chainId);
    }
}