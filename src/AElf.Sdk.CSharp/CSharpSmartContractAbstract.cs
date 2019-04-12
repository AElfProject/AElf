using System.Runtime.CompilerServices;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Sdk.CSharp
{
    public abstract class CSharpSmartContractAbstract : CSharpSmartContract
    {
        internal abstract TransactionExecutingStateSet GetChanges();
        internal abstract void Cleanup();

        protected void Assert(bool asserted, string message = "Assertion failed!")
        {
            if (!asserted)
            {
                throw new AssertionException(message);
            }
        }

        internal abstract void InternalInitialize(ISmartContractBridgeContext bridgeContext);
    }
}