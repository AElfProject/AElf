using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS3;
using AElf.Standards.ACS7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Genesis;
using AElf.Contracts.Parliament;
using AElf.Contracts.Referendum;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractCrossChainTestBase : ContractTestBase<MultiTokenContractCrossChainTestAElfModule>
    {
        internal CrossChainContractImplContainer.CrossChainContractImplStub CrossChainContractStub;

        internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub;

        internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub;

        internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub;
        
        internal TokenContractImplContainer.TokenContractImplStub TokenContractStub;

        internal ReferendumContractImplContainer.ReferendumContractImplStub ReferendumContractStub;

        protected Address SideBasicContractZeroAddress;
        internal BasicContractZeroImplContainer.BasicContractZeroImplStub SideChainBasicContractZeroStub;

        protected Address SideCrossChainContractAddress;
        internal CrossChainContractImplContainer.CrossChainContractImplStub SideChainCrossChainContractStub;

        protected Address SideTokenContractAddress;
        internal TokenContractImplContainer.TokenContractImplStub SideChainTokenContractStub;

        protected Address SideParliamentAddress;
        internal ParliamentContractImplContainer.ParliamentContractImplStub SideChainParliamentContractStub;

        protected Address SideConsensusAddress;
        internal AEDPoSContractContainer.AEDPoSContractStub SideChainAEDPoSContractStub;

        protected Address Side2BasicContractZeroAddress;
        protected Address Side2CrossChainContractAddress;
        internal CrossChainContractImplContainer.CrossChainContractImplStub SideChain2CrossChainContractStub;
        protected Address Side2TokenContractAddress;
        internal TokenContractImplContainer.TokenContractImplStub SideChain2TokenContractStub;
        protected Address Side2ParliamentAddress;
        internal ParliamentContractImplContainer.ParliamentContractImplStub SideChain2ParliamentContractStub;
        protected Address Side2ConsensusAddress;
        internal AEDPoSContractContainer.AEDPoSContractStub SideChain2AEDPoSContractStub;

        protected long TotalSupply;
        protected long BalanceOfStarter;
        protected Timestamp BlockchainStartTimestamp => TimestampHelper.GetUtcNow();

        protected ContractTestKit<MultiTokenContractSideChainTestAElfModule> SideChainTestKit;
        protected ContractTestKit<MultiTokenContractSideChainTestAElfModule> SideChain2TestKit;

        protected readonly List<string> ResourceTokenSymbolList;

        protected int MainChainId;

        public MultiTokenContractCrossChainTestBase()
        {
            MainChainId = Application.ServiceProvider.GetRequiredService<IOptionsSnapshot<ChainOptions>>().Value
                .ChainId;
            BasicContractZeroStub =
                GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(BasicContractZeroAddress,
                    DefaultAccount.KeyPair);

            CrossChainContractStub =
                GetTester<CrossChainContractImplContainer.CrossChainContractImplStub>(CrossChainContractAddress,
                    DefaultAccount.KeyPair);

            TokenContractStub =
                GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress,
                    DefaultAccount.KeyPair);

            ParliamentContractStub =
                GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
                    DefaultAccount.KeyPair);

            AEDPoSContractStub = GetTester<AEDPoSContractContainer.AEDPoSContractStub>(ConsensusContractAddress);

            ReferendumContractStub =
                GetTester<ReferendumContractImplContainer.ReferendumContractImplStub>(ReferendumContractAddress,
                    DefaultAccount.KeyPair);

            ResourceTokenSymbolList = Application.ServiceProvider
                .GetRequiredService<IOptionsSnapshot<HostSmartContractBridgeContextOptions>>()
                .Value.ContextVariables["SymbolListToPayRental"].Split(",").ToList();
        }

        protected void StartSideChain(int chainId, long height, string symbol,
            bool registerParentChainTokenContractAddress)
        {
            SideChainTestKit = CreateContractTestKit<MultiTokenContractSideChainTestAElfModule>(
                new ChainInitializationDto
                {
                    ChainId = chainId,
                    Symbol = symbol,
                    ParentChainTokenContractAddress = TokenContractAddress,
                    ParentChainId = MainChainId,
                    CreationHeightOnParentChain = height,
                    RegisterParentChainTokenContractAddress = registerParentChainTokenContractAddress
                });
            SideBasicContractZeroAddress = SideChainTestKit.ContractZeroAddress;
            SideChainBasicContractZeroStub =
                SideChainTestKit.GetTester<BasicContractZeroImplContainer.BasicContractZeroImplStub>(
                    SideBasicContractZeroAddress);

            SideCrossChainContractAddress =
                SideChainTestKit.SystemContractAddresses[CrossChainSmartContractAddressNameProvider.Name];
            SideChainCrossChainContractStub =
                SideChainTestKit.GetTester<CrossChainContractImplContainer.CrossChainContractImplStub>(
                    SideCrossChainContractAddress);

            SideTokenContractAddress =
                SideChainTestKit.SystemContractAddresses[TokenSmartContractAddressNameProvider.Name];
            SideChainTokenContractStub =
                SideChainTestKit.GetTester<TokenContractImplContainer.TokenContractImplStub>(SideTokenContractAddress);
            SideParliamentAddress =
                SideChainTestKit.SystemContractAddresses[ParliamentSmartContractAddressNameProvider.Name];
            SideChainParliamentContractStub =
                SideChainTestKit.GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(SideParliamentAddress);

            SideConsensusAddress =
                SideChainTestKit.SystemContractAddresses[ConsensusSmartContractAddressNameProvider.Name];
            SideChainAEDPoSContractStub =
                SideChainTestKit.GetTester<AEDPoSContractContainer.AEDPoSContractStub>(SideConsensusAddress);
        }


        protected void StartSideChain2(int chainId, long height, string symbol)
        {
            SideChain2TestKit = CreateContractTestKit<MultiTokenContractSideChainTestAElfModule>(
                new ChainInitializationDto
                {
                    ChainId = chainId,
                    Symbol = symbol,
                    ParentChainTokenContractAddress = TokenContractAddress,
                    ParentChainId = MainChainId,
                    CreationHeightOnParentChain = height
                });
            Side2BasicContractZeroAddress = SideChain2TestKit.ContractZeroAddress;
            Side2CrossChainContractAddress =
                SideChain2TestKit.SystemContractAddresses[CrossChainSmartContractAddressNameProvider.Name];
            SideChain2CrossChainContractStub =
                SideChain2TestKit.GetTester<CrossChainContractImplContainer.CrossChainContractImplStub>(
                    Side2CrossChainContractAddress);
            Side2TokenContractAddress =
                SideChain2TestKit.SystemContractAddresses[TokenSmartContractAddressNameProvider.Name];
            SideChain2TokenContractStub = SideChain2TestKit
                .GetTester<TokenContractImplContainer.TokenContractImplStub>(Side2TokenContractAddress);
            Side2ParliamentAddress =
                SideChain2TestKit.SystemContractAddresses[ParliamentSmartContractAddressNameProvider.Name];
            SideChain2ParliamentContractStub =
                SideChain2TestKit.GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(Side2ParliamentAddress);
            Side2ConsensusAddress =
                SideChain2TestKit.SystemContractAddresses[ConsensusSmartContractAddressNameProvider.Name];
            SideChain2AEDPoSContractStub =
                SideChain2TestKit.GetTester<AEDPoSContractContainer.AEDPoSContractStub>(Side2ConsensusAddress);
        }

        protected async Task<int> InitAndCreateSideChainAsync(string symbol, long parentChainHeightOfCreation = 0,
            int parentChainId = 0, long lockedTokenAmount = 10)
        {
            await ApproveBalanceAsync(lockedTokenAmount);
            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, symbol);
            await ApproveWithMinersAsync(proposalId);

            var releaseResult = await CrossChainContractStub.ReleaseSideChainCreation.SendAsync(
                new ReleaseSideChainCreationInput
                    {ProposalId = proposalId});
            var sideChainCreatedEvent = SideChainCreatedEvent.Parser
                .ParseFrom(releaseResult.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(SideChainCreatedEvent)))
                    .NonIndexed);
            var chainId = sideChainCreatedEvent.ChainId;

            return chainId;
        }

        internal async Task<CrossChainMerkleProofContext> GetBoundParentChainHeightAndMerklePathByHeight(long height)
        {
            return await SideChainCrossChainContractStub.GetBoundParentChainHeightAndMerklePathByHeight.CallAsync(
                new Int64Value
                {
                    Value = height
                });
        }

        internal async Task<long> GetSideChainHeight(int chainId)
        {
            return (await CrossChainContractStub.GetSideChainHeight.CallAsync(new Int32Value
            {
                Value = chainId
            })).Value;
            ;
        }

        internal async Task<long> GetParentChainHeight(
            CrossChainContractImplContainer.CrossChainContractImplStub crossChainContractStub)
        {
            return (await crossChainContractStub.GetParentChainHeight.CallAsync(new Empty())).Value;
        }

        private SideChainCreationRequest CreateSideChainCreationRequest(long indexingPrice, long lockedTokenAmount,
            string symbol, params SideChainTokenInitialIssue[] sideChainTokenInitialIssueList)
        {
            var res = new SideChainCreationRequest
            {
                IndexingPrice = indexingPrice,
                LockedTokenAmount = lockedTokenAmount,
                SideChainTokenCreationRequest = new SideChainTokenCreationRequest{
                    SideChainTokenDecimals = 2,
                    SideChainTokenTotalSupply = 1_000_000_000,
                    SideChainTokenSymbol = symbol,
                    SideChainTokenName = "TEST"
                },
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
                    Address = DefaultAccount.Address,
                    Amount = 100
                });
            var requestSideChainCreationResult =
                await CrossChainContractStub.RequestSideChainCreation.SendAsync(createProposalInput);

            var proposalId = ProposalCreated.Parser.ParseFrom(requestSideChainCreationResult.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed).ProposalId;
            return proposalId;
        }

        protected async Task ApproveWithMinersAsync(Hash proposalId, bool isMainChain = true)
        {
            var transactionList = new List<Transaction>();

            if (isMainChain)
            {
                foreach (var account in SampleAccount.Accounts.Take(5))
                {
                    var parliamentContractStub =
                        GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
                            account.KeyPair);
                    transactionList.Add(parliamentContractStub.Approve.GetTransaction(proposalId));
                }

                await MineAsync(transactionList);
            }
            else
            {
                foreach (var account in SampleAccount.Accounts.Take(5))
                {
                    var parliamentContractStub =
                        SideChainTestKit.GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
                            SideParliamentAddress, account.KeyPair);
                    transactionList.Add(parliamentContractStub.Approve.GetTransaction(proposalId));
                }

                await SideChainTestKit.MineAsync(transactionList);
            }
        }

        protected async Task<TransactionResult> ReleaseProposalAsync(Hash proposalId)
        {
            var transaction = ParliamentContractStub.Release.GetTransaction(proposalId);
            return await ExecuteTransactionWithMiningAsync(transaction);
            ;
        }

        internal async Task<Hash> CreateProposalAsync(
            ParliamentContractImplContainer.ParliamentContractImplStub parliamentContractStub, string method,
            ByteString input, Address contractAddress)
        {
            var organizationAddress = await parliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposalResult = await parliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
            {
                ContractMethodName = method,
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                Params = input,
                ToAddress = contractAddress,
                OrganizationAddress = organizationAddress
            });
            var proposalId = proposalResult.Output;
            return proposalId;
        }

        internal async Task BootMinerChangeRoundAsync(AEDPoSContractContainer.AEDPoSContractStub aedPoSContractStub,
            bool isMainChain, long nextRoundNumber = 2)
        {
            if (isMainChain)
            {
                var currentRound = await aedPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                var expectedStartTime = TimestampHelper.GetUtcNow();
                currentRound.GenerateNextRoundInformation(expectedStartTime, BlockchainStartTimestamp,
                    out var nextRound);
                nextRound.RealTimeMinersInformation[DefaultAccount.KeyPair.PublicKey.ToHex()]
                    .ExpectedMiningTime = expectedStartTime;
                await aedPoSContractStub.NextRound.SendAsync(nextRound);
            }

            if (!isMainChain)
            {
                var currentRound = await aedPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
                var expectedStartTime = BlockchainStartTimestamp.ToDateTime()
                    .AddMilliseconds(
                        ((long) currentRound.TotalMilliseconds(4000)).Mul(
                            nextRoundNumber.Sub(1)));
                currentRound.GenerateNextRoundInformation(expectedStartTime.ToTimestamp(), BlockchainStartTimestamp,
                    out var nextRound);

                if (currentRound.RoundNumber >= 3)
                {
                    nextRound.RealTimeMinersInformation[DefaultAccount.KeyPair.PublicKey.ToHex()]
                        .ExpectedMiningTime -= new Duration {Seconds = 2400};
                    await aedPoSContractStub.NextRound.SendAsync(nextRound);
                }
                else
                {
                    nextRound.RealTimeMinersInformation[DefaultAccount.KeyPair.PublicKey.ToHex()]
                        .ExpectedMiningTime -= new Duration {Seconds = (currentRound.RoundNumber) * 20};

                    await aedPoSContractStub.NextRound.SendAsync(nextRound);
                }
            }
        }

        private async Task ApproveBalanceAsync(long amount)
        {
            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = CrossChainContractAddress,
                Symbol = "ELF",
                Amount = amount
            });
        }

        private async Task InitializeCrossChainContractAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0)
        {
            await CrossChainContractStub.Initialize.SendAsync(new CrossChain.InitializeInput
            {
                ParentChainId = parentChainId == 0 ? ChainHelper.ConvertBase58ToChainId("AELF") : parentChainId,
                CreationHeightOnParentChain = parentChainHeightOfCreation
            });
        }
    }
}