using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.Manager.Interfaces
{
    public interface IFunctionMetadataManager
    {
        Task AddMetadataAsync(Hash chainId, string name, FunctionMetadata metadata);
        Task<FunctionMetadata> GetMetadataAsync(Hash chainId, string name);
        Task RemoveMetadataAsync(Hash chainId, string name);
        Task AddCallGraphAsync(Hash chainId, SerializedCallGraph callGraph);
        Task<SerializedCallGraph> GetCallGraphAsync(Hash chainId);
    }
}