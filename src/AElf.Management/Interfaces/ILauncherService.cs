using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface ILauncherService
    {
        Task<List<LauncherResult>> GetAllLaunchers(string chainId);
    }
}