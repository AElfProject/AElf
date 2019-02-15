using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Domain
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