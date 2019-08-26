using System;
using System.Collections.Generic;
using System.Linq;
using Acs2;
using System.Threading.Tasks;
using Acs3;
using Acs7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Profit;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.ParliamentAuth;
using AElf.Contracts.TestBase;
using AElf.Contracts.TestKit;
using AElf.CrossChain;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.Contracts.Treasury;
using AElf.Contracts.TokenConverter;
using AElf.Kernel.Consensus;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using InitializeInput = AElf.Contracts.CrossChain.InitializeInput;
using SampleAddress = AElf.Contracts.TestKit.SampleAddress;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenContractTestBase : TestKit.ContractTestBase<MultiTokenContractTestAElfModule>
    {
        public byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        protected long AliceCoinTotalAmount => 1_000_000_000_0000000L;
        protected long BobCoinTotalAmout => 1_000_000_000_0000L;
        protected Address TokenContractAddress { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub;
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
        protected Address TokenConverterContractAddress { get; set; }

        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub;

        internal ACS2BaseContainer.ACS2BaseStub Acs2BaseStub;
        
        protected Address BasicFunctionContractAddress { get; set; }
        
        protected Address OtherBasicFunctionContractAddress { get; set; }
        
        internal BasicFunctionContractContainer.BasicFunctionContractStub BasicFunctionContractStub { get; set; }
        
        internal BasicFunctionContractContainer.BasicFunctionContractStub OtherBasicFunctionContractStub { get; set; }
        protected byte[] BasicFunctionContractCode => Codes.Single(kv => kv.Key.Contains("BasicFunction")).Value;
        protected Hash BasicFunctionContractName => Hash.FromString("AElf.TestContractNames.BasicFunction");
        protected Hash OtherBasicFunctionContractName => Hash.FromString("AElf.TestContractNames.OtherBasicFunction");
        
        protected readonly Address Address = SampleAddress.AddressList[0];
        
        protected const string SymbolForTest = "ELFTEST";
        
        protected const long Amount = 100;
        protected void CheckResult(TransactionResult result)
        {
            if (!string.IsNullOrEmpty(result.Error))
            {
                throw new Exception(result.Error);
            }
        }
    }

    public class
        MultiTokenContractCrossChainTestBase : TestBase.ContractTestBase<MultiTokenContractCrossChainTestAElfModule>
    {
        protected Address BasicContractZeroAddress;
        protected Address CrossChainContractAddress;
        protected Address TokenContractAddress;
        protected Address ParliamentAddress;
        protected Address ConsensusAddress;
        
        protected Address SideBasicContractZeroAddress;
        protected Address SideCrossChainContractAddress;
        protected Address SideTokenContractAddress;
        protected Address SideParliamentAddress;
        protected Address SideConsensusAddress;

        protected long TotalSupply;
        protected long BalanceOfStarter;
        protected bool IsPrivilegePreserved;
        protected Timestamp BlockchainStartTimestamp => TimestampHelper.GetUtcNow();
        
        protected ContractTester<MultiTokenContractCrossChainTestAElfModule> MainChainTester;
        protected ContractTester<MultiTokenContractCrossChainTestAElfModule> SideChainTester;

        protected int MainChainId;
        public MultiTokenContractCrossChainTestBase()
        {
            MainChainId = ChainHelper.ConvertBase58ToChainId("AELF");
            MainChainTester = new ContractTester<MultiTokenContractCrossChainTestAElfModule>(MainChainId,SampleECKeyPairs.KeyPairs[0]);
            AsyncHelper.RunSync(() =>
                MainChainTester.InitialChainAsyncWithAuthAsync(MainChainTester.GetDefaultContractTypes(MainChainTester.GetCallOwnerAddress(), out TotalSupply,
                    out _,
                    out BalanceOfStarter)));
            BasicContractZeroAddress = MainChainTester.GetZeroContractAddress();
            CrossChainContractAddress = MainChainTester.GetContractAddress(CrossChainSmartContractAddressNameProvider.Name);
            TokenContractAddress = MainChainTester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            ParliamentAddress = MainChainTester.GetContractAddress(ParliamentAuthSmartContractAddressNameProvider.Name);
            ConsensusAddress = MainChainTester.GetContractAddress(ConsensusSmartContractAddressNameProvider.Name);
        }

        protected void StartSideChain(int chainId)
        {
            SideChainTester = new ContractTester<MultiTokenContractCrossChainTestAElfModule>(chainId,SampleECKeyPairs.KeyPairs[0]);
            AsyncHelper.RunSync(() =>
                SideChainTester.InitialCustomizedChainAsync(chainId,configureSmartContract :SideChainTester.GetSideChainSystemContract(MainChainTester.GetCallOwnerAddress(),out TotalSupply,SideChainTester.GetCallOwnerAddress(),out IsPrivilegePreserved)));
            SideBasicContractZeroAddress = SideChainTester.GetZeroContractAddress();
            SideCrossChainContractAddress = SideChainTester.GetContractAddress(CrossChainSmartContractAddressNameProvider.Name);
            SideTokenContractAddress = SideChainTester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            SideParliamentAddress = SideChainTester.GetContractAddress(ParliamentAuthSmartContractAddressNameProvider.Name);
            SideConsensusAddress = SideChainTester.GetContractAddress(ConsensusSmartContractAddressNameProvider.Name);
        }

        protected async Task ApproveBalanceAsync(long amount)
        {
            var callOwner = Address.FromPublicKey(MainChainTester.KeyPair.PublicKey);

            var approveResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Approve), new ApproveInput
                {
                    Spender = CrossChainContractAddress,
                    Symbol = "ELF",
                    Amount = amount
                });
            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetAllowance),
                new GetAllowanceInput
                {
                    Symbol = "ELF",
                    Owner = callOwner,
                    Spender = CrossChainContractAddress
                });
        }

        protected async Task InitializeCrossChainContractAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0)
        {
            var crossChainInitializationTransaction = await MainChainTester.GenerateTransactionAsync(CrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Initialize), new InitializeInput
                {
                    ParentChainId = parentChainId == 0 ? ChainHelper.ConvertBase58ToChainId("AELF") : parentChainId,
                    CreationHeightOnParentChain = parentChainHeightOfCreation
                });
            await MainChainTester.MineAsync(new List<Transaction> {crossChainInitializationTransaction});
        }
        
        protected async Task InitializeCrossChainContractOnSideChainAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0)
        {
            var crossChainInitializationTransaction = await SideChainTester.GenerateTransactionAsync(SideCrossChainContractAddress,
                nameof(CrossChainContractContainer.CrossChainContractStub.Initialize), new InitializeInput
                {
                    ParentChainId = parentChainId == 0 ? ChainHelper.ConvertBase58ToChainId("AELF") : parentChainId,
                    CreationHeightOnParentChain = parentChainHeightOfCreation
                });
            await SideChainTester.MineAsync(new List<Transaction> {crossChainInitializationTransaction});
        }

        protected async Task<int> InitAndCreateSideChainAsync(long parentChainHeightOfCreation = 0,
            int parentChainId = 0, long lockedTokenAmount = 10)
        {
            await InitializeCrossChainContractAsync(parentChainHeightOfCreation, parentChainId);
            await ApproveBalanceAsync(lockedTokenAmount);
            var proposalId = await CreateSideChainProposalAsync(1, lockedTokenAmount, ByteString.CopyFromUtf8("Test"));
            await ApproveWithMinersOnMainChainAsync(proposalId);

            var transactionResult = await ReleaseProposalAsync(proposalId,ParliamentAddress,"Main");
            var chainId = CreationRequested.Parser.ParseFrom(transactionResult.Logs[0].NonIndexed).ChainId;

            return chainId;
        }

        protected async Task<Block> MineAsync(List<Transaction> txs, string chainType)
        {
            if (chainType.Equals("Side"))
                return await SideChainTester.MineAsync(txs);
            return await MainChainTester.MineAsync(txs);
        }

        protected async Task<TransactionResult> ExecuteContractWithMiningAsync(Address contractAddress,
            string methodName, IMessage input, string chainType)
        {
            if (chainType.Equals("Side"))
            {
                return await SideChainTester.ExecuteContractWithMiningAsync(contractAddress, methodName, input);
            }
            return await MainChainTester.ExecuteContractWithMiningAsync(contractAddress, methodName, input);
        }

        protected async Task<Transaction> GenerateTransactionAsync(Address contractAddress, string methodName,
            ECKeyPair ecKeyPair, IMessage input, string chainType)
        {
            if (chainType.Equals("Side"))
            {
                return ecKeyPair == null
                    ? await SideChainTester.GenerateTransactionAsync(contractAddress, methodName, input)
                    : await SideChainTester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
            }
            return ecKeyPair == null
                ? await MainChainTester.GenerateTransactionAsync(contractAddress, methodName, input)
                : await MainChainTester.GenerateTransactionAsync(contractAddress, methodName, ecKeyPair, input);
        }

        protected async Task<TransactionResult> GetTransactionResultAsync(Hash txId)
        {
            return await MainChainTester.GetTransactionResultAsync(txId);
        }

        protected async Task<ByteString> CallContractMethodAsync(Address contractAddress, string methodName,
            IMessage input)
        {
            return await MainChainTester.CallContractMethodAsync(contractAddress, methodName, input);
        }

        internal SideChainCreationRequest CreateSideChainCreationRequest(long indexingPrice, long lockedTokenAmount,
            ByteString contractCode, IEnumerable<ResourceTypeBalancePair> resourceTypeBalancePairs = null)
        {
            var res = new SideChainCreationRequest
            {
                ContractCode = contractCode,
                IndexingPrice = indexingPrice,
                LockedTokenAmount = lockedTokenAmount
            };
//            if (resourceTypeBalancePairs != null)
//                res.ResourceBalances.AddRange(resourceTypeBalancePairs.Select(x =>
//                    ResourceTypeBalancePair.Parser.ParseFrom(x.ToByteString())));
            return res;
        }

        private async Task<Hash> CreateSideChainProposalAsync(long indexingPrice, long lockedTokenAmount,
            ByteString contractCode, IEnumerable<ResourceTypeBalancePair> resourceTypeBalancePairs = null)
        {
            var createProposalInput = CreateSideChainCreationRequest(indexingPrice, lockedTokenAmount, contractCode);
            var organizationAddress = Address.Parser.ParseFrom((await MainChainTester.ExecuteContractWithMiningAsync(
                    ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = "CreateSideChain",
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = createProposalInput.ToByteString(),
                    ToAddress = CrossChainContractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }

        protected async Task ApproveWithMinersOnMainChainAsync(Hash proposalId)
        {
            var approveTransaction1 = await GenerateTransactionAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), MainChainTester.InitialMinerList[1],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                },"Main");
            var approveTransaction2 = await GenerateTransactionAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), MainChainTester.InitialMinerList[2],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                },"Main");
            await MineAsync(new List<Transaction> {approveTransaction1, approveTransaction2},"Main");
        }
        
        protected async Task ApproveWithMinersOnSideChainAsync(Hash proposalId)
        {
            var approveTransaction1 = await GenerateTransactionAsync(SideParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), SideChainTester.InitialMinerList[1],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                },"Side");
            var approveTransaction2 = await GenerateTransactionAsync(SideParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), SideChainTester.InitialMinerList[2],
                new Acs3.ApproveInput
                {
                    ProposalId = proposalId
                },"Side");
            await MineAsync(new List<Transaction> {approveTransaction1, approveTransaction2},"Side");
        }

        protected async Task<TransactionResult> ReleaseProposalAsync(Hash proposalId,Address parliamentAddress ,string chainType)
        {
            var transactionResult = await ExecuteContractWithMiningAsync(parliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Release),proposalId,chainType);
            return transactionResult;
        }
        
        protected async Task<Hash> CreateProposalAsyncOnMainChain(string method, ByteString input, Address contractAddress)
        {
            var organizationAddress = Address.Parser.ParseFrom((await MainChainTester.ExecuteContractWithMiningAsync(
                    ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
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
        
        protected async Task<Hash> CreateProposalAsyncOnSideChain(string method, ByteString input, Address contractAddress)
        {
            var organizationAddress = Address.Parser.ParseFrom((await SideChainTester.ExecuteContractWithMiningAsync(
                    SideParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty()))
                .ReturnValue);
            var proposal = await SideChainTester.ExecuteContractWithMiningAsync(SideParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
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

        protected async Task BootMinerChangeRoundAsync(string type, long nextRoundNumber = 2)
        {
            switch (type)
            {
                case "Side":
                {
                    var info = await SideChainTester.CallContractMethodAsync(SideConsensusAddress,
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
                        nextRound.RealTimeMinersInformation[SideChainTester.InitialMinerList[0].PublicKey.ToHex()]
                            .ExpectedMiningTime -= new Duration {Seconds = 2400};
                        var res = await SideChainTester.ExecuteContractWithMiningAsync(SideConsensusAddress,
                            nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                            nextRound);
                        res.Status.ShouldBe(TransactionResultStatus.Mined);
                        break;
                    }

                    nextRound.RealTimeMinersInformation[SideChainTester.InitialMinerList[0].PublicKey.ToHex()]
                        .ExpectedMiningTime -= new Duration {Seconds = (currentRound.RoundNumber) * 20};

                    var txResult = await SideChainTester.ExecuteContractWithMiningAsync(SideConsensusAddress,
                        nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                        nextRound);
                    txResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    break;
                }
                case "Main":
                {
                    var info = await MainChainTester.CallContractMethodAsync(ConsensusAddress,
                        nameof(AEDPoSContractContainer.AEDPoSContractStub.GetCurrentRoundInformation),
                        new Empty());
                    var currentRound = Round.Parser.ParseFrom(info);
                    var expectedStartTime = BlockchainStartTimestamp.ToDateTime()
                        .AddMilliseconds(
                            ((long) currentRound.TotalMilliseconds(4000)).Mul(
                                nextRoundNumber.Sub(1)));
                    currentRound.GenerateNextRoundInformation(expectedStartTime.ToTimestamp(), BlockchainStartTimestamp,
                        out var nextRound);
                    nextRound.RealTimeMinersInformation[MainChainTester.InitialMinerList[0].PublicKey.ToHex()]
                        .ExpectedMiningTime -= new Duration {Seconds = currentRound.RoundNumber * 20};

                    var txResult = await MainChainTester.ExecuteContractWithMiningAsync(ConsensusAddress,
                        nameof(AEDPoSContractContainer.AEDPoSContractStub.NextRound),
                        nextRound);
                    txResult.Status.ShouldBe(TransactionResultStatus.Mined);
                    break;
                }
                default:
                    return;
            }
        }
    }
}