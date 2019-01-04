using AElf.Common;
using AElf.Kernel.Managers;

namespace AElf.Sdk.CSharp
{
    public abstract class CSharpSmartContractAbstract : CSharpSmartContract
    {
        internal abstract void SetStateManager(IStateManager stateManager);
        internal abstract void SetContractAddress(Address address);
        internal abstract void Cleanup();

        public void Assert(bool asserted, string message = "Assertion failed!")
        {
            if (!asserted)
            {
                throw new AssertionError(message);
            }
        }
    }
}