using AElf.Kernel.Types.SmartContract;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Token
{
    public partial class TokenContract : IFeeChargedContract
    {
        [View]
        public ulong GetMethodFee(string methodName)
        {
            return State.MethodFees[methodName];
        }

        public void SetMethodFee(string methodName, ulong fee)
        {
            State.MethodFees[methodName] = fee;
        }
    }
}