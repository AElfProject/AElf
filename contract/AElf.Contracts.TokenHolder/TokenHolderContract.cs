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

        public override Empty AddBeneficiary(AddTokenHolderBeneficiaryInput input)
        {
            var scheme = GetValidScheme(Context.Sender);
            Assert(scheme != null, "Token holder profit scheme not found.");
            UpdateTokenHolderProfitScheme(ref scheme);
            State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
            {
                SchemeId = scheme.SchemeId,
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = input.Beneficiary,
                    Shares = input.Shares
                }
            });
            return new Empty();
        }

        public override Empty RemoveBeneficiary(RemoveTokenHolderBeneficiaryInput input)
        {
            var scheme = GetValidScheme(Context.Sender);
            State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
            {
                SchemeId = scheme.SchemeId,
                Beneficiary = input.Beneficiary
            });
            return new Empty();
        }

        public override Empty ContributeProfits(ContributeProfitsInput input)
        {
            var scheme = GetValidScheme(input.SchemeManager);
            State.ProfitContract.ContributeProfits.Send(new Profit.ContributeProfitsInput
            {
                SchemeId = scheme.SchemeId,
                Symbol = scheme.Symbol,
                Amount = input.Amount
            });
            return new Empty();
        }

        public override Empty DistributeProfits(DistributeProfitsInput input)
        {
            var scheme = GetValidScheme(input.SchemeManager);
            State.ProfitContract.DistributeProfits.Send(new Profit.DistributeProfitsInput
            {
                SchemeId = scheme.SchemeId,
                Symbol = input.Symbol ?? scheme.Symbol,
                Period = scheme.Period
            });
            return new Empty();
        }

        public override Empty RegisterForProfits(RegisterForProfitsInput input)
        {
            var scheme = GetValidScheme(input.SchemeManager);
            State.TokenContract.Lock.Send(new LockInput
            {
                LockId = Context.TransactionId,
                Symbol = scheme.Symbol,
                Address = Context.Sender,
                Amount = input.Amount,
            });
            State.LockIds[input.SchemeManager][Context.Sender] = Context.TransactionId;
            State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
            {
                SchemeId = scheme.SchemeId,
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = Context.Sender,
                    Shares = input.Amount
                }
            });
            return new Empty();
        }

        public override Empty Withdraw(Address input)
        {
            var scheme = GetValidScheme(input);
            var amount = State.TokenContract.GetLockedAmount.Call(new GetLockedAmountInput
            {
                Address = Context.Sender,
                LockId = State.LockIds[input][Context.Sender],
                Symbol = scheme.Symbol
            }).Amount;
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Address = Context.Sender,
                LockId = State.LockIds[input][Context.Sender],
                Amount = amount,
                Symbol = scheme.Symbol
            });

            // TODO: Remove this key.
            State.LockIds[input][Context.Sender] = null;
            State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
            {
                SchemeId = scheme.SchemeId,
                Beneficiary = Context.Sender
            });
            return new Empty();
        }

        public override TokenHolderProfitScheme GetScheme(Address input)
        {
            return State.TokenHolderProfitSchemes[input] ?? new TokenHolderProfitScheme();
        }

        private TokenHolderProfitScheme GetValidScheme(Address manager)
        {
            var scheme = State.TokenHolderProfitSchemes[manager];
            Assert(scheme != null, "Token holder profit scheme not found.");
            UpdateTokenHolderProfitScheme(ref scheme);
            return scheme;
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