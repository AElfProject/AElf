using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.NFTMarket;
using AElf.Contracts.NFTMinter;
using AElf.Contracts.Parliament;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using AElf.Contracts.Whitelist;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Standards.ACS3;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;

namespace AElf.Contracts.NFT;

public class NFTContractTestBase : ContractTestBase<NFTContractTestAElfModule>
{
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
    protected Address User4Address => Accounts[15].Address;
    protected Address User5Address => Accounts[16].Address;

    protected Address User6Address => Accounts[17].Address;

    protected Address User7Address => Accounts[18].Address;
    protected ECKeyPair MarketServiceFeeReceiverKeyPair => Accounts[12].KeyPair;
    protected Address MarketServiceFeeReceiverAddress => Accounts[12].Address;

    protected const long Amount = 100;

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();

    internal TokenContractImplContainer.TokenContractImplStub TokenContractStub;
    internal TokenContractImplContainer.TokenContractImplStub UserTokenContractStub;
    internal TokenContractImplContainer.TokenContractImplStub NFTBuyerTokenContractStub;
    internal TokenContractImplContainer.TokenContractImplStub NFTBuyer2TokenContractStub;

    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub;

    protected Hash NFTContractName => HashHelper.ComputeFrom("AElf.ContractNames.NFT");
    protected Hash NFTMarketContractName => HashHelper.ComputeFrom("AElf.ContractNames.NFTMarket");
    protected Hash NFTMinterContractName => HashHelper.ComputeFrom("AElf.ContractNames.NFTMinter");
    protected Hash WhitelistContractName => HashHelper.ComputeFrom("AElf.ContractNames.Whitelist");
    protected Address NFTContractAddress { get; set; }
    protected Address NFTMarketContractAddress { get; set; }
    protected Address NFTMinterContractAddress { get; set; }
    protected Address WhitelistContractAddress { get; set; }
    internal NFTContractContainer.NFTContractStub NFTContractStub { get; set; }
    internal NFTContractContainer.NFTContractStub MinterNFTContractStub { get; set; }

    internal NFTMinterContractContainer.NFTMinterContractStub CreatorNFTMinterContractStub { get; set; }
    internal NFTMinterContractContainer.NFTMinterContractStub UserNFTMinterContractStub { get; set; }

    internal NFTMarketContractContainer.NFTMarketContractStub SellerNFTMarketContractStub { get; set; }
    internal NFTMarketContractContainer.NFTMarketContractStub BuyerNFTMarketContractStub { get; set; }
    internal NFTMarketContractContainer.NFTMarketContractStub Buyer2NFTMarketContractStub { get; set; }
    internal NFTMarketContractContainer.NFTMarketContractStub CreatorNFTMarketContractStub { get; set; }
    internal NFTMarketContractContainer.NFTMarketContractStub AdminNFTMarketContractStub { get; set; }
    internal WhitelistContractContainer.WhitelistContractStub WhitelistContractStub { get; set; }

    internal WhitelistContractContainer.WhitelistContractStub UserWhitelistContractStub { get; set; }

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
        NFTMarketContractAddress = SystemContractAddresses[NFTMarketContractName];
        NFTMinterContractAddress = SystemContractAddresses[NFTMinterContractName];
        WhitelistContractAddress = SystemContractAddresses[WhitelistContractName];

        NFTContractStub = GetTester<NFTContractContainer.NFTContractStub>(NFTContractAddress, DefaultKeyPair);
        MinterNFTContractStub = GetTester<NFTContractContainer.NFTContractStub>(NFTContractAddress, MinterKeyPair);

        CreatorNFTMinterContractStub =
            GetTester<NFTMinterContractContainer.NFTMinterContractStub>(NFTMinterContractAddress, DefaultKeyPair);
        UserNFTMinterContractStub =
            GetTester<NFTMinterContractContainer.NFTMinterContractStub>(NFTMinterContractAddress, User1KeyPair);

        SellerNFTMarketContractStub =
            GetTester<NFTMarketContractContainer.NFTMarketContractStub>(NFTMarketContractAddress, DefaultKeyPair);
        BuyerNFTMarketContractStub =
            GetTester<NFTMarketContractContainer.NFTMarketContractStub>(NFTMarketContractAddress, User2KeyPair);
        Buyer2NFTMarketContractStub =
            GetTester<NFTMarketContractContainer.NFTMarketContractStub>(NFTMarketContractAddress, User3KeyPair);
        CreatorNFTMarketContractStub =
            GetTester<NFTMarketContractContainer.NFTMarketContractStub>(NFTMarketContractAddress, DefaultKeyPair);
        AdminNFTMarketContractStub =
            GetTester<NFTMarketContractContainer.NFTMarketContractStub>(NFTMarketContractAddress, DefaultKeyPair);
        WhitelistContractStub =
            GetTester<WhitelistContractContainer.WhitelistContractStub>(WhitelistContractAddress, DefaultKeyPair);
        UserWhitelistContractStub =
            GetTester<WhitelistContractContainer.WhitelistContractStub>(WhitelistContractAddress, User2KeyPair);
        ParliamentContractStub = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(
            ParliamentContractAddress, DefaultKeyPair);

        AsyncHelper.RunSync(CreateNativeTokenAsync);
        AsyncHelper.RunSync(SetNFTContractAddress);
    }

    internal ParliamentContractImplContainer.ParliamentContractImplStub GetParliamentContractTester(
        ECKeyPair keyPair)
    {
        return GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
            keyPair);
    }

    private async Task CreateNativeTokenAsync()
    {
        await TokenContractStub.Create.SendAsync(new MultiToken.CreateInput()
        {
            Symbol = NativeTokenInfo.Symbol,
            TokenName = NativeTokenInfo.TokenName,
            TotalSupply = NativeTokenInfo.TotalSupply,
            Decimals = NativeTokenInfo.Decimals,
            Issuer = NativeTokenInfo.Issuer,
            IsBurnable = NativeTokenInfo.IsBurnable
        });
    }

    private TokenInfo NativeTokenInfo => new TokenInfo
    {
        Symbol = "ELF",
        TokenName = "Native token",
        TotalSupply = 10_00000000_00000000,
        Decimals = 8,
        IsBurnable = true,
        Issuer = DefaultAddress
    };

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