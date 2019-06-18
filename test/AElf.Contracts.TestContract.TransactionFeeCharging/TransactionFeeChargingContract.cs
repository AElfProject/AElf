using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.TransactionFeeCharging
{
    public class TransactionFeeChargingContract : TransactionFeeChargingContractContainer.TransactionFeeChargingContractBase
    {
        public override Empty InitializeTransactionFeeChargingContract(InitializeTransactionFeeChargingContractInput input)
        {
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = input.Symbol,
                TokenName = "Token of Transaction Fee Charging Contract",
                Decimals = 2,
                Issuer = Context.Self,
                IsBurnable = true,
                TotalSupply = 1_000_000_000
            });
            return new Empty();
        }

        public override Empty SendForFun(Empty input)
        {
            return new Empty();
        }
    }
}