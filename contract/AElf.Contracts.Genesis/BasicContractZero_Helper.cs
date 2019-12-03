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

        private void AssertProposerAuthority(Address proposer)
        {
            var isGenesisOwnerAuthorityRequired = State.ContractDeploymentAuthorityRequired.Value;
            if (!isGenesisOwnerAuthorityRequired)
                return;
            var validationResult = ValidateProposer(proposer);
            Assert(validationResult, "Proposer authority validation failed.");
        }
        
        private bool ValidateProposer(Address address)
        {
            var proposerWhiteListContext = GetProposerWhiteListContext();
            if (proposerWhiteListContext.ProposerAuthorityRequired)
                return proposerWhiteListContext.Proposers.Any(p => p == address);

            return proposerWhiteListContext.Proposers.Count != 0 || CheckAddressIsParliamentMember(address);
        }

        private bool CheckAddressIsParliamentMember(Address address)
        {
            RequireParliamentAuthAddressSet();
            return State.ParliamentAuthContract.ValidateAddressIsParliamentMember.Call(address).Value;
        }
        
        private GetProposerWhiteListContextOutput GetProposerWhiteListContext()
        {
            RequireParliamentAuthAddressSet();
            return State.ParliamentAuthContract.GetProposerWhiteListContext.Call(new Empty());
        }

        private void RemoveContractProposingInfo(Hash inputHash)
        {
            var contractProposingInfo = State.ContractProposingInfoMap[inputHash];
            var isGenesisOwnerAuthorityRequired = State.ContractDeploymentAuthorityRequired.Value;
            if (isGenesisOwnerAuthorityRequired)
                Assert(contractProposingInfo != null, "Contract proposing info not found.");

            if (contractProposingInfo == null)
                return;
            Assert(contractProposingInfo.Proposer == Context.Origin && contractProposingInfo.IsReleased,
                "Unable to remove contract proposing info.");
            State.ContractProposingInfoMap.Remove(inputHash);
        }

        private void AssertAuthorByContractInfo(ContractInfo contractInfo)
        {
            if (contractInfo.IsSystemContract || contractInfo.Author == State.GenesisOwner.Value)
                Assert(Context.Sender == State.GenesisOwner.Value, "No permission.");
            else
                Assert(Context.Origin == contractInfo.Author, "No permission.");
        }

        private Address DecideContractAuthor()
        {
            if (!State.Initialized.Value || !State.ContractDeploymentAuthorityRequired.Value)
                return Context.Origin;
            var proposerWhiteListContext = GetProposerWhiteListContext();
            if (!proposerWhiteListContext.ProposerAuthorityRequired && proposerWhiteListContext.Proposers.Count > 0)
                return Context.Origin;
            if (proposerWhiteListContext.ProposerAuthorityRequired 
                && proposerWhiteListContext.Proposers.Any(p => p == Context.Origin))
                return Context.Origin;
            return State.GenesisOwner.Value;
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