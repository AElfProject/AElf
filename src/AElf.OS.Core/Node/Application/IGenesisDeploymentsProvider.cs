using System.Collections.Generic;
using AElf.Standards.ACS0;

namespace AElf.OS.Node.Application
{
    public interface IGenesisDeploymentsProvider
    {
        IEnumerable<SystemContractDeploymentInput> GetDeployments();
    }
}