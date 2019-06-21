using Acs5;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.ProfitSharing
{
    public class ProfitSharingContract : ProfitSharingContractContainer.ProfitSharingContractBase
    {
        public override Empty InitializeProfitSharingContract(InitializeProfitSharingContractInput input)
        {
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            // Create token
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = input.Symbol,
                TokenName = "Token of Profit Sharing Contract",
                Issuer = Context.Self,
                IsBurnable = true,
                Decimals = 2,
                TotalSupply = ProfitSharingContractConstants.TotalSupply
            });
            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = input.Symbol,
                Amount = ProfitSharingContractConstants.AmountIssueToTokenConverterContract,
                To = Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName)
            });

            // Create Token Connector.
            return new Empty();
        }

        public override Empty SendForFun(Empty input)
        {
            return new Empty();
        }

        public override Empty SetMethodProfitFee(SetMethodProfitFeeInput input)
        {
            State.MethodProfitFees[input.Method] = new MethodProfitFee {SymbolToAmount = {input.SymbolToAmount}};
            return new Empty();
        }

        public override MethodProfitFee GetMethodProfitFee(StringValue input)
        {
            return State.MethodProfitFees[input.Value];
        }
        
        
    }
}