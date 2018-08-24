using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface ISideChainService
    {
        void Deploy(string chainId, DeployArg arg);

        void Remove(string chainId);
    }
}