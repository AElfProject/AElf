using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface ISideChainService
    {
        void Deploy(DeployArg arg);

        void Remove(string chainId);
    }
}