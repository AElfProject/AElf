using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.MethodCallThreshold;
using AElf.Contracts.TestContract.TransactionFeeCharging;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.Parliament.InitializeInput;

namespace AElf.Contracts.Economic.TestBase;

// ReSharper disable InconsistentNaming
public partial class EconomicContractsTestBase
{
    #region Private Preperties

    private const int Category = KernelConstants.CodeCoverageRunnerCategory;

    #endregion

    #region Contract Address

    protected Dictionary<ProfitType, Hash> ProfitItemsIds { get; set; }

    private Address _zeroAddress;
    protected new Address ContractZeroAddress => GetZeroContract();

    private Address _tokenAddress;
    protected Address TokenContractAddress => GetOrDeployContract(Contracts.MultiToken, ref _tokenAddress);

    private Address _voteAddress;
    protected Address VoteContractAddress => GetOrDeployContract(Contracts.Vote, ref _voteAddress);

    private Address _profitAddress;
    protected Address ProfitContractAddress => GetOrDeployContract(Contracts.Profit, ref _profitAddress);

    private Address _electionAddress;
    protected Address ElectionContractAddress => GetOrDeployContract(Contracts.Election, ref _electionAddress);

    private Address _consensusAddress;
    protected Address ConsensusContractAddress => GetOrDeployContract(Contracts.AEDPoS, ref _consensusAddress);

    private Address _tokenConverterAddress;

    protected Address TokenConverterContractAddress =>
        GetOrDeployContract(Contracts.TokenConverter, ref _tokenConverterAddress);

    private Address _treasuryAddress;
    protected Address TreasuryContractAddress => GetOrDeployContract(Contracts.Treasury, ref _treasuryAddress);

    private Address _feeChargingAddress;

    protected Address TransactionFeeChargingContractAddress =>
        GetOrDeployContract(Contracts.TransactionFee, ref _feeChargingAddress);

    private Address _associationAddress;
    protected Address AssociationContractAddress => GetOrDeployContract(Contracts.Association, ref _associationAddress);

    private Address _methodCallThresholdAddress;

    protected Address MethodCallThresholdContractAddress =>
        GetOrDeployContract(TestContracts.MethodCallThreshold, ref _methodCallThresholdAddress);

    private Address _economicAddress;
    protected Address EconomicContractAddress => GetOrDeployContract(Contracts.Economic, ref _economicAddress);

    private Address _parliamentAddress;

    protected Address ParliamentContractAddress =>
        GetOrDeployContract(Contracts.Parliament, ref _parliamentAddress);

    private Address _referendumAddress;

    protected Address ReferendumContractAddress =>
        GetOrDeployContract(Contracts.Referendum, ref _referendumAddress);

    private Address _tokenHolderAddress;

    protected Address TokenHolderContractAddress =>
        GetOrDeployContract(Contracts.TokenHolder, ref _tokenHolderAddress);

    private Address _configurationAddress;

    protected Address ConfigurationAddress =>
        GetOrDeployContract(Contracts.Configuration, ref _configurationAddress);

    #endregion

    #region Contract Stub

    internal BasicContractZeroContainer.BasicContractZeroStub BasicContractZeroStub =>
        GetContractZeroTester(BootMinerKeyPair);

    internal TokenContractContainer.TokenContractStub TokenContractStub => GetTokenContractTester(BootMinerKeyPair);

    internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub =>
        GetTokenConverterContractTester(BootMinerKeyPair);

    internal VoteContractContainer.VoteContractStub VoteContractStub =>
        GetVoteContractTester(BootMinerKeyPair);

    internal ProfitContractContainer.ProfitContractStub ProfitContractStub =>
        GetProfitContractTester(BootMinerKeyPair);

    internal ElectionContractContainer.ElectionContractStub ElectionContractStub =>
        GetElectionContractTester(BootMinerKeyPair);

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub =>
        GetConsensusContractTester(BootMinerKeyPair);

    internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub =>
        GetTreasuryContractTester(BootMinerKeyPair);

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub =>
        GetParliamentContractTester(BootMinerKeyPair);

    internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
        TransactionFeeChargingContractStub => GetTransactionFeeChargingContractTester(BootMinerKeyPair);

    internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub MethodCallThresholdContractStub =>
        GetMethodCallThresholdContractTester(BootMinerKeyPair);

    internal EconomicContractContainer.EconomicContractStub EconomicContractStub =>
        GetEconomicContractTester(BootMinerKeyPair);

    internal ConfigurationContainer.ConfigurationStub ConfigurationStub =>
        GetConfigurationContractTester(BootMinerKeyPair);

    #endregion

    #region Get Contract Stub Tester

    internal BasicContractZeroContainer.BasicContractZeroStub GetContractZeroTester(ECKeyPair keyPair)
    {
        return GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, keyPair);
    }

    internal TokenContractContainer.TokenContractStub GetTokenContractTester(ECKeyPair keyPair)
    {
        return GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, keyPair);
    }

    internal TokenConverterContractContainer.TokenConverterContractStub GetTokenConverterContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
            keyPair);
    }

    internal VoteContractContainer.VoteContractStub GetVoteContractTester(ECKeyPair keyPair)
    {
        return GetTester<VoteContractContainer.VoteContractStub>(VoteContractAddress, keyPair);
    }

    internal ProfitContractContainer.ProfitContractStub GetProfitContractTester(ECKeyPair keyPair)
    {
        return GetTester<ProfitContractContainer.ProfitContractStub>(ProfitContractAddress, keyPair);
    }

    internal ElectionContractContainer.ElectionContractStub GetElectionContractTester(ECKeyPair keyPair)
    {
        return GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress, keyPair);
    }

    internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusContractTester(ECKeyPair keyPair)
    {
        return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
    }

    internal TreasuryContractContainer.TreasuryContractStub GetTreasuryContractTester(ECKeyPair keyPair)
    {
        return GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress, keyPair);
    }

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
        GetTransactionFeeChargingContractTester(ECKeyPair keyPair)
    {
        return GetTester<TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub>(
            TransactionFeeChargingContractAddress, keyPair);
    }

    internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub
        GetMethodCallThresholdContractTester(
            ECKeyPair keyPair)
    {
        return GetTester<MethodCallThresholdContractContainer.MethodCallThresholdContractStub>(
            MethodCallThresholdContractAddress,
            keyPair);
    }

    internal EconomicContractContainer.EconomicContractStub GetEconomicContractTester(ECKeyPair keyPair)
    {
        return GetTester<EconomicContractContainer.EconomicContractStub>(EconomicContractAddress, keyPair);
    }

    internal ConfigurationContainer.ConfigurationStub GetConfigurationContractTester(ECKeyPair keyPair)
    {
        return GetTester<ConfigurationContainer.ConfigurationStub>(ConfigurationAddress, keyPair);
    }

    #endregion

    #region Get or Deploy Contract

    private Address GetZeroContract()
    {
        if (_zeroAddress != null)
            return _zeroAddress;

        _zeroAddress = ContractAddressService.GetZeroSmartContractAddress();
        return _zeroAddress;
    }

    private Address GetOrDeployContract(Contracts contract, ref Address address)
    {
        if (address != null)
            return address;

        address = AsyncHelper.RunSync(() => DeployContract(contract));
        return address;
    }

    private Address GetOrDeployContract(TestContracts contract, ref Address address)
    {
        if (address != null)
            return address;

        address = AsyncHelper.RunSync(() => DeployContract(contract));
        return address;
    }

    private async Task<Address> DeployContract(Contracts contract)
    {
        var code = Codes.Single(kv => kv.Key.Contains(contract.ToString())).Value;
        Hash hash;
        switch (contract)
        {
            case Contracts.Parliament:
                hash = HashHelper.ComputeFrom("AElf.ContractNames.Parliament");
                break;
            case Contracts.AEDPoS:
                hash = HashHelper.ComputeFrom("AElf.ContractNames.Consensus");
                break;
            case Contracts.MultiToken:
                hash = HashHelper.ComputeFrom("AElf.ContractNames.Token");
                break;
            case Contracts.TransactionFee:
                hash = HashHelper.ComputeFrom("AElf.ContractNames.TransactionFeeCharging");
                break;
            default:
                hash = HashHelper.ComputeFrom($"AElf.ContractNames.{contract.ToString()}");
                break;
        }

        var address = await DeploySystemSmartContract(Category, code, hash, BootMinerKeyPair);

        return address;
    }

    private async Task<Address> DeployContract(TestContracts contract)
    {
        var code = Codes.Single(kv => kv.Key.Contains(contract.ToString())).Value;
        var hash = HashHelper.ComputeFrom($"AElf.ContractNames.{contract.ToString()}");
        var address = await DeploySystemSmartContract(Category, code, hash, BootMinerKeyPair);

        return address;
    }

    protected void DeployAllContracts()
    {
        _ = VoteContractAddress;
        _ = ProfitContractAddress;
        _ = EconomicContractAddress;
        _ = ElectionContractAddress;
        _ = TreasuryContractAddress;
        _ = TransactionFeeChargingContractAddress;
        _ = ParliamentContractAddress;
        _ = TokenConverterContractAddress;
        _ = ConsensusContractAddress;
        _ = MethodCallThresholdContractAddress;
        _ = ReferendumContractAddress;
        _ = TokenContractAddress;
        _ = TokenHolderContractAddress;
        _ = AssociationContractAddress;
    }

    #endregion

    #region Contract Initialize

    protected async Task InitializeTreasuryConverter()
    {
        {
            var result =
                await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());
            CheckResult(result.TransactionResult);
        }
        {
            var result =
                await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(
                    new Empty());
            CheckResult(result.TransactionResult);
        }
        //get profit ids
        {
            var profitIds = (await ProfitContractStub.GetManagingSchemeIds.CallAsync(
                new GetManagingSchemeIdsInput
                {
                    Manager = TreasuryContractAddress
                })).SchemeIds;
            ProfitItemsIds = new Dictionary<ProfitType, Hash>
            {
                { ProfitType.Treasury, profitIds[0] },
                { ProfitType.MinerReward, profitIds[1] },
                { ProfitType.BasicMinerReward, profitIds[2] },
                { ProfitType.FlexibleReward, profitIds[3] },
                { ProfitType.WelcomeReward, profitIds[4] }
            };
        }
        {
            var profitIds = (await ProfitContractStub.GetManagingSchemeIds.CallAsync(
                new GetManagingSchemeIdsInput
                {
                    Manager = ElectionContractAddress
                })).SchemeIds;
            ProfitItemsIds.Add(ProfitType.BackupSubsidy, profitIds[0]);
            ProfitItemsIds.Add(ProfitType.CitizenWelfare, profitIds[1]);
        }
    }

    protected async Task InitializeElection()
    {
        var minerList = InitialCoreDataCenterKeyPairs.Select(o => o.PublicKey.ToHex()).ToArray();
        var result = await ElectionContractStub.InitialElectionContract.SendAsync(new InitialElectionContractInput
        {
            MaximumLockTime = 1080 * 86400,
            MinimumLockTime = 7 * 86400,
            TimeEachTerm = EconomicContractsTestConstants.PeriodSeconds,
            MinerList = { minerList },
            MinerIncreaseInterval = EconomicContractsTestConstants.MinerIncreaseInterval
        });
        CheckResult(result.TransactionResult);
    }

    protected async Task InitializeEconomicContract()
    {
        //create native token
        {
            var result = await EconomicContractStub.InitialEconomicSystem.SendAsync(new InitialEconomicSystemInput
            {
                NativeTokenDecimals = EconomicContractsTestConstants.Decimals,
                IsNativeTokenBurnable = EconomicContractsTestConstants.IsBurnable,
                NativeTokenSymbol = EconomicContractsTestConstants.NativeTokenSymbol,
                NativeTokenTotalSupply = EconomicContractsTestConstants.TotalSupply,
                MiningRewardTotalAmount = EconomicContractsTestConstants.TotalSupply / 5
            });
            CheckResult(result.TransactionResult);
        }
    }

    protected async Task InitialMiningRewards()
    {
        //Issue native token to core data center keyPairs
        {
            var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
            {
                Amount = EconomicContractsTestConstants.TotalSupply / 5,
                To = TreasuryContractAddress,
                Memo = "Set mining rewards."
            });
            CheckResult(result.TransactionResult);
        }
    }

    protected async Task InitializeToken()
    {
        //issue some to default user and buy resource
        {
            var issueResult = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
            {
                Amount = 1000_000_00000000L,
                To = BootMinerAddress,
                Memo = "Used to transfer other testers"
            });
            CheckResult(issueResult.TransactionResult);

            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = BootMinerAddress,
                Symbol = EconomicContractsTestConstants.NativeTokenSymbol
            });
            balance.Balance.ShouldBe(1000_000_00000000L);
        }

        {
            var addresses = new Address[] { Address.FromBase58("BHN8oN7D8kWZL9YW3aqD3dct4F83zqAd3CgaBTWucUiNSakcp"), Address.FromBase58("2EeEu68HG5MsiUaoaJW8kQ3LBQ2sJHQVRgikkHn2LNsFs2rMit") };
            foreach (var address in addresses)
            {
                var issueResult = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = 10000_00000000,
                    To = address,
                    Memo = "Used to transfer other testers"
                });
                CheckResult(issueResult.TransactionResult);

                var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = address,
                    Symbol = EconomicContractsTestConstants.NativeTokenSymbol
                });
                balance.Balance.ShouldBe(10000_00000000L);
            }
        }

        foreach (var coreDataCenterKeyPair in CoreDataCenterKeyPairs)
        {
            var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
            {
                Amount = EconomicContractsTestConstants.LockTokenForElection * 10,
                To = Address.FromPublicKey(coreDataCenterKeyPair.PublicKey),
                Memo = "Used to announce election."
            });
            CheckResult(result.TransactionResult);
        }

        foreach (var validationDataCenterKeyPair in ValidationDataCenterKeyPairs)
        {
            var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
            {
                Amount = EconomicContractsTestConstants.UserInitializeTokenAmount,
                To = Address.FromPublicKey(validationDataCenterKeyPair.PublicKey),
                Memo = "Used to announce election."
            });
            CheckResult(result.TransactionResult);
        }

        foreach (var validationDataCenterCandidateKeyPair in ValidationDataCenterCandidateKeyPairs)
        {
            var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
            {
                Amount = EconomicContractsTestConstants.UserInitializeTokenAmount,
                To = Address.FromPublicKey(validationDataCenterCandidateKeyPair.PublicKey),
                Memo = "Used to announce election."
            });
            CheckResult(result.TransactionResult);
        }

        foreach (var voterKeyPair in VoterKeyPairs)
        {
            var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
            {
                Amount = EconomicContractsTestConstants.UserInitializeTokenAmount,
                To = Address.FromPublicKey(voterKeyPair.PublicKey),
                Memo = "Used to vote data center."
            });
            CheckResult(result.TransactionResult);
        }
    }

    protected async Task InitializeAElfConsensus()
    {
        {
            var result = await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                new InitialAElfConsensusContractInput
                {
                    PeriodSeconds = 604800L,
                    MinerIncreaseInterval = EconomicContractsTestConstants.MinerIncreaseInterval
                });
            CheckResult(result.TransactionResult);
        }
        {
            var result = await AEDPoSContractStub.FirstRound.SendAsync(
                new MinerList
                {
                    Pubkeys = { InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey)) }
                }.GenerateFirstRoundOfNewTerm(EconomicContractsTestConstants.MiningInterval, StartTimestamp));
            CheckResult(result.TransactionResult);
        }
    }

    protected async Task InitializeTransactionFeeChargingContract()
    {
        await ExecuteProposalForParliamentTransaction(TokenContractAddress,
            nameof(TokenContractStub.AddAddressToCreateTokenWhiteList), TransactionFeeChargingContractAddress);
        var result = await TransactionFeeChargingContractStub.InitializeTransactionFeeChargingContract.SendAsync(
            new InitializeTransactionFeeChargingContractInput
            {
                Symbol = EconomicContractsTestConstants.TransactionFeeChargingContractTokenSymbol
            });
        CheckResult(result.TransactionResult);

        {
            var approveResult = await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = EconomicContractsTestConstants.NativeTokenSymbol,
                Spender = TokenConverterContractAddress,
                Amount = 1000_000_00000000L
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        foreach (var coreDataCenterKeyPair in CoreDataCenterKeyPairs)
        {
            var tokenStub = GetTokenContractTester(coreDataCenterKeyPair);
            var approveResult = await tokenStub.Approve.SendAsync(new ApproveInput
            {
                Symbol = EconomicContractsTestConstants.TransactionFeeChargingContractTokenSymbol,
                Spender = TreasuryContractAddress,
                Amount = 1000_000_00000000L
            });
            approveResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }

    protected async Task InitializeTokenConverter()
    {
        await SetConnectors();
    }

    protected async Task InitializeParliamentContract()
    {
        var initializeResult = await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
        {
            PrivilegedProposer = BootMinerAddress,
            ProposerAuthorityRequired = true
        });
        CheckResult(initializeResult.TransactionResult);
    }

    protected async Task SetConnectors()
    {
        {
            await SetConnector(new PairConnectorParam
            {
                ResourceConnectorSymbol = EconomicContractsTestConstants.TransactionFeeChargingContractTokenSymbol,
                ResourceWeight = "0.05",
                NativeWeight = "0.05",
                NativeVirtualBalance = 1_000_000_00000000
            });
        }
    }

    private async Task ApproveByParliamentMembers(Hash proposalId)
    {
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            var approveResult = await tester.Approve.SendAsync(proposalId);
            CheckResult(approveResult.TransactionResult);
        }
    }

    private async Task<Hash> CreateAndApproveProposalForParliament(Address contract,
        string method, IMessage input, Address parliamentOrganization = null)
    {
        if (parliamentOrganization == null)
            parliamentOrganization =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposal = new CreateProposalInput
        {
            OrganizationAddress = parliamentOrganization,
            ContractMethodName = method,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
            Params = input.ToByteString(),
            ToAddress = contract
        };
        var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
        CheckResult(createResult.TransactionResult);
        var proposalHash = createResult.Output;
        await ApproveByParliamentMembers(proposalHash);
        return proposalHash;
    }


    protected async Task ExecuteProposalForParliamentTransaction(Address contract,
        string method, IMessage input, Address parliamentOrganization = null)
    {
        if (parliamentOrganization == null)
            parliamentOrganization =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalHash =
            await CreateAndApproveProposalForParliament(contract, method, input,
                parliamentOrganization);
        var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalHash);
        CheckResult(releaseResult.TransactionResult);
    }

    protected async Task<TransactionResult> ExecuteProposalForParliamentTransactionWithException(Address from,
        Address contract,
        string method, IMessage input, Address parliamentOrganization = null)
    {
        if (parliamentOrganization == null)
            parliamentOrganization =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalHash =
            await CreateAndApproveProposalForParliament(contract, method, input,
                parliamentOrganization);
        var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalHash);
        return releaseResult.TransactionResult;
    }

    #endregion

    #region Other Methods

    private void CheckResult(TransactionResult result)
    {
        if (!string.IsNullOrEmpty(result.Error)) throw new Exception(result.Error);
    }

    private async Task SetConnector(PairConnectorParam connector)
    {
        var connectorManagerAddress =
            await TokenConverterContractStub.GetControllerForManageConnector.CallAsync(new Empty());
        var proposal = new CreateProposalInput
        {
            OrganizationAddress = connectorManagerAddress.OwnerAddress,
            ContractMethodName = nameof(TokenConverterContractStub.AddPairConnector),
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            Params = connector.ToByteString(),
            ToAddress = TokenConverterContractAddress
        };
        var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
        CheckResult(createResult.TransactionResult);

        var proposalHash = createResult.Output;
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            var approveResult = await tester.Approve.SendAsync(proposalHash);
            CheckResult(approveResult.TransactionResult);
        }

        var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalHash);
        CheckResult(releaseResult.TransactionResult);
    }

    #endregion
}