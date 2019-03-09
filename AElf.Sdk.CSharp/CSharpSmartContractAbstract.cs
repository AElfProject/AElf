using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContractBridge;

namespace AElf.Sdk.CSharp
{
    public abstract class CSharpSmartContractAbstract : CSharpSmartContract
    {
        internal abstract void SetStateProvider(IStateProvider stateProvider);
        protected abstract void SetContractAddress(Address address);
        internal abstract TransactionExecutingStateSet GetChanges();
        internal abstract void Cleanup();

        protected void Assert(bool asserted, string message = "Assertion failed!")
        {
            if (!asserted)
            {
                throw new AssertionError(message);
            }
        }

        internal abstract void Initialize(ISmartContractBridgeContext bridgeContext);
    }
}