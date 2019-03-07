using AElf.Common;

namespace AElf.Kernel.Types.SmartContract
{
    public interface ITokenContract
    {
        void Initialize(string symbol, string tokenName, ulong totalSupply, uint decimals);

        void Transfer(Address to, ulong amount);

        void TransferFrom(Address from, Address to, ulong amount);

        void Approve(Address spender, ulong amount);

        void UnApprove(Address spender, ulong amount);

        void Burn(ulong amount);
        void ChargeTransactionFees(ulong feeAmount);
        void ClaimTransactionFees(ulong height);
    }
}