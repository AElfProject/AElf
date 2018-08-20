using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management
{
    public interface IChainService
    {
        List<ChainResult> GetAllChains();
    }
}