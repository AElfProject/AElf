using System.Runtime.CompilerServices;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Sdk;

[assembly: InternalsVisibleTo("AElf.Sdk.CSharp.Tests")]

namespace AElf.Sdk.CSharp
{
    public abstract class CSharpSmartContractAbstract : CSharpSmartContract
    {
        internal abstract void SetStateProvider(IStateProvider stateProvider);
        internal abstract void SetContractAddress(Address address);
        internal abstract TransactionExecutingStateSet GetChanges();
        internal abstract void Cleanup();

        protected void Assert(bool asserted, string message = "Assertion failed!")
        {
            if (!asserted)
            {
                throw new AssertionError(message);
            }
        }

        internal abstract void InternalInitialize(ISmartContractBridgeContext bridgeContext);
    }
}