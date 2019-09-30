using Acs1;
using AElf.Contracts.MultiToken;
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
                TotalSupply = TransactionFeeChargingContractConstants.TotalSupply,
                LockWhiteList =
                {
                    Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName),
                    Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName)
                }
            });
            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = input.Symbol,
                Amount = TransactionFeeChargingContractConstants.AmountIssueToTokenConverterContract,
                To = Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName)
            });
            return new Empty();
        }

        public override Empty SetMethodFee(TokenAmounts input)
        {
            State.TransactionFees[input.Method] = input;
            return new Empty();
        }

        public override TokenAmounts GetMethodFee(MethodName input)
        {
            return State.TransactionFees[input.Name];
        }

        public override Empty SendForFun(Empty input)
        {
            return new Empty();
        }

        public override Empty SupposedToFail(Empty input)
        {
            Assert(false, "Fate!");
            return new Empty();
        }
    }
}