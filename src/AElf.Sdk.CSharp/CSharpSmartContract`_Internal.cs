using System;
using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Sdk;
using Google.Protobuf;

namespace AElf.Sdk.CSharp
{
    public partial class CSharpSmartContract<TContractState> : CSharpSmartContractAbstract
    {

        public CSharpSmartContract()
        {
            State = new TContractState();
            State.Path = new StatePath();;
        }
        internal override TransactionExecutingStateSet GetChanges()
        {
            return State.GetChanges();
        }

        internal override void Cleanup()
        {
            State.Clear();
        }

        internal override void InternalInitialize(ISmartContractBridgeContext bridgeContext)
        {
            if (Context != null)
                throw new InvalidOperationException();
            Context = new CSharpSmartContractContext(bridgeContext);
            State.Context = Context;
        }
    }
}