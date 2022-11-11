using System;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core.Extension;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Genesis;

public partial class BasicContractZero
{
    private Address DeploySmartContract(Hash name, int category, byte[] code, bool isSystemContract,
        Address author)
    {
        if (name != null)
            Assert(State.NameAddressMapping[name] == null, "contract name has already been registered before");

        var codeHash = HashHelper.ComputeFrom(code);

        Assert(State.SmartContractRegistrations[codeHash] == null, "contract code has already been deployed before");

        var serialNumber = State.ContractSerialNumber.Value;
        // Increment
        State.ContractSerialNumber.Value = serialNumber + 1;
        var contractAddress = AddressHelper.BuildContractAddress(Context.ChainId, serialNumber);

        var info = new ContractInfo
        {
            SerialNumber = serialNumber,
            Author = author,
            Category = category,
            CodeHash = codeHash,
            IsSystemContract = isSystemContract,
            Version = 1
        };
        State.ContractInfos[contractAddress] = info;

        var reg = new SmartContractRegistration
        {
            Category = category,
            Code = ByteString.CopyFrom(code),
            CodeHash = codeHash,
            IsSystemContract = info.IsSystemContract,
            Version = info.Version
        };

        State.SmartContractRegistrations[reg.CodeHash] = reg;

        Context.DeployContract(contractAddress, reg, name);

        Context.Fire(new ContractDeployed
        {
            CodeHash = codeHash,
            Address = contractAddress,
            Author = author,
            Version = info.Version,
            Name = name
        });

        Context.LogDebug(() => "BasicContractZero - Deployment ContractHash: " + codeHash.ToHex());
        Context.LogDebug(() => "BasicContractZero - Deployment success: " + contractAddress.ToBase58());

        if (name != null)
            State.NameAddressMapping[name] = contractAddress;

        var contractCodeHashList =
            State.ContractCodeHashListMap[Context.CurrentHeight] ?? new ContractCodeHashList();
        contractCodeHashList.Value.Add(codeHash);
        State.ContractCodeHashListMap[Context.CurrentHeight] = contractCodeHashList;

        return contractAddress;
    }

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
            State.ParliamentContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
    }

    private void AssertSenderAddressWith(Address address)
    {
        Assert(Context.Sender == address, "Unauthorized behavior.");
    }

    private Hash CalculateHashFromInput(IMessage input)
    {
        return HashHelper.ComputeFrom(input);
    }

    private bool CheckOrganizationExist(AuthorityInfo authorityInfo)
    {
        return Context.Call<BoolValue>(authorityInfo.ContractAddress,
            nameof(AuthorizationContractContainer.AuthorizationContractReferenceState.ValidateOrganizationExist),
            authorityInfo.OwnerAddress).Value;
    }

    private bool TryClearContractProposingData(Hash inputHash, out ContractProposingInput contractProposingInput)
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

    private void RegisterContractProposingData(Hash proposedContractInputHash)
    {
        var registered = State.ContractProposingInputMap[proposedContractInputHash];
        Assert(registered == null || Context.CurrentBlockTime >= registered.ExpiredTime, "Already proposed.");
        var expirationTimePeriod = GetCurrentContractProposalExpirationTimePeriod();
        State.ContractProposingInputMap[proposedContractInputHash] = new ContractProposingInput
        {
            Proposer = Context.Sender,
            Status = ContractProposingInputStatus.Proposed,
            ExpiredTime = Context.CurrentBlockTime.AddSeconds(expirationTimePeriod)
        };
    }

    private void CreateParliamentOrganizationForInitialControllerAddress(bool proposerAuthorityRequired)
    {
        RequireParliamentContractAddressSet();
        var parliamentProposerWhitelist = State.ParliamentContract.GetProposerWhiteList.Call(new Empty());

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

    private Address DecideNonSystemContractAuthor(Address proposer, Address sender)
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
        return input.CodeCheckReleaseMethod == nameof(DeploySmartContract)
            ? ContractDeploymentInput.Parser.ParseFrom(input.ContractInput).Code
            : ContractUpdateInput.Parser.ParseFrom(input.ContractInput).Code;
    }

    private void AssertCodeCheckProposingInput(ContractCodeCheckInput input)
    {
        Assert(
            input.CodeCheckReleaseMethod == nameof(DeploySmartContract) ||
            input.CodeCheckReleaseMethod == nameof(UpdateSmartContract), "Invalid input.");
    }
    
    private int GetCurrentContractProposalExpirationTimePeriod()
    {
        return State.ContractProposalExpirationTimePeriod.Value == 0
            ? ContractProposalExpirationTimePeriod
            : State.ContractProposalExpirationTimePeriod.Value;
    }
}

public static class AddressHelper
{
    /// <summary>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private static Address BuildContractAddress(Hash chainId, long serialNumber)
    {
        var hash = HashHelper.ConcatAndCompute(chainId, HashHelper.ComputeFrom(serialNumber));
        return Address.FromBytes(hash.ToByteArray());
    }

    public static Address BuildContractAddress(int chainId, long serialNumber)
    {
        return BuildContractAddress(HashHelper.ComputeFrom(chainId), serialNumber);
    }
}