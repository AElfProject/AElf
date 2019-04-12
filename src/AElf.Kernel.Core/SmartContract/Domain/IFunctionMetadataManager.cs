using System.Threading.Tasks;

namespace AElf.Kernel.SmartContract.Domain
{
    public interface IFunctionMetadataManager
    {
        Task AddMetadataAsync(string name, FunctionMetadata metadata);
        Task<FunctionMetadata> GetMetadataAsync(string name);
        Task RemoveMetadataAsync(string name);
        Task AddCallGraphAsync(SerializedCallGraph callGraph);
        Task<SerializedCallGraph> GetCallGraphAsync();
    }
}