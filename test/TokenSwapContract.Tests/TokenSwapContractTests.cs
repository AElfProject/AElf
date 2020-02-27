using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Tokenswap;
using Xunit;

namespace TokenSwapContract.Tests
{
    public class TokenSwapContractTests : TokenSwapContractTestBase
    {
        public TokenSwapContractTests()
        {
            InitializePatchedContracts();
        }

        [Fact]
        public async Task TestAddSwapPair()
        {
            var tokenName = "ELF";
            var symbol = "ELF";
            var totalSupply = 100_000_000_000_000_000;
            await CreateAndIssueTokenAsync(tokenName, symbol, 8, totalSupply);
            var swapRatio = new SwapRatio
            {
                OriginShare = 10_000_000_000,
                TargetShare = 1
            };
            var originTokenSizeInByte = 32;
            var addSwapPairTx = await TokenSwapContractStub.AddSwapPair.SendAsync(new AddSwapPairInput
            {
                OriginTokenSizeInByte = originTokenSizeInByte,
                SwapRatio = swapRatio,
                TargetTokenSymbol = symbol
            });
            var pairId = addSwapPairTx.Output;
            var swapPair = await TokenSwapContractStub.GetSwapPair.CallAsync(pairId);
            swapPair.Controller.ShouldBe(DefaultSenderAddress);
            swapPair.CurrentRound.ShouldBeNull();
            swapPair.SwappedAmount.ShouldBe(0);
            swapPair.SwappedTimes.ShouldBe(0);
            swapPair.SwapRatio.ShouldBe(swapRatio);
            swapPair.TargetTokenSymbol.ShouldBe(symbol);
            swapPair.SwapPairId.ShouldBe(pairId);
            swapPair.OriginTokenSizeInByte.ShouldBe(originTokenSizeInByte);
        }

        [Fact]
        public async Task TestAddSwapRound()
        {
            var pairId = await AddSwapPairAsync();
            var addSwapRoundInput = new AddSwapRoundInput
            {
                MerkleTreeRoot =
                    HashHelper.HexStringToHash("0x3d4fb6567b200fa417a7fd3e38a2c0b43648cbdf42470c045d29fd82e3d50850"),
                SwapPairId = pairId
            };
            var blockTimeProvider = GetService<IBlockTimeProvider>();
            var utcNow = TimestampHelper.GetUtcNow();
            blockTimeProvider.SetBlockTime(utcNow);
            var addSwapRoundTx = await TokenSwapContractStub.AddSwapRound.SendAsync(addSwapRoundInput);
            var newTokenSwapRoundEvent = NewSwapRoundEvent.Parser.ParseFrom(addSwapRoundTx.TransactionResult.Logs
                .First(l => l.Name == nameof(NewSwapRoundEvent)).NonIndexed);
            newTokenSwapRoundEvent.MerkleTreeRoot.ShouldBe(addSwapRoundInput.MerkleTreeRoot);
            newTokenSwapRoundEvent.StartTime.ShouldBe(utcNow);
        }

        [Fact]
        public async Task TestTokenSwap1()
        {
            var pairId = await AddSwapPairAsync();
            var merkleTreeRoot =
                HashHelper.HexStringToHash("0x3d4fb6567b200fa417a7fd3e38a2c0b43648cbdf42470c045d29fd82e3d50850");
            await AddSwapRound(pairId, merkleTreeRoot);
            var receiverAddress =
                AddressHelper.Base58StringToAddress("SkMGjviAAs9bnYvv6cKcafbhf6tbRGQGK93WgKvZoCoS5amMK");
            var amountInStr = "75900000000000000000";
            var swapTokenInput = new SwapTokenInput
            {
                OriginAmount = amountInStr,
                ReceiverAddress = receiverAddress,
                UniqueId = HashHelper.HexStringToHash(
                    "96de8fc8c256fa1e1556d41af431cace7dca68707c78dd88c3acab8b17164c47"),
                MerklePath = new MerklePath
                {
                    MerklePathNodes =
                    {
                        new MerklePathNode
                        {
                            Hash = HashHelper.HexStringToHash(
                                "0x3450a26ef013f3e943ee35977601835abc463a0a905ce1c1d27342fb1cb9f79a"),
                            IsLeftChildNode = true
                        },
                        new MerklePathNode
                        {
                            Hash = HashHelper.HexStringToHash(
                                "0x7ede1519e67561f7017de9bed8a1ff30c45de1dc79f9bfbd369c75f9066540e8"),
                            IsLeftChildNode = false
                        },
                        new MerklePathNode
                        {
                            Hash = HashHelper.HexStringToHash(
                                "0xe7d02b7e62103a4c41585d4bd74d134c1f2bb63a7679a0dcda14adc892c32523"),
                            IsLeftChildNode = true
                        }
                    }
                },
                SwapPairId = pairId
            };
            var swapTokenTx = await TokenSwapContractStub.SwapToken.SendAsync(swapTokenInput);
            var tokenSwapEvent = TokenSwapEvent.Parser.ParseFrom(swapTokenTx.TransactionResult.Logs
                .First(l => l.Name == nameof(TokenSwapEvent)).NonIndexed);
            tokenSwapEvent.Address.ShouldBe(receiverAddress);
            tokenSwapEvent.Symbol.ShouldBe("ELF");
            tokenSwapEvent.Amount.ShouldBe(7590000000);

            var tokenTransferredEvent = swapTokenTx.TransactionResult.Logs
                .First(l => l.Name == nameof(Transferred));
            var nonIndexed = Transferred.Parser.ParseFrom(tokenTransferredEvent.NonIndexed);
            nonIndexed.Amount.ShouldBe(7590000000);

            Transferred.Parser.ParseFrom(tokenTransferredEvent.Indexed[1]).To.ShouldBe(receiverAddress);
            Transferred.Parser.ParseFrom(tokenTransferredEvent.Indexed[2]).Symbol.ShouldBe("ELF");
        }

        [Fact]
        public async Task TestTokenSwap2()
        {
            var pairId = await AddSwapPairAsync();
            var merkleTreeRoot =
                HashHelper.HexStringToHash("0x3d4fb6567b200fa417a7fd3e38a2c0b43648cbdf42470c045d29fd82e3d50850");
            await AddSwapRound(pairId, merkleTreeRoot);
            var receiverAddress =
                AddressHelper.Base58StringToAddress("2ADXLcyKMGGrRe9aGC7XMXECv8cxz3Tos1z6PJHSfyXguSaVb5");
            var amountInStr = "5500000000000000000";
            var swapTokenInput = new SwapTokenInput
            {
                OriginAmount = amountInStr,
                ReceiverAddress = receiverAddress,
                UniqueId = HashHelper.HexStringToHash(
                    "d9147961436944f43cd99d28b2bbddbf452ef872b30c8279e255e7daafc7f946"),
                MerklePath = new MerklePath
                {
                    MerklePathNodes =
                    {
                        new MerklePathNode
                        {
                            Hash = HashHelper.HexStringToHash(
                                "0xd3c078e54709a9329ad7136b9ebf482a9077fe7067ed46f2055a22343a115b5f"),
                            IsLeftChildNode = true
                        },
                        new MerklePathNode
                        {
                            Hash = HashHelper.HexStringToHash(
                                "0x3d247ec73f65bd010951ca7657139b480ec5a299bf5fc8b6e439518480bfd2c4"),
                            IsLeftChildNode = true
                        },
                        new MerklePathNode
                        {
                            Hash = HashHelper.HexStringToHash(
                                "0x8394a5d294470004842cc17699e1f9ee17878401a4a47b34af82b881e39a6b42"),
                            IsLeftChildNode = false
                        }
                    }
                },
                SwapPairId = pairId
            };
            var swapTokenTx = await TokenSwapContractStub.SwapToken.SendAsync(swapTokenInput);
            var tokenSwapEvent = TokenSwapEvent.Parser.ParseFrom(swapTokenTx.TransactionResult.Logs
                .First(l => l.Name == nameof(TokenSwapEvent)).NonIndexed);
            tokenSwapEvent.Address.ShouldBe(receiverAddress);
            tokenSwapEvent.Symbol.ShouldBe("ELF");
            tokenSwapEvent.Amount.ShouldBe(550000000);

            var tokenTransferredEvent = swapTokenTx.TransactionResult.Logs
                .First(l => l.Name == nameof(Transferred));
            var nonIndexed = Transferred.Parser.ParseFrom(tokenTransferredEvent.NonIndexed);
            nonIndexed.Amount.ShouldBe(550000000);

            Transferred.Parser.ParseFrom(tokenTransferredEvent.Indexed[1]).To.ShouldBe(receiverAddress);
            Transferred.Parser.ParseFrom(tokenTransferredEvent.Indexed[2]).Symbol.ShouldBe("ELF");
        }
    }
}