using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.QuadraticFunding
{
    public partial class QuadraticFundingContract : QuadraticFundingContractContainer.QuadraticFundingContractBase
    {
        public override Empty Initialize(InitializeInput input)
        {
            State.Owner.Value = input.Owner;
            State.VoteSymbol.Value = input.VoteSymbol;
            State.TaxPoint.Value = 100;
            State.CurrentRound.Value = 1;
            State.Interval.Value = 60 * 24 * 3600; // 60 days.

            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            return new Empty();
        }
    }
}