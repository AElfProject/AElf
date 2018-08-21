using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface IManagerService
    {
        List<ManagerResult> GetAllManagers(string chainId);
    }
}