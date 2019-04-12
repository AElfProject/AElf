using System;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.CSharp.Core;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public interface IExecutivePlugin
    {
        void PostMain(IHostSmartContractBridgeContext context, ServerServiceDefinition definition);
    }
}