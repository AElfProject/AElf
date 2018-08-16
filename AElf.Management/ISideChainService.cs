using AElf.Management.Models;

namespace AElf.Management
{
    public interface ISideChainService
    {
        void Deploy(string chainId, DeployArg arg);

        void Remove(string chainId);
    }
}