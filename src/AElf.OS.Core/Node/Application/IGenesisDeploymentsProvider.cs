using System.Collections.Generic;
using Acs0;

namespace AElf.OS.Node.Application
{
    public interface IGenesisDeploymentsProvider
    {
        IEnumerable<SystemContractDeploymentInput> GetDeployments();
    }
}