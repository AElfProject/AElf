using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    /// <summary>
    /// An isolated environment for runtime contract code.
    /// </summary>
    public interface IExecutive
    {
        IReadOnlyList<ServiceDescriptor> Descriptors { get; }
        IExecutive SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext);
        Task ApplyAsync(ITransactionContext transactionContext);
        string GetJsonStringOfParameters(string methodName, byte[] paramsBytes);
        bool IsView(string methodName);
        byte[] GetFileDescriptorSet();
        IEnumerable<FileDescriptor> GetFileDescriptors();
        
        Hash ContractHash { get; }
        Timestamp LastUsedTime { get; set; }
        string ContractVersion { get; }
    }
}
