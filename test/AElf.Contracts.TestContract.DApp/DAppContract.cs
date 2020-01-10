using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenHolder;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.DApp
{
    public partial class DAppContract : DAppContainer.DAppBase
    {
        //just for unit cases
        public override Empty InitializeForUnitTest(InitializeInput input)
        {
            State.TokenHolderContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName);
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

            CreateToken(true);
            CreateTokenHolderProfitScheme();
            SetProfitReceivingInformation(input.ProfitReceiver);
            return new Empty();
        }

        public override Empty Initialize(InitializeInput input)
        {
            State.TokenHolderContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName);
            State.TokenContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);

            CreateToken();
            CreateTokenHolderProfitScheme();
            SetProfitReceivingInformation(input.ProfitReceiver);
            return new Empty();
        }

        public override Empty SignUp(Empty input)
        {
            Assert(State.Profiles[Context.Sender] == null, "Already registered.");
            State.Profiles[Context.Sender] = new Profile
            {
                UserAddress = Context.Sender
            };
            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = DAppConstants.Symbol,
                Amount = DAppConstants.ForNewUser,
                To = Context.Sender
            });

            // Update profile.
            var profile = State.Profiles[Context.Sender];
            profile.Records.Add(new Record
            {
                Type = RecordType.SignUp,
                Timestamp = Context.CurrentBlockTime,
                Description = $"{DAppConstants.Symbol} +{DAppConstants.ForNewUser}"
            });
            State.Profiles[Context.Sender] = profile;

            return new Empty();
        }

        public override Empty Deposit(DepositInput input)
        {
            // (All) DApp Contract can't use TransferFrom method directly.
            State.TokenContract.TransferToContract.Send(new TransferToContractInput
            {
                Symbol = Context.Variables.NativeSymbol,
                Amount = input.Amount
            });

            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = DAppConstants.Symbol,
                Amount = input.Amount,
                To = Context.Sender
            });

            // Update profile.
            var profile = State.Profiles[Context.Sender];
            profile.Records.Add(new Record
            {
                Type = RecordType.Deposit,
                Timestamp = Context.CurrentBlockTime,
                Description = $"{DAppConstants.Symbol} +{input.Amount}"
            });
            State.Profiles[Context.Sender] = profile;

            return new Empty();
        }

        public override Empty Withdraw(WithdrawInput input)
        {
            State.TokenContract.TransferToContract.Send(new TransferToContractInput
            {
                Symbol = DAppConstants.Symbol,
                Amount = input.Amount
            });

            State.TokenContract.Transfer.Send(new TransferInput
            {
                To = Context.Sender,
                Symbol = input.Symbol,
                Amount = input.Amount
            });

            State.TokenHolderContract.RemoveBeneficiary.Send(new RemoveTokenHolderBeneficiaryInput
            {
                Beneficiary = Context.Sender,
                Amount = input.Amount
            });

            // Update profile.
            var profile = State.Profiles[Context.Sender];
            profile.Records.Add(new Record
            {
                Type = RecordType.Withdraw,
                Timestamp = Context.CurrentBlockTime,
                Description = $"{DAppConstants.Symbol} -{input.Amount}"
            });
            State.Profiles[Context.Sender] = profile;

            return new Empty();
        }

        public override Empty Use(Record input)
        {
            State.TokenContract.TransferToContract.Send(new TransferToContractInput
            {
                Symbol = DAppConstants.Symbol,
                Amount = DAppConstants.UseFee
            });

            var primaryTokenSymbol = State.TokenContract.GetPrimaryTokenSymbol.Call(new Empty()).Value;
            var contributeAmount = DAppConstants.UseFee.Div(3);
            State.TokenContract.Approve.Send(new ApproveInput
            {
                Spender = State.TokenHolderContract.Value,
                Symbol = primaryTokenSymbol,
                Amount = contributeAmount
            });

            // Contribute 1/3 profits (ELF) to profit scheme.
            State.TokenHolderContract.ContributeProfits.Send(new ContributeProfitsInput
            {
                SchemeManager = Context.Self,
                Amount = contributeAmount,
                Symbol = primaryTokenSymbol
            });

            // Update profile.
            var profile = State.Profiles[Context.Sender];
            profile.Records.Add(new Record
            {
                Type = RecordType.Withdraw,
                Timestamp = Context.CurrentBlockTime,
                Description = $"{DAppConstants.Symbol} -{DAppConstants.UseFee}"
            });
            State.Profiles[Context.Sender] = profile;

            return new Empty();
        }

        private void CreateToken(bool includingSelf = false)
        {
            var lockWhiteList = new List<Address>
                {Context.GetContractAddressByName(SmartContractConstants.TokenHolderContractSystemName)};
            if (includingSelf)
                lockWhiteList.Add(Context.Self);
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = DAppConstants.Symbol,
                TokenName = "DApp Token",
                Decimals = DAppConstants.Decimal,
                Issuer = Context.Self,
                IsBurnable = true,
                TotalSupply = DAppConstants.TotalSupply,
                LockWhiteList =
                {
                    lockWhiteList
                }
            });
        }

        private void CreateTokenHolderProfitScheme()
        {
            State.TokenHolderContract.CreateScheme.Send(new CreateTokenHolderProfitSchemeInput
            {
                Symbol = DAppConstants.Symbol
            });
        }

        private void SetProfitReceivingInformation(Address receiver)
        {
            State.TokenContract.SetProfitReceivingInformation.Send(new ProfitReceivingInformation
            {
                ContractAddress = Context.Self,
                DonationPartsPerHundred = 1,
                ProfitReceiverAddress = receiver
            });
        }
    }
}