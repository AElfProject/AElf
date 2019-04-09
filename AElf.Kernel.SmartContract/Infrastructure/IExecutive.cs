using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    /// <summary>
    /// An isolated environment for runtime contract code.
    /// </summary>
    public interface IExecutive
    {
        IExecutive SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext);
        Task ApplyAsync(ITransactionContext transactionContext);
        string GetJsonStringOfParameters(string methodName, byte[] paramsBytes);
        byte[] GetFileDescriptorSet();
    }
}
