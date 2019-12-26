using AElf.Contracts.MultiToken;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.TransactionFees
{
    public partial class TransactionFeesContract
    {
        public override StringValue GetContractName(Empty input)
        {
            return new StringValue
            {
                Value = nameof(TransactionFeesContract)
            };
        }

        public override ResourcesOutput QueryContractResource(Empty input)
        {
            var symbols = new[] {"CPU", "RAM", "NET", "STO"};
            var resources = new ResourcesOutput();
            for (var i = 0; i < symbols.Length; i++)
            {
                var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = Context.Self,
                    Symbol = symbols[i]
                });
                resources.Resources.Add(new TokenInfo
                {
                    Symbol = symbols[i],
                    Amount = balance.Balance
                });
            }

            return resources;
        }
    }
}