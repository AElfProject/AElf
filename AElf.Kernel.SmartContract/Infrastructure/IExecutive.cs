using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IExecutive
    {
        IExecutive SetMaxCallDepth(int maxCallDepth);

        IExecutive SetHostSmartContractBridgeContext(IHostSmartContractBridgeContext smartContractBridgeContext);
        IExecutive SetTransactionContext(ITransactionContext transactionContext);
        void SetDataCache(IStateCache cache); //temporary solution to let data provider access actor's state cache
        Task ApplyAsync();
        string GetJsonStringOfParameters(string methodName, byte[] paramsBytes);
        byte[] GetFileDescriptorSet();
    }
}