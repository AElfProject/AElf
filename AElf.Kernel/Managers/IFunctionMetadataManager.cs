using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.Managers
{
    public interface IFunctionMetadataManager
    {
        Task AddMetadataAsync(int chainId, string name, FunctionMetadata metadata);
        Task<FunctionMetadata> GetMetadataAsync(int chainId, string name);
        Task RemoveMetadataAsync(int chainId, string name);
        Task AddCallGraphAsync(int chainId, SerializedCallGraph callGraph);
        Task<SerializedCallGraph> GetCallGraphAsync(int chainId);
    }
}