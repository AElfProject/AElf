using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management
{
    public interface IManagerService
    {
        List<ManagerResult> GetAllManagers(string chainId);
    }
}