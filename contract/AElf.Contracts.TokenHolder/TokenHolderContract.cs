using System;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TokenHolder
{
    public partial class TokenHolderContract : TokenHolderContractContainer.TokenHolderContractBase
    {
        public override Empty CreateScheme(CreateTokenHolderProfitSchemeInput input)
        {
            if (State.ProfitContract.Value == null)
            {
                State.ProfitContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
            }

            State.ProfitContract.CreateScheme.Send(new CreateSchemeInput
            {
                Manager = Context.Sender,
                IsReleaseAllBalanceEveryTimeByDefault = true
            });

            State.TokenHolderProfitSchemes[Context.Sender] = new TokenHolderProfitScheme
            {
                Symbol = input.Symbol
            };

            return new Empty();
        }

        public override Empty ContributeProfits(ContributeProfitsInput input)
        {
            var scheme = State.TokenHolderProfitSchemes[Context.Sender];
            Assert(scheme != null, "Token holder profit scheme not found.");
            UpdateTokenHolderProfitScheme(ref scheme);
            State.ProfitContract.ContributeProfits.Send(new Profit.ContributeProfitsInput
            {
                SchemeId = scheme.SchemeId,
                Symbol = scheme.Symbol,
                Amount = input.Amount
            });
            return new Empty();
        }

        private void UpdateTokenHolderProfitScheme(ref TokenHolderProfitScheme scheme)
        {
            if (scheme.SchemeId != null) return;
            var originSchemeId = State.ProfitContract.GetManagingSchemeIds.Call(new GetManagingSchemeIdsInput
            {
                Manager = Context.Sender
            }).SchemeIds.FirstOrDefault();
            Assert(originSchemeId != null, "Origin scheme not found.");
            var originScheme = State.ProfitContract.GetScheme.Call(originSchemeId);
            scheme.SchemeId = originScheme.SchemeId;
            State.TokenHolderProfitSchemes[Context.Sender] = scheme;
        }
    }
}