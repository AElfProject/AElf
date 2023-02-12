using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.NFT;

public class NFTContractTestBase : ContractTestBase<NFTContractTestAElfModule>
{
    protected const long Amount = 100;
    internal TokenContractImplContainer.TokenContractImplStub NFTBuyer2TokenContractStub;
    internal TokenContractImplContainer.TokenContractImplStub NFTBuyerTokenContractStub;

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub;

    internal TokenContractImplContainer.TokenContractImplStub TokenContractStub;
    internal TokenContractImplContainer.TokenContractImplStub UserTokenContractStub;

    public NFTContractTestBase()
    {
        TokenContractStub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, DefaultKeyPair);
        UserTokenContractStub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User1KeyPair);
        NFTBuyerTokenContractStub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User2KeyPair);
        NFTBuyer2TokenContractStub =
            GetTester<TokenContractImplContainer.TokenContractImplStub>(TokenContractAddress, User3KeyPair);

        NFTContractAddress = SystemContractAddresses[NFTContractName];

        NFTContractStub = GetTester<NFTContractContainer.NFTContractStub>(NFTContractAddress, DefaultKeyPair);
        MinterNFTContractStub = GetTester<NFTContractContainer.NFTContractStub>(NFTContractAddress, MinterKeyPair);

        ParliamentContractStub = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
            ParliamentContractAddress, DefaultKeyPair);

        AsyncHelper.RunSync(CreateNativeTokenAsync);
        AsyncHelper.RunSync(SetNFTContractAddress);
    }

    protected ECKeyPair DefaultKeyPair => Accounts[0].KeyPair;
    protected Address DefaultAddress => Accounts[0].Address;
    protected ECKeyPair MinterKeyPair => Accounts[1].KeyPair;
    protected Address MinterAddress => Accounts[1].Address;

    protected ECKeyPair User1KeyPair => Accounts[10].KeyPair;
    protected ECKeyPair User2KeyPair => Accounts[11].KeyPair;
    protected ECKeyPair User3KeyPair => Accounts[14].KeyPair;
    protected Address User1Address => Accounts[10].Address;
    protected Address User2Address => Accounts[11].Address;
    protected Address User3Address => Accounts[14].Address;

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();

    protected Hash NFTContractName => HashHelper.ComputeFrom("AElf.ContractNames.NFT");
    protected Address NFTContractAddress { get; set; }
    internal NFTContractContainer.NFTContractStub NFTContractStub { get; set; }
    internal NFTContractContainer.NFTContractStub MinterNFTContractStub { get; set; }

    private TokenInfo NativeTokenInfo => new()
    {
        Symbol = "ELF",
        TokenName = "Native token",
        TotalSupply = 10_00000000_00000000,
        Decimals = 8,
        IsBurnable = true,
        Issuer = DefaultAddress
    };

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    private async Task CreateNativeTokenAsync()
    {
        await TokenContractStub.Create.SendAsync(new MultiToken.CreateInput
        {
            Symbol = NativeTokenInfo.Symbol,
            TokenName = NativeTokenInfo.TokenName,
            TotalSupply = NativeTokenInfo.TotalSupply,
            Decimals = NativeTokenInfo.Decimals,
            Issuer = NativeTokenInfo.Issuer,
            IsBurnable = NativeTokenInfo.IsBurnable
        });
    }

    private async Task SetNFTContractAddress()
    {
        var defaultParliament = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposalId = await CreateProposalAsync(TokenContractAddress,
            defaultParliament, nameof(TokenContractStub.AddAddressToCreateTokenWhiteList),
            NFTContractAddress);
        await ApproveWithMinersAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);
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
            await tester.Approve.SendAsync(proposalId);
        }
    }
}