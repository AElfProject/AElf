using System;
using System.Threading.Tasks;
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
        internal override async Task<TransactionExecutingStateSet> GetChanges()
        {
            return await State.GetChanges();
        }

        internal override async Task Cleanup()
        {
            await State.Clear();
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