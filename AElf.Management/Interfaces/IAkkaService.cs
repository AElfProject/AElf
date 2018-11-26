using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface IAkkaService
    {
        List<ActorStateResult> GetState(string chainId);
    }
}