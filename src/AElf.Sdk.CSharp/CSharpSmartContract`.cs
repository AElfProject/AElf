using System;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Sdk.CSharp.State;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public partial class CSharpSmartContract<TContractState> where TContractState : ContractState, new()
    {
        public CSharpSmartContractContext Context { get; private set; }

        public TContractState State { get; internal set; }

    }
}