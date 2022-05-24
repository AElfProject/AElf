using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.NFTMinter;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.NFT
{
    public partial class NFTContractTests
    {
        [Fact]
        public async Task<string> CreateBadgeTest()
        {
            await InitializeNFTMinterContractAsync();

            var executionResult = await NFTContractStub.Create.SendAsync(new CreateInput
            {
                ProtocolName = "Top of OASIS",
                NftType = NFTType.Badges.ToString(),
                TotalSupply = 100_000,
                IsBurnable = false,
                IsTokenIdReuse = true,
                MinterList = new MinterList
                {
                    Value = { NFTMinterContractAddress}
                }
            });
            var symbol = executionResult.Output.Value;

            await CreatorNFTMinterContractStub.CreateBadge.SendAsync(new CreateBadgeInput
            {
                Symbol = symbol,
                TokenId = 233,
                Alias = "Participant",
                Metadata = new BadgeMetadata
                {
                    Value = {["Role"] = "Developer"}
                },
                Uri = "https://hackerlink.io/hackathon/13"
            });

            var nftProtocolInfo = await NFTContractStub.GetNFTProtocolInfo.CallAsync(new StringValue {Value = symbol});
            nftProtocolInfo.TotalSupply.ShouldBe(100_000);

            return symbol;
        }

        [Fact]
        public async Task<string> ConfigBadgeTest()
        {
            var symbol = await CreateBadgeTest();

            await CreatorNFTMinterContractStub.ConfigBadge.SendAsync(new ConfigBadgeInput
            {
                Symbol = symbol,
                TokenId = 233,
                Limit = int.MaxValue,
                IsPublic = false
            });

            var badgeInfo = await CreatorNFTMinterContractStub.GetBadgeInfo.CallAsync(new GetBadgeInfoInput
            {
                Symbol = symbol,
                TokenId = 233
            });
            badgeInfo.Limit.ShouldBe(int.MaxValue);
            badgeInfo.IsPublic.ShouldBe(false);

            return symbol;
        }

        [Fact]
        public async Task<string> ManageWhiteListTest()
        {
            var symbol = await ConfigBadgeTest();
            var executionResult = await CreatorNFTMinterContractStub.ManageMintingWhiteList.SendAsync(new ManageMintingWhiteListInput
            {
                Symbol = symbol,
                TokenId = 233,
                AddressList = new NFTMinter.AddressList {Value = {User1Address}}
            });
            var logEvent =
                MintingWhiteListChanged.Parser.ParseFrom(executionResult.TransactionResult.Logs.Single().NonIndexed);
            logEvent.AddressList.Value.ShouldContain(User1Address);

            {
                var isInWhiteList = await CreatorNFTMinterContractStub.IsInMintingWhiteList.CallAsync(
                    new IsInMintingWhiteListInput
                    {
                        Symbol = symbol,
                        TokenId = 233,
                        Address = User1Address
                    });
                isInWhiteList.Value.ShouldBeTrue();
            }
            {
                var isInWhiteList = await CreatorNFTMinterContractStub.IsInMintingWhiteList.CallAsync(
                    new IsInMintingWhiteListInput
                    {
                        Symbol = symbol,
                        TokenId = 233,
                        Address = User2Address
                    });
                isInWhiteList.Value.ShouldBeFalse();
            }
            return symbol;
        }

        [Fact]
        public async Task<string> MintBadgeTest()
        {
            var symbol = await ManageWhiteListTest();
            await UserNFTMinterContractStub.MintBadge.SendAsync(new MintBadgeInput
            {
                Symbol = symbol,
                TokenId = 233
            });
            var balance = await NFTContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = symbol,
                TokenId = 233,
                Owner = User1Address
            });
            balance.Balance.ShouldBe(1);

            return symbol;
        }

        private async Task InitializeNFTMinterContractAsync()
        {
            await CreatorNFTMinterContractStub.Initialize.SendAsync(new InitializeInput
            {
                NftContractAddress = NFTContractAddress,
                AdminAddress = DefaultAddress
            });
        }
    }
}