using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface IChainService
    {
        List<ChainResult> GetAllChains();
    }
}