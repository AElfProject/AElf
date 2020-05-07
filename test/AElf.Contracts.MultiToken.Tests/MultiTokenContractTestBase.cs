using System;
using System.Collections.Generic;
using System.Linq;
using Acs2;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.Parliament;
using AElf.Contracts.Referendum;
using AElf.Contracts.TestBase;
using AElf.CrossChain;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.Contracts.Treasury;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.GovernmentSystem;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.Threading;
using SampleECKeyPairs = AElf.Contracts.TestKit.SampleECKeyPairs;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractTestBase : TestKit.ContractTestBase<MultiTokenContractTestAElfModule>
    {
        public byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        protected long AliceCoinTotalAmount => 1_000_000_000_0000000L;
        protected long BobCoinTotalAmount => 1_000_000_000_0000L;
        protected Address TokenContractAddress { get; set; }
        internal TokenContractImplContainer.TokenContractImplStub TokenContractStub;
        protected ECKeyPair DefaultKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultAddress => Address.FromPublicKey(DefaultKeyPair.PublicKey);
        protected ECKeyPair User1KeyPair { get; } = SampleECKeyPairs.KeyPairs[10];
        protected Address User1Address => Address.FromPublicKey(User1KeyPair.PublicKey);
        protected ECKeyPair User2KeyPair { get; } = SampleECKeyPairs.KeyPairs[11];
        protected ECKeyPair ManagerKeyPair { get; } = SampleECKeyPairs.KeyPairs[12];
        protected Address ManagerAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        protected Address User2Address => Address.FromPublicKey(User2KeyPair.PublicKey);
        protected const string DefaultSymbol = "ELF";
        public byte[] TreasuryContractCode => Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
        protected Address TreasuryContractAddress { get; set; }

        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub;
        public byte[] ProfitContractCode => Codes.Single(kv => kv.Key.Contains("Profit")).Value;
        protected Address ProfitContractAddress { get; set; }

        internal ProfitContractContainer.ProfitContractStub ProfitContractStub;
        public byte[] TokenConverterContractCode => Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;

        public byte[] ReferendumContractCode => Codes.Single(kv => kv.Key.Contains("Referendum")).Value;
        public byte[] ParliamentCode => Codes.Single(kv => kv.Key.Contains("Parliament")).Value;
        public byte[] ConsensusContractCode => Codes.Single(kv => kv.Key.Contains("Consensus.AEDPoS")).Value;
        public byte[] AssociationContractCode => Codes.Single(kv => kv.Key.Contains("Association")).Value;
        protected Address TokenConverterContractAddress { get; set; }
        protected Address ConsensusContractAddress { get; set; }

        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub;
        internal ReferendumContractContainer.ReferendumContractStub ReferendumContractStub;
        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub;
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub AEDPoSContractStub { get; set; }

        internal ParliamentContractContainer.ParliamentContractStub GetParliamentContractTester(
            ECKeyPair keyPair)
        {
            return GetTester<ParliamentContractContainer.ParliamentContractStub>(ParliamentContractAddress,
                keyPair);
        }

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub GetConsensusContractTester(ECKeyPair keyPair)
        {
            return GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(ConsensusContractAddress, keyPair);
        }

        protected static List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            SampleECKeyPairs.KeyPairs.Take(EconomicContractsTestConstants.InitialCoreDataCenterCount).ToList();
        protected Address ReferendumContractAddress { get; set; }

        internal ACS2BaseContainer.ACS2BaseStub Acs2BaseStub;

        protected Address BasicFunctionContractAddress { get; set; }

        protected Address OtherBasicFunctionContractAddress { get; set; }
        protected Address ParliamentContractAddress { get; set; }

        internal BasicFunctionContractContainer.BasicFunctionContractStub BasicFunctionContractStub { get; set; }

        internal BasicFunctionContractContainer.BasicFunctionContractStub OtherBasicFunctionContractStub { get; set; }
        protected byte[] BasicFunctionContractCode => Codes.Single(kv => kv.Key.EndsWith("BasicFunction")).Value;

        protected byte[] OtherBasicFunctionContractCode =>
            Codes.Single(kv => kv.Key.Contains("BasicFunctionWithParallel")).Value;

        protected Hash BasicFunctionContractName => HashHelper.ComputeFrom("AElf.TestContractNames.BasicFunction");
        protected Hash OtherBasicFunctionContractName => HashHelper.ComputeFrom("AElf.TestContractNames.OtherBasicFunction");

        protected readonly Address Address = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey);

        protected const string SymbolForTest = "ELF";

        protected const long Amount = 100;

        protected void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }

        protected async Task InitializeParliamentContract()
        {
            var initializeResult = await ParliamentContractStub.Initialize.SendAsync(new Parliament.InitializeInput()
            {
                PrivilegedProposer = DefaultAddress,
                ProposerAuthorityRequired = true
            });
            CheckResult(initializeResult.TransactionResult);
        }

        protected async Task InitializeAElfConsensus()
        {
            {
                var result = await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                    new InitialAElfConsensusContractInput
                    {
                        PeriodSeconds = 604800L,
                        MinerIncreaseInterval = 31536000
                    });
                CheckResult(result.TransactionResult);
            }
            {
                var result = await AEDPoSContractStub.FirstRound.SendAsync(
                    new MinerList
                    {
                        Pubkeys = {InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}
                    }.GenerateFirstRoundOfNewTerm(4000, TimestampHelper.GetUtcNow()));
                CheckResult(result.TransactionResult);
            }
        }
    }

    public class MultiTokenContractCrossChainTestBase : TestBase.ContractTestBase<MultiTokenContractCrossChainTestAElfModule>
    {
        protected Address BasicContractZeroAddress;
        protected Address CrossChainContractAddress;
        protected Address TokenContractAddress;
        protected Address ParliamentAddress;
        protected Address ConsensusAddress;
        protected Address ReferendumAddress;
        protected Address AssociationAddress;

        protected Address SideBasicContractZeroAddress;
        protected Address SideCrossChainContractAddress;
        protected Address SideTokenContractAddress;
        protected Address SideParliamentAddress;
        protected Address SideConsensusAddress;

        protected Address Side2BasicContractZeroAddress;
        protected Address Side2CrossChainContractAddress;
        protected Address Side2TokenContractAddress;
        protected Address Side2ParliamentAddress;
        protected Address Side2ConsensusAddress;

        protected long TotalSupply;
        protected long BalanceOfStarter;
        protected Timestamp BlockchainStartTimestamp => TimestampHelper.GetUtcNow();

        protected ContractTester<MultiTokenContractCrossChainTestAElfModule> MainChainTester;
        protected ContractTester<MultiTokenContractCrossChainTestAElfModule> SideChainTester;
        protected ContractTester<MultiTokenContractCrossChainTestAElfModule> SideChain2Tester;

        protected readonly List<string> ResourceTokenSymbolList;

        protected int MainChainId;
        
        public MultiTokenContractCrossChainTestBase()
        {
            MainChainId = ChainHelper.ConvertBase58ToChainId("AELF");
            MainChainTester =
                new ContractTester<MultiTokenContractCrossChainTestAElfModule>(MainChainId,
                    SampleECKeyPairs.KeyPairs[0]);
            AsyncHelper.RunSync(() =>
                MainChainTester.InitialChainAsyncWithAuthAsync(MainChainTester.GetDefaultContractTypes(
                    MainChainTester.GetCallOwnerAddress(), out TotalSupply,
                    out _,
                    out BalanceOfStarter)));
            BasicContractZeroAddress = MainChainTester.GetZeroContractAddress();
            CrossChainContractAddress =
                MainChainTester.GetContractAddress(CrossChainSmartContractAddressNameProvider.Name);
            TokenContractAddress = MainChainTester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            ParliamentAddress = MainChainTester.GetContractAddress(ParliamentSmartContractAddressNameProvider.Name);
            ConsensusAddress = MainChainTester.GetContractAddress(ConsensusSmartContractAddressNameProvider.Name);
            ReferendumAddress = MainChainTester.GetContractAddress(ReferendumSmartContractAddressNameProvider.Name);
            AssociationAddress = MainChainTester.GetContractAddress(AssociationSmartContractAddressNameProvider.Name);
            ResourceTokenSymbolList = GetRequiredService<IOptionsSnapshot<HostSmartContractBridgeContextOptions>>()
                .Value.ContextVariables["SymbolListToPayRental"].Split(",").ToList();
        }

        protected void StartSideChain(int chainId, long height, string symbol, bool registerParentChainTokenContractAddress)
        {
            SideChainTester =
                new ContractTester<MultiTokenContractCrossChainTestAElfModule>(chainId, SampleECKeyPairs.KeyPairs[0]);
            AsyncHelper.RunSync(() =>
                SideChainTester.InitialCustomizedChainAsync(chainId,
                    configureSmartContract: SideChainTester.GetSideChainSystemContract(
                        SideChainTester.GetCallOwnerAddress(), MainChainId, symbol, out TotalSupply,
                        SideChainTester.GetCallOwnerAddress(), height,
                        registerParentChainTokenContractAddress ? TokenContractAddress : null)));
            SideBasicContractZeroAddress = SideChainTester.GetZeroContractAddress();
            SideCrossChainContractAddress =
                SideChainTester.GetContractAddress(CrossChainSmartContractAddressNameProvider.Name);
            SideTokenContractAddress = SideChainTester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            SideParliamentAddress =
                SideChainTester.GetContractAddress(ParliamentSmartContractAddressNameProvider.Name);
            SideConsensusAddress = SideChainTester.GetContractAddress(ConsensusSmartContractAddressNameProvider.Name);
        }

        protected void StartSideChain2(int chainId, long height, string symbol)
        {
            SideChain2Tester =
                new ContractTester<MultiTokenContractCrossChainTestAElfModule>(chainId, SampleECKeyPairs.KeyPairs[0]);
            AsyncHelper.RunSync(() =>
                SideChain2Tester.InitialCustomizedChainAsync(chainId,
                    configureSmartContract: SideChain2Tester.GetSideChainSystemContract(
                        SideChain2Tester.GetCallOwnerAddress(), MainChainId, symbol, out TotalSupply,
                        SideChain2Tester.GetCallOwnerAddress(), height, TokenContractAddress)));
            Side2BasicContractZeroAddress = SideChain2Tester.GetZeroContractAddress();
            Side2CrossChainContractAddress =
                SideChain2Tester.GetContractAddress(CrossChainSmartContractAddressNameProvider.Name);
            Side2TokenContractAddress = SideChain2Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            Side2ParliamentAddress =
                SideChain2Tester.GetContractAddress(ParliamentSmartContractAddressNameProvider.Name);
            Side2ConsensusAddress = SideChain2Tester.GetContractAddress(ConsensusSmartContractAddressNameProvider.Name);
        }

        protected async Task<int> InitAndCreateSideChainAsync(string symbol, long parentChainHeightOfCreation = 0,
            int parentChainId = 0, long lockedTokenAmount = 10)
        {
            await InitializeCrossChainContractAsync(parentChainHeightOfCreation, parentChainId);
            await ApproveBalanceAsync(lockedTokenAmount);
            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, symbol);
            await ApproveWithMinersAsync(proposalId, ParliamentAddress, MainChainTester);

            var releaseTxResult =
                await MainChainTester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.ReleaseSideChainCreation),
                    new ReleaseSideChainCreationInput {ProposalId = proposalId});
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(releaseTxResult.Logs.First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var chainId = sideChainCreatedEvent.ChainId;

            return chainId;
        }

        protected async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
            ECKeyPair ecKeyPair, IMessage input, bool isMainChain)
        {
            if (!isMainChain)
            {
                return ecKeyPair == null
                    ? await SideChainTester.GenerateTransactionAsync(contractAddress, methodName, input)
                    : await SideChainTester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
            }

            return ecKeyPair == null
                ? await MainChainTester.GenerateTransactionAsync(contractAddress, methodName, input)
                : await MainChainTester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
        }

        internal async Task<CrossChainMerkleProofContext> GetBoundParentChainHeightAndMerklePathByHeight(long height)
        {
            var result = await SideChainTester.ExecuteContractWithMiningAsync(SideCrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub
                    .GetBoundParentChainHeightAndMerklePathByHeight), new Int64Value
                {
                    Value = height
                });

            var crossChainMerkleProofContext = CrossChainMerkleProofContext.Parser.ParseFrom(result.ReturnValue);
            return crossChainMerkleProofContext;
        }

        internal async Task<long> GetSideChainHeight(int chainId)
        {
            var result = await MainChainTester.CallContractMethodAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub
                    .GetSideChainHeight), new Int32Value
                {
                    Value = chainId
                });

            var height = Int64Value.Parser.ParseFrom(result);
            return height.Value;
        }

        internal async Task<long> GetParentChainHeight(
            ContractTester<MultiTokenContractCrossChainTestAElfModule> tester, Address sideCrossChainContract)
        {
            var result = await tester.CallContractMethodAsync(sideCrossChainContract,
                nameof(CrossChainContractContainer.CrossChainContractStub
                    .GetParentChainHeight), new Empty());

            var height = Int64Value.Parser.ParseFrom(result);
            return height.Value;
        }

        private SideChainCreationRequest CreateSideChainCreationRequest(long indexingPrice, long lockedTokenAmount,
            string symbol, params SideChainTokenInitialIssue[] sideChainTokenInitialIssueList)
        {
            var res = new SideChainCreationRequest
            {
                IndexingPrice = indexingPrice,
                LockedTokenAmount = lockedTokenAmount,
                SideChainTokenDecimals = 2,
                IsSideChainTokenBurnable = true,
                SideChainTokenTotalSupply = 1_000_000_000,
                SideChainTokenSymbol = symbol,
                SideChainTokenName = "TEST",
                SideChainTokenInitialIssueList = {sideChainTokenInitialIssueList},
                InitialResourceAmount = {ResourceTokenSymbolList.ToDictionary(resource => resource, resource => 1)}
            };
            return res;
        }

        private async Task<Hash> CreateSideChainProposalAsync(long indexingPrice, long lockedTokenAmount, string symbol)
        {
            var createProposalInput = CreateSideChainCreationRequest(indexingPrice, lockedTokenAmount, symbol,
                new SideChainTokenInitialIssue
                {
                    Address = MainChainTester.GetCallOwnerAddress(),
                    Amount = 100
                });
            var requestSideChainCreationResult =
                await MainChainTester.ExecuteContractWithMiningAsync(CrossChainContractAddress,
                    nameof(CrossChainContractContainer.CrossChainContractStub.RequestSideChainCreation),
                    createProposalInput);

            var proposalId = ProposalCreated.Parser.ParseFrom(requestSideChainCreationResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            return proposalId;
        }

        protected async Task ApproveWithMinersAsync(Hash proposalId, Address parliament,
            ContractTester<MultiTokenContractCrossChainTestAElfModule> tester)
        {
            var approveTransaction1 = await tester.GenerateTransactionAsync(parliament,
                nameof(ParliamentContractContainer.ParliamentContractStub.Approve),
                tester.InitialMinerList[1], proposalId);
            var approveTransaction2 = await tester.GenerateTransactionAsync(parliament,
                nameof(ParliamentContractContainer.ParliamentContractStub.Approve),
                tester.InitialMinerList[2], proposalId);
            var approveTransaction0 = await tester.GenerateTransactionAsync(parliament,
                nameof(ParliamentContractContainer.ParliamentContractStub.Approve),
                tester.InitialMinerList[0], proposalId);
            await tester.MineAsync(
                new List<Transaction> {approveTransaction0, approveTransaction1, approveTransaction2});
        }

        protected async Task<TransactionResult> ReleaseProposalAsync(Hash proposalId, Address parliamentAddress,
            ContractTester<MultiTokenContractCrossChainTestAElfModule> tester)
        {
            var transactionResult = await tester.ExecuteContractWithMiningAsync(parliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.Release), proposalId);
            return transactionResult;
        }

        protected async Task<Hash> CreateProposalAsync(
            ContractTester<MultiTokenContractCrossChainTestAElfModule> tester, Address parliamentAddress, string method,
            ByteString input,
            Address contractAddress)
        {
            var organizationAddress = Address.Parser.ParseFrom((await tester.ExecuteContractWithMiningAsync(
                    parliamentAddress,
                    nameof(ParliamentContractContainer.ParliamentContractStub.GetDefaultOrganizationAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await tester.ExecuteContractWithMiningAsync(parliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = method,
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = input,
                    ToAddress = contractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }

        protected async Task BootMinerChangeRoundAsync(
            ContractTester<MultiTokenContractCrossChainTestAElfModule> tester, Address consensusAddress,
            bool isMainChain, long nextRoundNumber = 2)
        {
            if (isMainChain)
            {
                var info = await tester.CallContractMethodAsync(consensusAddress,
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.GetCurrentRoundInformation),
                    new Empty());
                var currentRound = Round.Parser.ParseFrom(info);
                var expectedStartTime = TimestampHelper.GetUtcNow();
                currentRound.GenerateNextRoundInformation(expectedStartTime, BlockchainStartTimestamp,
                    out var nextRound);
                nextRound.RealTimeMinersInformation[tester.InitialMinerList[0].PublicKey.ToHex()]
                    .ExpectedMiningTime = expectedStartTime;

                var txResult = await tester.ExecuteContractWithMiningAsync(consensusAddress,
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                    nextRound);
                txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            if (!isMainChain)
            {
                var info = await tester.CallContractMethodAsync(consensusAddress,
                    nameof(AEDPoSContractContainer.AEDPoSContractStub.GetCurrentRoundInformation),
                    new Empty());
                var currentRound = Round.Parser.ParseFrom(info);
                var expectedStartTime = BlockchainStartTimestamp.ToDateTime()
                    .AddMilliseconds(
                        ((long) currentRound.TotalMilliseconds(4000)).Mul(
                            nextRoundNumber.Sub(1)));
                currentRound.GenerateNextRoundInformation(expectedStartTime.ToTimestamp(), BlockchainStartTimestamp,
                    out var nextRound);

                if (currentRound.RoundNumber >= 3)
                {
                    nextRound.RealTimeMinersInformation[tester.InitialMinerList[0].PublicKey.ToHex()]
                        .ExpectedMiningTime -= new Duration {Seconds = 2400};
                    var res = await tester.ExecuteContractWithMiningAsync(consensusAddress,
                        nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                        nextRound);
                    res.Status.ShouldBe(TransactionResultStatus.Mined);
                }
                else
                {
                    nextRound.RealTimeMinersInformation[tester.InitialMinerList[0].PublicKey.ToHex()]
                        .ExpectedMiningTime -= new Duration {Seconds = (currentRound.RoundNumber) * 20};

                    var txResult = await tester.ExecuteContractWithMiningAsync(consensusAddress,
                        nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                        nextRound);
                    txResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }
            }
        }

        private async Task ApproveBalanceAsync(long amount)
        {
            var callOwner = Address.FromPublicKey(MainChainTester.KeyPair.PublicKey);

            var approveResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.Approve), new ApproveInput
                {
                    Spender = CrossChainContractAddress,
                    Symbol = "ELF",
                    Amount = amount
                });
            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.GetAllowance),
                new GetAllowanceInput
                {
                    Symbol = "ELF",
                    Owner = callOwner,
                    Spender = CrossChainContractAddress
                });
        }

        private async Task InitializeCrossChainContractAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0)
        {
            var crossChainInitializationTransaction = await MainChainTester.GenerateTransactionAsync(
                CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Initialize), new CrossChain.InitializeInput
                {
                    ParentChainId = parentChainId == 0 ? ChainHelper.ConvertBase58ToChainId("AELF") : parentChainId,
                    CreationHeightOnParentChain = parentChainHeightOfCreation
                });
            await MainChainTester.MineAsync(new List<Transaction> {crossChainInitializationTransaction});
        }
    }
}