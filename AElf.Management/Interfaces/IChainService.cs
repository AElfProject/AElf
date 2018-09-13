using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface IChainService
    {
        List<ChainResult> GetAllChains();

        void DeployMainChain(DeployArg arg);

        void RemoveMainChain(string chainId);

        DeployTestChainResult DeployTestChain();

        void RemoveTestChain(string chainId);
    }
}