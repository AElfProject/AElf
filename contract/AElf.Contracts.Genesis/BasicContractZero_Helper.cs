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
            if (!State.ContractProposerAuthorityRequired.Value) 
                return;
            var validationResult = ValidateProposer(proposer);
            Assert(validationResult, "Proposer authority validation failed.");
        }
        
        private bool ValidateProposer(Address address)
        {
            // if parliament authority required and proposer is in whitelist
            // or parliament authority not required and proposer is one of parliament members 
            var proposerWhiteListContext = GetParliamentProposerWhiteListContext();
            return proposerWhiteListContext.ProposerAuthorityRequired
                ? proposerWhiteListContext.Proposers.Any(p => p == address) 
                : CheckAddressIsParliamentMember(address);
        }

        private bool ValidateNewAuthor(Address newAuthor, ContractInfo info)
        {
            // system contract
            if (info.IsSystemContract)
                return false;

            if (!State.ContractDeploymentAuthorityRequired.Value && !State.ContractProposerAuthorityRequired.Value)
                return true;

            // old author is organization address
            if(CheckOrganizationExist(info.Author))
                return CheckOrganizationExist(newAuthor);
            
            // old author is normal address
            var proposerWhiteListContext = GetParliamentProposerWhiteListContext();
            return !proposerWhiteListContext.ProposerAuthorityRequired ||
                   proposerWhiteListContext.Proposers.Any(p => p == newAuthor);
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

        private bool CheckProposerInWhiteList(Address address)
        {
            var proposerWhiteListContext = GetParliamentProposerWhiteListContext();
            return proposerWhiteListContext.ProposerAuthorityRequired &&
                   proposerWhiteListContext.Proposers.Any(p => p == address);
        }
        
        private GetProposerWhiteListContextOutput GetParliamentProposerWhiteListContext()
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
            {
                Assert(Context.Sender == State.GenesisOwner.Value, "No permission.");
                var validationResult = ValidateProposer(Context.Origin);
                Assert(validationResult, "Proposer authority validation failed.");
            }
            else
                Assert(Context.Origin == contractInfo.Author, "No permission.");
        }

        private Address DecideContractAuthor()
        {
            // if genesis contract not initialized
            // or if proposer authority not required
            // or parliament authority required and proposer is in whitelist
            // then author is Context.Origin
            if (!State.Initialized.Value || !State.ContractProposerAuthorityRequired.Value ||
                CheckProposerInWhiteList(Context.Origin))
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