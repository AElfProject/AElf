using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS2;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.MultiToken;

public class MultiTokenContractTestBase : ContractTestBase<MultiTokenContractTestAElfModule>
{
    protected const string DefaultSymbol = "ELF";

    protected const long Amount = 100;
    internal ACS2BaseContainer.ACS2BaseStub Acs2BaseStub;

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub;

    internal TokenContractImplContainer.TokenContractImplStub TokenContractStub;
    internal TokenContractImplContainer.TokenContractImplStub TokenContractStubUser;
    internal TokenContractImplContainer.TokenContractImplStub TokenContractStubDelegate;
    internal TokenContractImplContainer.TokenContractImplStub TokenContractStubDelegate2;
    internal TokenContractImplContainer.TokenContractImplStub TokenContractStubDelegate3;


    internal TokenConverterContractImplContainer.TokenConverterContractImplStub TokenConverterContractStub;

    internal TreasuryContractImplContainer.TreasuryContractImplStub TreasuryContractStub;

    public MultiTokenContractTestBase()
    {
        TokenContractStub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, DefaultKeyPair);

        TokenContractStubUser =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);
        TokenContractStubDelegate = 
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User2KeyPair);
        TokenContractStubDelegate2 = 
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User3KeyPair);
        TokenContractStubDelegate3 = 
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User4KeyPair);
        Acs2BaseStub = GetTester<ACS2BaseContainer.ACS2BaseStub>(TokenContractAddress, DefaultKeyPair);

        TreasuryContractStub = GetTester<TreasuryContractImplContainer.TreasuryContractImplStub>(
            TreasuryContractAddress,
            DefaultKeyPair);

        TokenConverterContractStub = GetTester<TokenConverterContractImplContainer.TokenConverterContractImplStub>(
            TokenConverterContractAddress,
            DefaultKeyPair);

        BasicFunctionContractAddress = SystemContractAddresses[BasicFunctionContractName];
        BasicFunctionContractStub = GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(
            BasicFunctionContractAddress, DefaultKeyPair);

        OtherBasicFunctionContractAddress = SystemContractAddresses[OtherBasicFunctionContractName];
        OtherBasicFunctionContractStub = GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(
            OtherBasicFunctionContractAddress, DefaultKeyPair);

        ParliamentContractStub = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
            ParliamentContractAddress, DefaultKeyPair);
        AsyncHelper.RunSync(() => SubmitAndApproveProposalOfDefaultParliament(TokenContractAddress,
            nameof(TokenContractStub.Create), new CreateInput()
            {
                Symbol = "ELF",
                Decimals = 8,
                IsBurnable = true,
                TokenName = "ELF2",
                TotalSupply = 100_000_000_000_000_000L,
                Issuer = DefaultAddress,
                ExternalInfo = new ExternalInfo(),
                Owner = DefaultAddress
            }));

        AsyncHelper.RunSync(() => CreateSeedNftCollection(TokenContractStub));
    }

    protected long AliceCoinTotalAmount => 1_000_000_000_0000000L;
    protected long BobCoinTotalAmount => 1_000_000_000_0000L;
    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;
    protected ECKeyPair User1KeyPair => Accounts[10].KeyPair;
    protected Address User1Address => Accounts[10].Address;
    protected ECKeyPair User2KeyPair => Accounts[11].KeyPair;
    protected Address User2Address => Accounts[11].Address;
    protected ECKeyPair User3KeyPair => Accounts[12].KeyPair;
    protected Address User3Address => Accounts[12].Address;
    protected ECKeyPair User4KeyPair => Accounts[13].KeyPair;
    protected Address User4Address => Accounts[13].Address;

    protected int SeedNum = 0;
    protected string SeedNFTSymbolPre = "SEED-";

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();

    protected Hash BasicFunctionContractName => HashHelper.ComputeFrom("AElf.TestContractNames.BasicFunction");
    protected Address BasicFunctionContractAddress { get; set; }
    internal BasicFunctionContractContainer.BasicFunctionContractStub BasicFunctionContractStub { get; set; }

    protected Hash OtherBasicFunctionContractName =>
        HashHelper.ComputeFrom("AElf.TestContractNames.OtherBasicFunction");


    protected Address OtherBasicFunctionContractAddress { get; set; }
    internal BasicFunctionContractContainer.BasicFunctionContractStub OtherBasicFunctionContractStub { get; set; }

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    private async Task SubmitAndApproveProposalOfDefaultParliament(Address contractAddress, string methodName,
        IMessage message)
    {
        var defaultParliamentAddress =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalId = await CreateProposalAsync(TokenContractAddress,
            defaultParliamentAddress, methodName, message);
        await ApproveWithMinersAsync(proposalId);
        var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
    }

    private async Task<Hash> CreateProposalAsync(Address contractAddress, Address organizationAddress,
        string methodName, IMessage input)
    {
        var proposal = new CreateProposalInput
        {
            OrganizationAddress = organizationAddress,
            ContractMethodName = methodName,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
            Params = input.ToByteString(),
            ToAddress = contractAddress
        };

        var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
        var proposalId = createResult.Output;

        return proposalId;
    }

    private async Task ApproveWithMinersAsync(Hash proposalId)
    {
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            var approveResult = await tester.Approve.SendAsync(proposalId);
        }
    }

    internal async Task CreateSeedNftCollection(TokenContractImplContainer.TokenContractImplStub stub)
    {
        var input = new CreateInput
        {
            Symbol = SeedNFTSymbolPre + SeedNum,
            Decimals = 0,
            IsBurnable = true,
            TokenName = "seed Collection",
            TotalSupply = 1,
            Issuer = DefaultAddress,
            Owner = DefaultAddress,
            ExternalInfo = new ExternalInfo()
        };
        await stub.Create.SendAsync(input);
    }


    internal async Task<CreateInput> CreateSeedNftAsync(TokenContractImplContainer.TokenContractImplStub stub,
        CreateInput createInput)
    {
        var input = BuildSeedCreateInput(createInput);
        await stub.Create.SendAsync(input);
        await stub.Issue.SendAsync(new IssueInput
        {
            Symbol = input.Symbol,
            Amount = 1,
            Memo = "ddd",
            To = DefaultAddress
        });
        return input;
    }
    
    

    internal async Task<IExecutionResult<Empty>> CreateSeedNftWithExceptionAsync(
        TokenContractImplContainer.TokenContractImplStub stub,
        CreateInput createInput)
    {
        var input = BuildSeedCreateInput(createInput);
        return await stub.Create.SendWithExceptionAsync(input);
    }

    internal CreateInput BuildSeedCreateInput(CreateInput createInput)
    {
        Interlocked.Increment(ref SeedNum);
        var input = new CreateInput
        {
            Symbol = SeedNFTSymbolPre + SeedNum,
            Decimals = 0,
            IsBurnable = true,
            TokenName = "seed token" + SeedNum,
            TotalSupply = 1,
            Issuer = DefaultAddress,
            Owner = DefaultAddress,
            ExternalInfo = new ExternalInfo(),
            LockWhiteList = { TokenContractAddress }
        };
        input.ExternalInfo.Value["__seed_owned_symbol"] = createInput.Symbol;
        input.ExternalInfo.Value["__seed_exp_time"] = TimestampHelper.GetUtcNow().AddDays(1).Seconds.ToString();
        return input;
    }

    internal async Task<IExecutionResult<Empty>> CreateMutiTokenAsync(
        TokenContractImplContainer.TokenContractImplStub stub,
        CreateInput createInput)
    {
        await CreateSeedNftAsync(stub, createInput);
        return await stub.Create.SendAsync(createInput);
    }

    internal async Task<IExecutionResult<Empty>> CreateMutiTokenWithExceptionAsync(
        TokenContractImplContainer.TokenContractImplStub stub, CreateInput createInput)
    {
        await CreateSeedNftAsync(stub, createInput);
        return await stub.Create.SendWithExceptionAsync(createInput);
    }
}