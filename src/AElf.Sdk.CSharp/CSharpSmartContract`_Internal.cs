using System;
using AElf.Types;
using AElf.Kernel.SmartContract;

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
            OnInitialized();
        }

        protected virtual void OnInitialized()
        {
            
        }
    }
}