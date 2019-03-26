using System;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IExecutivePlugin
    {
        void AfterApply(IHostSmartContractBridgeContext context,
            Func<string, IMessage, IMessage> executeReadOnlyHandler);
    }
}