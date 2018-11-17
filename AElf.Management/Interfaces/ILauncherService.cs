using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface ILauncherService
    {
        List<LauncherResult> GetAllLaunchers(string chainId);
    }
}