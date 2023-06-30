using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.CrossChain;
using AElf.Contracts.Genesis;
using AElf.Contracts.Parliament;
using AElf.Contracts.Referendum;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CrossChain;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract;
using AElf.Kernel.Token;
using AElf.Standards.ACS3;
using AElf.Standards.ACS7;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using InitializeInput = AElf.Contracts.CrossChain.InitializeInput;

namespace AElf.Contracts.MultiToken;

public class MultiTokenContractCrossChainTestBase : ContractTestBase<MultiTokenContractCrossChainTestAElfModule>
{
    protected readonly List<string> ResourceTokenSymbolList;

    internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub;
    protected long BalanceOfStarter;

    internal BasicContractZeroImplContainer.BasicContractZeroImplStub BasicContractZeroStub;
    internal CrossChainContractImplContainer.CrossChainContractImplStub CrossChainContractStub;

    protected int MainChainId;

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub;

    internal ReferendumContractImplContainer.ReferendumContractImplStub ReferendumContractStub;

    protected Address Side2BasicContractZeroAddress;
    protected Address Side2ConsensusAddress;
    protected Address Side2CrossChainContractAddress;
    protected Address Side2ParliamentAddress;
    protected Address Side2TokenContractAddress;

    protected Address SideBasicContractZeroAddress;
    internal AEDPoSContractContainer.AEDPoSContractStub SideChain2AEDPoSContractStub;
    internal CrossChainContractImplContainer.CrossChainContractImplStub SideChain2CrossChainContractStub;
    internal ParliamentContractImplContainer.ParliamentContractImplStub SideChain2ParliamentContractStub;
    protected ContractTestKit<MultiTokenContractSideChainTestAElfModule> SideChain2TestKit;
    internal TokenContractImplContainer.TokenContractImplStub SideChain2TokenContractStub;
    internal AEDPoSContractContainer.AEDPoSContractStub SideChainAEDPoSContractStub;
    internal BasicContractZeroImplContainer.BasicContractZeroImplStub SideChainBasicContractZeroStub;
    internal CrossChainContractImplContainer.CrossChainContractImplStub SideChainCrossChainContractStub;
    internal ParliamentContractImplContainer.ParliamentContractImplStub SideChainParliamentContractStub;

    protected ContractTestKit<MultiTokenContractSideChainTestAElfModule> SideChainTestKit;
    internal TokenContractImplContainer.TokenContractImplStub SideChainTokenContractStub;
    protected readonly IBlockchainService BlockchainService;


    protected Address SideConsensusAddress;

    protected Address SideCrossChainContractAddress;

    protected Address SideParliamentAddress;

    protected Address SideTokenContractAddress;

    internal TokenContractImplContainer.TokenContractImplStub TokenContractStub;

    protected long TotalSupply;
    
    protected int SeedNum = 0;
    protected string SeedNFTSymbolPre = "SEED-";

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
        
        BlockchainService = Application.ServiceProvider.GetRequiredService<IBlockchainService>();
    }

    protected Timestamp BlockchainStartTimestamp => TimestampHelper.GetUtcNow();

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
            SideChainTestKit.GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
                SideParliamentAddress);

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
            SideChain2TestKit.GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
                Side2ParliamentAddress);
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
                { ProposalId = proposalId });
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
            SideChainTokenCreationRequest = new SideChainTokenCreationRequest
            {
                SideChainTokenDecimals = 2,
                SideChainTokenTotalSupply = 1_000_000_000,
                SideChainTokenSymbol = symbol,
                SideChainTokenName = "TEST"
            },
            SideChainTokenInitialIssueList = { sideChainTokenInitialIssueList },
            InitialResourceAmount = { ResourceTokenSymbolList.ToDictionary(resource => resource, resource => 1) }
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
            var randomNumber = await GenerateRandomProofAsync(aedPoSContractStub, DefaultAccount.KeyPair);
            var currentRound = await aedPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            var expectedStartTime = TimestampHelper.GetUtcNow();
            currentRound.GenerateNextRoundInformation(expectedStartTime, BlockchainStartTimestamp,
                ByteString.CopyFrom(randomNumber), out var nextRound);
            nextRound.RealTimeMinersInformation[DefaultAccount.KeyPair.PublicKey.ToHex()]
                .ExpectedMiningTime = expectedStartTime;
            await aedPoSContractStub.NextRound.SendAsync(nextRound);
        }

        if (!isMainChain)
        {
            var randomNumber = CryptoHelper.ECVrfProve(DefaultAccount.KeyPair, Hash.Empty.ToByteArray());
            var currentRound = await aedPoSContractStub.GetCurrentRoundInformation.CallAsync(new Empty());
            var expectedStartTime = BlockchainStartTimestamp.ToDateTime()
                .AddMilliseconds(
                    ((long)currentRound.TotalMilliseconds(4000)).Mul(
                        nextRoundNumber.Sub(1)));
            currentRound.GenerateNextRoundInformation(expectedStartTime.ToTimestamp(), BlockchainStartTimestamp,
                ByteString.CopyFrom(randomNumber), out var nextRound);

            if (currentRound.RoundNumber >= 3)
            {
                nextRound.RealTimeMinersInformation[DefaultAccount.KeyPair.PublicKey.ToHex()]
                    .ExpectedMiningTime -= new Duration { Seconds = 2400 };
                await aedPoSContractStub.NextRound.SendAsync(nextRound);
            }
            else
            {
                nextRound.RealTimeMinersInformation[DefaultAccount.KeyPair.PublicKey.ToHex()]
                    .ExpectedMiningTime -= new Duration { Seconds = currentRound.RoundNumber * 20 };

                await aedPoSContractStub.NextRound.SendAsync(nextRound);
            }
        }
    }

    private async Task<byte[]> GenerateRandomProofAsync(AEDPoSContractContainer.AEDPoSContractStub aedPoSContractStub,
        ECKeyPair keyPair)
    {
        var blockHeight = (await BlockchainService.GetChainAsync()).BestChainHeight;
        var previousRandomHash =
            blockHeight <= 1
                ? Hash.Empty
                : await aedPoSContractStub.GetRandomHash.CallAsync(new Int64Value
                    { Value = blockHeight });
        return CryptoHelper.ECVrfProve(keyPair, previousRandomHash.ToByteArray());
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
        await CrossChainContractStub.Initialize.SendAsync(new InitializeInput
        {
            ParentChainId = parentChainId == 0 ? ChainHelper.ConvertBase58ToChainId("AELF") : parentChainId,
            CreationHeightOnParentChain = parentChainHeightOfCreation
        });
    }
    
    internal async Task CreateSeedNftCollection(TokenContractImplContainer.TokenContractImplStub stub, Address address)
    {
        var input = new CreateInput
        {
            Symbol = SeedNFTSymbolPre + 0,
            Decimals = 0,
            IsBurnable = true,
            TokenName = "seed Collection",
            TotalSupply = 1,
            Issuer = address,
            ExternalInfo = new ExternalInfo()
        };
        var re= await stub.Create.SendAsync(input);
    }


    internal async Task<CreateInput> CreateSeedNftAsync(TokenContractImplContainer.TokenContractImplStub stub,
        CreateInput createInput,Address lockWhiteAddress)
    {
        var input = BuildSeedCreateInput(createInput,lockWhiteAddress);
        await stub.Create.SendAsync(input);
        await stub.Issue.SendAsync(new IssueInput
        {
            Symbol = input.Symbol,
            Amount = 1,
            Memo = "ddd",
            To = input.Issuer
        });
        return input;
    }
    
    internal CreateInput BuildSeedCreateInput(CreateInput createInput,Address lockWhiteAddress)
    {
        Interlocked.Increment(ref SeedNum);
        var input = new CreateInput
        {
            Symbol = SeedNFTSymbolPre + SeedNum,
            Decimals = 0,
            IsBurnable = true,
            TokenName = "seed token" + SeedNum,
            TotalSupply = 1,
            Issuer = createInput.Issuer,
            ExternalInfo = new ExternalInfo(),
            LockWhiteList = { lockWhiteAddress }
        };
        input.ExternalInfo.Value["__seed_owned_symbol"] = createInput.Symbol;
        input.ExternalInfo.Value["__seed_exp_time"] = TimestampHelper.GetUtcNow().AddDays(1).Seconds.ToString();
        return input;
    }

}