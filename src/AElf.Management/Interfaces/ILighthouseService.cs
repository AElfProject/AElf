using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface ILighthouseService
    {
        Task<List<LighthouseResult>> GetAllLighthouses(string chainId);
    }
}