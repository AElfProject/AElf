using AElf.Types;

namespace AElf.Sdk.CSharp.Tests.TestContract
{
    public partial class TokenContract
    {
        private void DoTransfer(Address from, Address to, ulong amount)
        {
            var balanceOfSender = State.Balances[from];
            Assert(balanceOfSender >= amount, $"Insufficient balance. Current balance: {balanceOfSender}");
            var balanceOfReceiver = State.Balances[to];
            State.Balances[from] = balanceOfSender.Sub(amount);
            State.Balances[to] = balanceOfReceiver.Add(amount);
        }
    }
}