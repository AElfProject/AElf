using System;
using Acs0;
using Acs3;
using AElf.Contracts.Parliament;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis
{
    public partial class BasicContractZero
    {
        private void RequireSenderAuthority(Address address = null)
        {
            if (!State.Initialized.Value)
            {
                // only authority of contract zero is valid before initialization
                AssertSenderAddressWith(Context.Self);
                return;
            }

            var isGenesisOwnerAuthorityRequired = State.ContractDeploymentAuthorityRequired.Value;
            if (!isGenesisOwnerAuthorityRequired)
                return;

            if (address != null)
                AssertSenderAddressWith(address);
        }

        private void RequireParliamentContractAddressSet()
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }
        }

        private void AssertSenderAddressWith(Address address)
        {
            Assert(Context.Sender == address, "Unauthorized behavior.");
        }

        private Hash CalculateHashFromInput(IMessage input)
        {
            return Hash.FromMessage(input);
        }

        private bool CheckOrganizationExist(AuthorityStuff authorityStuff)
        {
            return Context.Call<BoolValue>(authorityStuff.ContractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.ValidateOrganizationExist),
                authorityStuff.OwnerAddress).Value;
        }

        private bool TryClearContractProposingInput(Hash inputHash, out ContractProposingInput contractProposingInput)
        {
            contractProposingInput = State.ContractProposingInputMap[inputHash];
            var isGenesisOwnerAuthorityRequired = State.ContractDeploymentAuthorityRequired.Value;
            if (isGenesisOwnerAuthorityRequired)
                Assert(
                    contractProposingInput != null, "Contract proposing data not found.");

            if (contractProposingInput == null)
                return false;

            Assert(contractProposingInput.Status == ContractProposingInputStatus.CodeChecked,
                "Invalid contract proposing status.");
            State.ContractProposingInputMap.Remove(inputHash);
            return true;
        }

        private void CreateParliamentOrganizationForInitialControllerAddress(bool proposerAuthorityRequired)
        {
            RequireParliamentContractAddressSet();
            var parliamentProposerWhitelist = State.ParliamentContract.GetProposerWhiteListContext.Call(new Empty());

            var isWhiteListEmpty = parliamentProposerWhitelist.Proposers.Count == 0;
            State.ParliamentContract.CreateOrganizationBySystemContract.Send(new CreateOrganizationBySystemContractInput
            {
                OrganizationCreationInput = new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = MinimalApprovalThreshold,
                        MinimalVoteThreshold = MinimalVoteThresholdThreshold,
                        MaximalRejectionThreshold = MaximalRejectionThreshold,
                        MaximalAbstentionThreshold = MaximalAbstentionThreshold
                    },
                    ProposerAuthorityRequired = proposerAuthorityRequired,
                    ParliamentMemberProposingAllowed = isWhiteListEmpty
                },
                OrganizationAddressFeedbackMethod = nameof(SetInitialControllerAddress)
            });
        }

        private void AssertAuthorityByContractInfo(ContractInfo contractInfo, Address address)
        {
            Assert(contractInfo.Author == Context.Self || address == contractInfo.Author, "No permission.");
        }

        private bool ValidateProposerAuthority(Address contractAddress, Address organizationAddress, Address proposer)
        {
            return Context.Call<BoolValue>(contractAddress,
                nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.ValidateProposerInWhiteList),
                new ValidateProposerInWhiteListInput
                {
                    OrganizationAddress = organizationAddress,
                    Proposer = proposer
                }).Value;
        }

        private Address DecideNormalContractAuthor(Address proposer, Address sender)
        {
            if (!State.ContractDeploymentAuthorityRequired.Value)
                return sender;

            var contractDeploymentController = State.ContractDeploymentController.Value;
            var isProposerInWhiteList = ValidateProposerAuthority(contractDeploymentController.ContractAddress,
                contractDeploymentController.OwnerAddress, proposer);
            return isProposerInWhiteList ? proposer : Context.Self;
        }

        private ByteString ExtractCodeFromContractCodeCheckInput(ContractCodeCheckInput input)
        {
            return input.IsContractDeployment
                ? ContractDeploymentInput.Parser.ParseFrom(input.ContractInput).Code
                : ContractUpdateInput.Parser.ParseFrom(input.ContractInput).Code;
        }
    }

    public static class AddressHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private static Address BuildContractAddress(Hash chainId, ulong serialNumber)
        {
            var hash = Hash.FromTwoHashes(chainId, Hash.FromRawBytes(serialNumber.ToBytes()));
            return Address.FromBytes(hash.ToByteArray());
        }

        public static Address BuildContractAddress(int chainId, ulong serialNumber)
        {
            return BuildContractAddress(chainId.ToHash(), serialNumber);
        }
    }
}