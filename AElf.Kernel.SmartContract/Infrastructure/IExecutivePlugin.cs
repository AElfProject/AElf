using System;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Types.CSharp;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IExecutivePlugin
    {
        void AfterApply(ServerServiceDefinition definition, IHostSmartContractBridgeContext context, Func<Transaction, TransactionTrace> readOnlyExecutor);
    }
}