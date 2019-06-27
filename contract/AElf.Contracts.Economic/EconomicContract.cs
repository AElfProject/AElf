using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Economic
{
    public class EconomicContract : EconomicContractContainer.EconomicContractBase
    {
        public override Empty CreateNativeToken(CreateNativeTokenInput input)
        {
            //TODO: for testing, tester cannot monitor zero contract call this method
            /*
            if (Context.Sender != Context.GetZeroSmartContractAddress())
            {
                return new Empty();
            }
            */

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = Context.Variables.NativeSymbol,
                TotalSupply = input.TotalSupply,
                Decimals = input.Decimals,
                IsBurnable = input.IsBurnable,
                TokenName = input.TokenName,
                Issuer = Context.Self,
                LockWhiteList =
                {
                    Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName),
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName),
                    Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName),
                    Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName)
                }
            });
            return new Empty();
        }

        public override Empty IssueNativeToken(IssueNativeTokenInput input)
        {
            //TODO: for testing, tester cannot monitor zero contract call this method
            /*
            if (Context.Sender != Context.GetZeroSmartContractAddress())
            {
                return new Empty();
            }
            */

            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = Context.Variables.NativeSymbol,
                Amount = input.Amount,
                To = input.To,
                Memo = input.Memo
            });
            return new Empty();
        }

        public override Empty InitialMiningReward(Empty input)
        {
            if (Context.Sender != Context.GetZeroSmartContractAddress())
            {
                return new Empty();
            }

            var consensusContractAddress =
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);

            State.TokenContract.Issue.Send(new IssueInput
            {
                To = consensusContractAddress,
                Amount = EconomicContractConstants.MiningReward,
                Symbol = Context.Variables.NativeSymbol,
                Memo = "Initial mining reward."
            });

            return new Empty();
        }

        public override Empty RegisterElectionVotingEvent(Empty input)
        {
            State.ElectionContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);

            State.ElectionContract.RegisterElectionVotingEvent.Send(new Empty());
            return new Empty();
        }
    }
}