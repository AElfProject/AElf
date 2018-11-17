using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface ILighthouseService
    {
        List<LighthouseResult> GetAllLighthouses(string chainId);
    }
}