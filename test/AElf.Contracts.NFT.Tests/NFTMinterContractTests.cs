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
        public async Task CreateBadgeTest()
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