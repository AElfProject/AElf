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
            var symbols = new[] {"READ", "WRITE", "STORAGE", "TRAFFIC"};
            var resources = new ResourcesOutput();
            if (State.TokenContract.Value == null)
                return resources;
            
            foreach (var symbol in symbols)
            {
                var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = Context.Self,
                    Symbol = symbol
                });
                resources.Resources.Add(new TokenInfo
                {
                    Symbol = symbol,
                    Amount = balance.Balance
                });
            }

            return resources;
        }
    }
}