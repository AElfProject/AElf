using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface IAkkaService
    {
        Task<List<ActorStateResult>> GetState(string chainId);
    }
}