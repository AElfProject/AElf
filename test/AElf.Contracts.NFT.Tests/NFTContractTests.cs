using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.NFT
{
    public partial class NFTContractTests : NFTContractTestBase
    {
        private const string BaseUri = "ipfs://aelf/";

        [Fact]
        public async Task<string> CreateTest()
        {
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = 1_00000000_00000000,
                To = DefaultAddress,
            });
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = 1_00000000_00000000,
                To = MinterAddress,
            });

            var executionResult = await NFTContractStub.Create.SendAsync(new CreateInput
            {
                BaseUri = BaseUri,
                Creator = DefaultAddress,
                IsBurnable = true,
                Metadata = new Metadata
                {
                    Value =
                    {
                        {"Description", "Stands for the human race."}
                    }
                },
                NftType = NFTType.VirtualWorlds.ToString(),
                ProtocolName = "HUMAN",
                TotalSupply = 1_000_000_000 // One billion
            });
            var symbol = executionResult.Output.Value;

            symbol.Length.ShouldBe(11);

            var protocolInfo = await NFTContractStub.GetNFTProtocolInfo.CallAsync(new StringValue
            {
                Value = symbol
            });
            protocolInfo.Symbol.ShouldBe(symbol);
            protocolInfo.Metadata.Value.ShouldContainKey("Description");
            protocolInfo.Creator.ShouldBe(DefaultAddress);
            protocolInfo.NftType.ShouldBe(NFTType.VirtualWorlds.ToString());
            protocolInfo.TotalSupply.ShouldBe(1_000_000_000);

            var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = symbol
            });

            tokenInfo.Decimals.ShouldBe(0);
            tokenInfo.Symbol.ShouldBe(symbol);
            tokenInfo.Issuer.ShouldBe(DefaultAddress);
            tokenInfo.ExternalInfo.Value["Description"].ShouldBe("Stands for the human race.");
            tokenInfo.ExternalInfo.Value["aelf_nft_type"].ShouldBe("VirtualWorlds");
            tokenInfo.ExternalInfo.Value["aelf_nft_base_uri"].ShouldBe(BaseUri);

            return symbol;
        }

        [Fact]
        public async Task<(string, Hash)> MintTest()
        {
            var symbol = await CreateTest();
            await AddMinterAsync(symbol);

            var tokenHash = (await MinterNFTContractStub.Mint.SendAsync(new MintInput
            {
                Symbol = symbol,
                Alias = "could be anything",
                Metadata = new Metadata
                {
                    Value =
                    {
                        {"Special Property", "A Value"}
                    }
                },
                Owner = DefaultAddress,
                Uri = $"{BaseUri}foo"
            })).Output;

            {
                var nftInfo = await NFTContractStub.GetNFTInfoByTokenHash.CallAsync(tokenHash);
                nftInfo.Creator.ShouldBe(DefaultAddress);
                nftInfo.Minters.ShouldContain(MinterAddress);
            }

            {
                var nftInfo = await NFTContractStub.GetNFTInfo.CallAsync(new GetNFTInfoInput
                {
                    Symbol = symbol,
                    TokenId = 1
                });
                nftInfo.Creator.ShouldBe(DefaultAddress);
                nftInfo.Minters.ShouldContain(MinterAddress);
            }

            {
                var protocolInfo = await NFTContractStub.GetNFTProtocolInfo.CallAsync(new StringValue
                {
                    Value = symbol
                });
                protocolInfo.Metadata.Value.ShouldNotContainKey("Special Property");
            }

            return (symbol, tokenHash);
        }

        [Fact(Skip = "Dup in TransferTest")]
        public async Task<string> MintMultiTokenTest()
        {
            var symbol = await CreateTest();
            await AddMinterAsync(symbol);

            await MinterNFTContractStub.Mint.SendAsync(new MintInput
            {
                Symbol = symbol,
                Alias = "could be anything",
                Metadata = new Metadata
                {
                    Value =
                    {
                        {"Max Health Points", "0"},
                        {"Max Mana Points", "0"},
                        {"Skill Points", "0"},
                        {"Level", "0"},
                        {"Experience", "0"}
                    }
                },
                Quantity = 100,
                Uri = $"{BaseUri}foo"
            });

            return symbol;
        }

        [Fact]
        public async Task<string> TransferTest()
        {
            var symbol = await MintMultiTokenTest();
            await MinterNFTContractStub.Transfer.SendAsync(new TransferInput
            {
                To = User1Address,
                Symbol = symbol,
                TokenId = 1,
                Amount = 10
            });

            {
                var balance = (await MinterNFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = User1Address,
                    Symbol = symbol,
                    TokenId = 1
                })).Balance;
                balance.ShouldBe(10);
            }

            {
                var balance = (await MinterNFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = MinterAddress,
                    Symbol = symbol,
                    TokenId = 1
                })).Balance;
                balance.ShouldBe(90);
            }

            return symbol;
        }

        [Fact]
        public async Task ApproveTest()
        {
            var symbol = await TransferTest();

            await MinterNFTContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = DefaultAddress,
                Symbol = symbol,
                TokenId = 1,
                Amount = 10
            });

            {
                var allowance = (await NFTContractStub.GetAllowance.CallAsync(new GetAllowanceInput
                {
                    Owner = MinterAddress,
                    Spender = DefaultAddress,
                    Symbol = symbol,
                    TokenId = 1
                })).Allowance;
                allowance.ShouldBe(10);
            }

            await NFTContractStub.TransferFrom.SendAsync(new TransferFromInput
            {
                To = User1Address,
                Symbol = symbol,
                TokenId = 1,
                Amount = 9,
                From = MinterAddress
            });

            {
                var balance = (await MinterNFTContractStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = User1Address,
                    Symbol = symbol,
                    TokenId = 1
                })).Balance;
                balance.ShouldBe(19);
            }

            await NFTContractStub.Transfer.SendWithExceptionAsync(new TransferInput
            {
                To = User1Address,
                Symbol = symbol,
                TokenId = 1,
                Amount = 2
            });
        }

        [Fact]
        public async Task AssembleTest()
        {
            var (symbol, tokenHash) = await MintTest();

            await TokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Spender = NFTContractAddress,
                Symbol = "ELF",
                Amount = long.MaxValue
            });

            await NFTContractStub.Assemble.SendAsync(new AssembleInput
            {
                Symbol = symbol,
                AssembledNfts = new AssembledNfts
                {
                    Value = {[tokenHash.ToHex()] = 1}
                },
                AssembledFts = new AssembledFts
                {
                    Value = {["ELF"] = 100}
                },
                Metadata = new Metadata
                {
                    Value =
                    {
                        ["Advanced Property"] = "whatever"
                    }
                }
            });
        }

        private async Task AddMinterAsync(string symbol)
        {
            await NFTContractStub.AddMinters.SendAsync(new AddMintersInput
            {
                Symbol = symbol,
                MinterList = new MinterList
                {
                    Value = {MinterAddress}
                }
            });
        }
    }
}