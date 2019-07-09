using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.MethodCallThreshold;
using AElf.Contracts.TestContract.TransactionFeeCharging;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using ApproveInput = AElf.Contracts.MultiToken.Messages.ApproveInput;
using InitializeInput = AElf.Contracts.ParliamentAuth.InitializeInput;

namespace AElf.Contracts.Economic.TestBase
{
    public partial class EconomicContractsTestBase
    {
        #region Private Preperties

        private const int Category = KernelConstants.CodeCoverageRunnerCategory;

        #endregion

        #region Contract Address

        protected Dictionary<ProfitType, Hash> ProfitItemsIds { get; set; }

        private Address _zeroAddress;
        protected Address ContractZeroAddress => GetZeroContract();

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

        private Address _methodCallThresholdAddress;
        protected Address MethodCallThresholdContractAddress =>
            GetOrDeployContract(TestContracts.MethodCallThreshold, ref _methodCallThresholdAddress);

        private Address _economicAddress;
        protected Address EconomicContractAddress => GetOrDeployContract(Contracts.Economic, ref _economicAddress);

        private Address _parliamentAddress;
        protected Address ParliamentAuthContractAddress =>
            GetOrDeployContract(Contracts.ParliamentAuth, ref _parliamentAddress);

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

        internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub =>
            GetConsensusContractTester(BootMinerKeyPair);

        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub =>
            GetTreasuryContractTester(BootMinerKeyPair);

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub ParliamentAuthContractStub =>
            GetParliamentAuthContractTester(BootMinerKeyPair);

        internal TransactionFeeChargingContractContainer.TransactionFeeChargingContractStub
            TransactionFeeChargingContractStub => GetTransactionFeeChargingContractTester(BootMinerKeyPair);

        internal MethodCallThresholdContractContainer.MethodCallThresholdContractStub MethodCallThresholdContractStub =>
            GetMethodCallThresholdContractTester(BootMinerKeyPair);

        internal EconomicContractContainer.EconomicContractStub EconomicContractStub =>
            GetEconomicContractTester(BootMinerKeyPair);

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

        internal AEDPoSContractContainer.AEDPoSContractStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress, keyPair);
        }

        internal TreasuryContractContainer.TreasuryContractStub GetTreasuryContractTester(ECKeyPair keyPair)
        {
            return GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress, keyPair);
        }

        internal ParliamentAuthContractContainer.ParliamentAuthContractStub GetParliamentAuthContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentAuthContractContainer.ParliamentAuthContractStub>(ParliamentAuthContractAddress,
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
                case Contracts.ParliamentAuth:
                    hash = Hash.FromString("AElf.ContractsName.Parliament");
                    break;
                case Contracts.AEDPoS:
                    hash = Hash.FromString("AElf.ContractNames.Consensus");
                    break;
                case Contracts.MultiToken:
                    hash = Hash.FromString("AElf.ContractNames.Token");
                    break;
                case Contracts.TransactionFee:
                    hash = Hash.FromString("AElf.ContractNames.TransactionFeeCharging");
                    break;
                default:
                    hash = Hash.FromString($"AElf.ContractNames.{contract.ToString()}");
                    break;
            }

            var address = await DeploySystemSmartContract(Category, code, hash, BootMinerKeyPair);

            return address;
        }

        private async Task<Address> DeployContract(TestContracts contract)
        {
            var code = Codes.Single(kv => kv.Key.Contains(contract.ToString())).Value;
            var hash = Hash.FromString($"AElf.ContractNames.{contract.ToString()}");
            var address = await DeploySystemSmartContract(Category, code, hash, BootMinerKeyPair);

            return address;
        }

        protected void DeployAllContracts()
        {
            _ = TokenContractAddress;
            _ = VoteContractAddress;
            _ = ProfitContractAddress;
            _ = EconomicContractAddress;
            _ = ElectionContractAddress;
            _ = TreasuryContractAddress;
            _ = TransactionFeeChargingContractAddress;
            _ = ParliamentAuthContractAddress;
            _ = TokenConverterContractAddress;
            _ = ConsensusContractAddress;
            _ = MethodCallThresholdContractAddress;
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
                var profitIds = (await ProfitContractStub.GetCreatedSchemeIds.CallAsync(
                    new GetCreatedSchemeIdsInput()
                    {
                        Creator = TreasuryContractAddress
                    })).SchemeIds;
                ProfitItemsIds = new Dictionary<ProfitType, Hash>
                {
                    {ProfitType.Treasury, profitIds[0]},
                    {ProfitType.MinerReward, profitIds[1]},
                    {ProfitType.BackupSubsidy, profitIds[2]},
                    {ProfitType.CitizenWelfare, profitIds[3]},
                    {ProfitType.BasicMinerReward, profitIds[4]},
                    {ProfitType.VotesWeightReward, profitIds[5]},
                    {ProfitType.ReElectionReward, profitIds[6]},
                };
            }
        }

        protected async Task InitializeElection()
        {
            var minerList = InitialCoreDataCenterKeyPairs.Select(o => o.PublicKey.ToHex()).ToArray();
            var result = await ElectionContractStub.InitialElectionContract.SendAsync(new InitialElectionContractInput
            {
                MaximumLockTime = 1080 * 86400,
                MinimumLockTime = 90 * 86400,
                TimeEachTerm = EconomicContractsTestConstants.TimeEachTerm,
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
                    To = Address.FromPublicKey(BootMinerKeyPair.PublicKey),
                    Memo = "Used to transfer other testers"
                });
                CheckResult(issueResult.TransactionResult);
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
                    Amount = EconomicContractsTestConstants.LockTokenForElection,
                    To = Address.FromPublicKey(validationDataCenterKeyPair.PublicKey),
                    Memo = "Used to announce election."
                });
                CheckResult(result.TransactionResult);
            }

            foreach (var validationDataCenterCandidateKeyPair in ValidationDataCenterCandidateKeyPairs)
            {
                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = EconomicContractsTestConstants.LockTokenForElection,
                    To = Address.FromPublicKey(validationDataCenterCandidateKeyPair.PublicKey),
                    Memo = "Used to announce election."
                });
                CheckResult(result.TransactionResult);
            }

            foreach (var voterKeyPair in VoterKeyPairs)
            {
                var result = await EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = EconomicContractsTestConstants.LockTokenForElection,
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
                        TimeEachTerm = 604800L,
                        MinerIncreaseInterval = EconomicContractsTestConstants.MinerIncreaseInterval
                    });
                CheckResult(result.TransactionResult);
            }
            {
                var result = await AEDPoSContractStub.FirstRound.SendAsync(
                    new MinerList
                    {
                        Pubkeys = {InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(EconomicContractsTestConstants.MiningInterval, StartTimestamp));
                CheckResult(result.TransactionResult);
            }
        }

        protected async Task InitializeTransactionFeeChargingContract()
        {
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
            var initializeResult = await ParliamentAuthContractStub.Initialize.SendAsync(new InitializeInput
            {
                GenesisOwnerReleaseThreshold = 1
            });
            CheckResult(initializeResult.TransactionResult);
        }

        protected async Task SetConnectors()
        {
            {
                await SetConnector(new Connector
                {
                    Symbol = EconomicContractsTestConstants.TransactionFeeChargingContractTokenSymbol,
                    IsPurchaseEnabled = true,
                    Weight = "0.2",
                    IsVirtualBalanceEnabled = true
                });
            }
        }

        #endregion

        #region Other Methods

        private void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }

        private async Task SetConnector(Connector connector)
        {
            var connectorManagerAddress = await TokenConverterContractStub.GetManagerAddress.CallAsync(new Empty());
            var proposal = new CreateProposalInput
            {
                OrganizationAddress = connectorManagerAddress,
                ContractMethodName = nameof(TokenConverterContractStub.SetConnector),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = connector.ToByteString(),
                ToAddress = TokenConverterContractAddress
            };
            var createResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(proposal);
            CheckResult(createResult.TransactionResult);

            var proposalHash = Hash.FromMessage(proposal);
            var approveResult = await ParliamentAuthContractStub.Approve.SendAsync(new Acs3.ApproveInput
            {
                ProposalId = proposalHash,
            });
            CheckResult(approveResult.TransactionResult);
        }

        #endregion
    }
}