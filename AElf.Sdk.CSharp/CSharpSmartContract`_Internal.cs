using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractBridge;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public partial class CSharpSmartContract<TContractState> : CSharpSmartContractAbstract
        where TContractState : ContractState
    {
        internal override void SetStateProvider(IStateProvider stateProvider)
        {
            State.Provider = stateProvider;
        }

        protected override void SetContractAddress(Address address)
        {
            if (address == null)
            {
                throw new Exception($"Input {nameof(address)} is null.");
            }

            var path = new StatePath();
            path.Path.Add(ByteString.CopyFromUtf8(address.GetFormatted()));
            State.Path = path;
        }

        internal override TransactionExecutingStateSet GetChanges()
        {
            return State.GetChanges();
        }

        internal override void Cleanup()
        {
            State.Clear();
        }

        internal override void Initialize(ISmartContractBridgeContext bridgeContext)
        {
            Context = bridgeContext;
        }
    }
}