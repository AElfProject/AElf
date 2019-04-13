using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Sdk;
using Google.Protobuf.Reflection;

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
        byte[] GetFileDescriptorSet();
    }
}
