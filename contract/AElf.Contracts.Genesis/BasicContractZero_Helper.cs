using System;
using System.Linq;
using Acs0;
using AElf.Contracts.ParliamentAuth;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis
{
    public partial class BasicContractZero
    {
        private void RequireSenderAuthority()
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

            AssertSenderAddressWith(State.GenesisOwner.Value);
        }

        private void RequireParliamentAuthAddressSet()
        {
            if (State.ParliamentAuthContract.Value == null)
            {
                State.ParliamentAuthContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            }
        }

        private void AssertSenderAddressWith(Address address)
        {
            Assert(Context.Sender.Equals(address), "Unauthorized behavior.");
        }

        private void InitializeGenesisOwner(Address genesisOwner)
        {
            Assert(State.GenesisOwner.Value == null, "Genesis owner already initialized");
            var address = GetContractAddressByName(SmartContractConstants.ParliamentAuthContractSystemName);
            Assert(Context.Sender.Equals(address), "Unauthorized to initialize genesis contract.");
            Assert(genesisOwner != null, "Genesis Owner should not be null.");
            State.GenesisOwner.Value = genesisOwner;
        }

        private Hash CalculateHashFromInput(IMessage input)
        {
            return Hash.FromMessage(input);
        }

        private void AssertDeploymentProposerAuthority(Address proposer)
        {
            var isGenesisOwnerAuthorityRequired = State.ContractDeploymentAuthorityRequired.Value;
            if (!isGenesisOwnerAuthorityRequired)
                return;
            if (!State.ContractProposerAuthorityRequired.Value)
                return;
            var proposerWhiteListContext = GetParliamentProposerWhiteListContext();
            var validationResult = proposerWhiteListContext.ProposerAuthorityRequired
                ? proposerWhiteListContext.Proposers.Any(p => p == proposer)
                : CheckAddressIsParliamentMember(proposer);
            Assert(validationResult, "Proposer authority validation failed.");
        }

        private bool CheckAddressIsParliamentMember(Address address)
        {
            RequireParliamentAuthAddressSet();
            return State.ParliamentAuthContract.ValidateAddressIsParliamentMember.Call(address).Value;
        }

        private bool CheckOrganizationExist(Address address)
        {
            return State.ParliamentAuthContract.ValidateOrganizationExist.Call(address).Value;
        }

        private GetProposerWhiteListContextOutput GetParliamentProposerWhiteListContext()
        {
            RequireParliamentAuthAddressSet();
            return State.ParliamentAuthContract.GetProposerWhiteListContext.Call(new Empty());
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

        private void RequireAuthorityByContractInfo(ContractInfo contractInfo)
        {
            bool validationResult;
            var proposerWhiteListContext = GetParliamentProposerWhiteListContext();
            if (proposerWhiteListContext.ProposerAuthorityRequired)
            {
                validationResult = proposerWhiteListContext.Proposers.Any(p => p == Context.Sender);
            }
            else if (State.ContractProposerAuthorityRequired.Value)
            {
                validationResult = CheckAddressIsParliamentMember(Context.Sender);
            }
            else if (contractInfo.IsSystemContract)
            {
                validationResult = proposerWhiteListContext.Proposers.Any(p => p == Context.Sender) ||
                                   CheckAddressIsParliamentMember(Context.Sender);
            }
            else
            {
                validationResult = Context.Sender == contractInfo.Author;
            }

            Assert(validationResult, "No permission.");
        }

        private Address DecideNormalContractAuthor(Address address)
        {
            if (!State.ContractDeploymentAuthorityRequired.Value)
                return address;
            var proposerWhiteListContext = GetParliamentProposerWhiteListContext();
            if (proposerWhiteListContext.ProposerAuthorityRequired)
            {
                // only proposer in whitelist can be contract author
                Assert(proposerWhiteListContext.Proposers.Any(p => p == address), "Unauthorized proposer.");
                return address;
            }

            if (!State.ContractProposerAuthorityRequired.Value)
                return address;

            // check parliament member
            Assert(CheckAddressIsParliamentMember(address), "Unauthorized proposer.");
            return Context.Self;
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