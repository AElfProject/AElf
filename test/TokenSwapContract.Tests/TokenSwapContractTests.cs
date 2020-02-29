using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Types;
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
            await CreatAndIssueDefaultToken();
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
                TargetTokenSymbol = DefaultSymbol,
                DepositAmount = TotalSupply
            });
            var pairId = addSwapPairTx.Output;
            var swapPair = await TokenSwapContractStub.GetSwapPair.CallAsync(pairId);
            swapPair.Controller.ShouldBe(DefaultSenderAddress);
            swapPair.CurrentRound.ShouldBeNull();
            swapPair.SwappedAmount.ShouldBe(0);
            swapPair.SwappedTimes.ShouldBe(0);
            swapPair.SwapRatio.ShouldBe(swapRatio);
            swapPair.TargetTokenSymbol.ShouldBe(DefaultSymbol);
            swapPair.PairId.ShouldBe(pairId);
            swapPair.OriginTokenSizeInByte.ShouldBe(originTokenSizeInByte);
        }

        [Fact]
        public async Task TestAddSwapRound()
        {
            await CreatAndIssueDefaultToken();
            var pairId = await AddSwapPairAsync();
            var addSwapRoundInput = new AddSwapRoundInput
            {
                MerkleTreeRoot =
                    HashHelper.HexStringToHash("0x3d4fb6567b200fa417a7fd3e38a2c0b43648cbdf42470c045d29fd82e3d50850"),
                PairId = pairId
            };
            var blockTimeProvider = GetService<IBlockTimeProvider>();
            var utcNow = TimestampHelper.GetUtcNow();
            blockTimeProvider.SetBlockTime(utcNow);
            var addSwapRoundTx = await TokenSwapContractStub.AddSwapRound.SendAsync(addSwapRoundInput);
            var newTokenSwapRoundEvent = SwapRoundUpdated.Parser.ParseFrom(addSwapRoundTx.TransactionResult.Logs
                .First(l => l.Name == nameof(SwapRoundUpdated)).NonIndexed);
            newTokenSwapRoundEvent.MerkleTreeRoot.ShouldBe(addSwapRoundInput.MerkleTreeRoot);
            newTokenSwapRoundEvent.StartTime.ShouldBe(utcNow);
        }

        [Fact]
        public async Task TestTokenSwap1()
        {
            await CreatAndIssueDefaultToken();
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
                PairId = pairId
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
            var expectedAmount = 7590000000;
            nonIndexed.Amount.ShouldBe(expectedAmount);

            Transferred.Parser.ParseFrom(tokenTransferredEvent.Indexed[1]).To.ShouldBe(receiverAddress);
            Transferred.Parser.ParseFrom(tokenTransferredEvent.Indexed[2]).Symbol.ShouldBe("ELF");

            var swapPair = await TokenSwapContractStub.GetSwapPair.CallAsync(pairId);
            swapPair.SwappedTimes.ShouldBe(1);
            swapPair.SwappedAmount.ShouldBe(expectedAmount);

            var swapRound = await TokenSwapContractStub.GetCurrentSwapRound.CallAsync(pairId);
            swapRound.SwappedAmount.ShouldBe(expectedAmount);
            swapPair.SwappedTimes.ShouldBe(1);

            // swap twice
            var transactionResult = (await TokenSwapContractStub.SwapToken.SendWithExceptionAsync(swapTokenInput))
                .TransactionResult;
            transactionResult.Error.ShouldContain("Already claimed.");
        }

        [Fact]
        public async Task TestTokenSwap2()
        {
            await CreatAndIssueDefaultToken();
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
                PairId = pairId
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
            
            // swap twice
            var transactionResult = (await TokenSwapContractStub.SwapToken.SendWithExceptionAsync(swapTokenInput))
                .TransactionResult;
            transactionResult.Error.ShouldContain("Already claimed.");
        }

        [Fact]
        public async Task TestTokenSwap_DepositNotEnough()
        {
            await CreatAndIssueDefaultToken();
            var depositAmount = 550000000 - 1;
            var pairId = await AddSwapPairAsync(DefaultSymbol, 32, null, depositAmount);
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
                PairId = pairId
            };
            var transactionResult = (await TokenSwapContractStub.SwapToken.SendWithExceptionAsync(swapTokenInput))
                .TransactionResult;
            transactionResult.Error.ShouldContain("Deposit not enough.");
        }


        [Fact]
        public async Task TestChangeSwapRatio()
        {
            await CreatAndIssueDefaultToken();
            var pairId = await AddSwapPairAsync();
            {
                var tx = await TokenSwapContractStub.ChangeSwapRatio.SendWithExceptionAsync(new ChainSwapRatioInput
                {
                    PairId = pairId,
                    SwapRatio = new SwapRatio
                    {
                        OriginShare = 1,
                        TargetShare = 0,
                    }
                });
                tx.TransactionResult.Error.ShouldContain("Invalid swap pair.");
            }

            {
                var tx = await TokenSwapContractStub.ChangeSwapRatio.SendWithExceptionAsync(new ChainSwapRatioInput
                {
                    PairId = pairId,
                    SwapRatio = new SwapRatio
                    {
                        OriginShare = 0,
                        TargetShare = 1,
                    }
                });
                tx.TransactionResult.Error.ShouldContain("Invalid swap pair.");
            }

            {
                var newStub = GetTokenSwapContractStub(NormalKeyPair);
                var tx = await newStub.ChangeSwapRatio.SendWithExceptionAsync(new ChainSwapRatioInput
                {
                    PairId = pairId,
                    SwapRatio = new SwapRatio
                    {
                        OriginShare = 1,
                        TargetShare = 1,
                    }
                });
                tx.TransactionResult.Error.ShouldContain("No permission.");
            }

            {
                await TokenSwapContractStub.ChangeSwapRatio.SendAsync(new ChainSwapRatioInput
                {
                    PairId = pairId,
                    SwapRatio = new SwapRatio
                    {
                        OriginShare = 1,
                        TargetShare = 1,
                    }
                });
            }
        }

        [Fact]
        public async Task TestSwapWithMultiTypes()
        {
            // int
            {
                var tokenName = "ELF";
                var symbol = "ELF";
                var totalSupply = 100_000_000_000_000_000;
                await CreateAndApproveTokenAsync(tokenName, symbol, 8, totalSupply, totalSupply);
                var tokenLocker = new TokenLocker(4);
                var addressList = SampleECKeyPairs.KeyPairs.Select(
                    keyPair => Address.FromPublicKey(keyPair.PublicKey)).ToList();

                var amount = int.MaxValue;
                foreach (var address in addressList)
                {
                    tokenLocker.Lock(address, amount, false);
                }

                tokenLocker.GenerateMerkleTree();

                var pairId = await AddSwapPairAsync(symbol, 4, new SwapRatio
                {
                    OriginShare = 1,
                    TargetShare = 1
                }, totalSupply, false);
                var merkleTreeRoot = tokenLocker.MerkleTreeRoot;
                await AddSwapRound(pairId, merkleTreeRoot);

                var amountInStr = int.MaxValue.ToString();
                for (int i = 0; i < addressList.Count; ++i)
                {
                    var address = addressList[i];
                    var swapTokenInput = new SwapTokenInput
                    {
                        MerklePath = tokenLocker.GetMerklePath(i),
                        OriginAmount = amountInStr,
                        ReceiverAddress = address,
                        PairId = pairId,
                        UniqueId = i.ToHash()
                    };

                    var transactionResult = (await TokenSwapContractStub.SwapToken.SendAsync(swapTokenInput))
                        .TransactionResult;
                    // swapTokenInput
                    var tokenSwapEvent = TokenSwapEvent.Parser.ParseFrom(transactionResult.Logs
                        .First(l => l.Name == nameof(TokenSwapEvent)).NonIndexed);
                    tokenSwapEvent.Address.ShouldBe(address);
                    tokenSwapEvent.Symbol.ShouldBe("ELF");
                    tokenSwapEvent.Amount.ShouldBe(amount);
                }
            }

            // long 
            {
                var tokenName = "ABC";
                var symbol = "ABC";
                var totalSupply = long.MaxValue;
                await CreateAndApproveTokenAsync(tokenName, symbol, 8, totalSupply, totalSupply);

                var tokenLocker = new TokenLocker(8);
                var addressList = SampleECKeyPairs.KeyPairs.Select(
                    keyPair => Address.FromPublicKey(keyPair.PublicKey)).ToList();

                var amount = long.MaxValue;
                foreach (var address in addressList)
                {
                    tokenLocker.Lock(address, amount, false);
                }

                tokenLocker.GenerateMerkleTree();

                var pairId = await AddSwapPairAsync(symbol, 8, new SwapRatio
                {
                    OriginShare = SampleECKeyPairs.KeyPairs.Count,
                    TargetShare = 1
                }, totalSupply, false);
                var merkleTreeRoot = tokenLocker.MerkleTreeRoot;
                await AddSwapRound(pairId, merkleTreeRoot);

                var amountInStr = long.MaxValue.ToString();
                for (int i = 0; i < addressList.Count; ++i)
                {
                    var address = addressList[i];
                    var swapTokenInput = new SwapTokenInput
                    {
                        MerklePath = tokenLocker.GetMerklePath(i),
                        OriginAmount = amountInStr,
                        ReceiverAddress = address,
                        PairId = pairId,
                        UniqueId = i.ToHash()
                    };

                    var transactionResult = (await TokenSwapContractStub.SwapToken.SendAsync(swapTokenInput))
                        .TransactionResult;
                    // swapTokenInput
                    var tokenSwapEvent = TokenSwapEvent.Parser.ParseFrom(transactionResult.Logs
                        .First(l => l.Name == nameof(TokenSwapEvent)).NonIndexed);
                    tokenSwapEvent.Address.ShouldBe(address);
                    tokenSwapEvent.Symbol.ShouldBe(symbol);
                    tokenSwapEvent.Amount.ShouldBe(amount / SampleECKeyPairs.KeyPairs.Count);
                }
            }

            // decimal
            {
                var tokenName = "XYZ";
                var symbol = "XYZ";
                var totalSupply = long.MaxValue;
                await CreateAndApproveTokenAsync(tokenName, symbol, 8, totalSupply, totalSupply);

                var bytesCount = 16;
                var tokenLocker = new TokenLocker(bytesCount);
                var addressList = SampleECKeyPairs.KeyPairs.Select(
                    keyPair => Address.FromPublicKey(keyPair.PublicKey)).ToList();

                var amount = decimal.MaxValue;
                foreach (var address in addressList)
                {
                    tokenLocker.Lock(address, amount, false);
                }

                tokenLocker.GenerateMerkleTree();

                var originShare = 10_000_000_000 * SampleECKeyPairs.KeyPairs.Count;
                var pairId = await AddSwapPairAsync(symbol, bytesCount, new SwapRatio
                {
                    OriginShare = originShare,
                    TargetShare = 1
                }, totalSupply, false);
                var merkleTreeRoot = tokenLocker.MerkleTreeRoot;
                await AddSwapRound(pairId, merkleTreeRoot);

                var amountInStr = decimal.MaxValue.ToString();
                for (int i = 0; i < addressList.Count; ++i)
                {
                    var address = addressList[i];
                    var swapTokenInput = new SwapTokenInput
                    {
                        MerklePath = tokenLocker.GetMerklePath(i),
                        OriginAmount = amountInStr,
                        ReceiverAddress = address,
                        PairId = pairId,
                        UniqueId = i.ToHash()
                    };

                    var transactionResult = (await TokenSwapContractStub.SwapToken.SendAsync(swapTokenInput))
                        .TransactionResult;
                    // swapTokenInput
                    var tokenSwapEvent = TokenSwapEvent.Parser.ParseFrom(transactionResult.Logs
                        .First(l => l.Name == nameof(TokenSwapEvent)).NonIndexed);
                    tokenSwapEvent.Address.ShouldBe(address);
                    tokenSwapEvent.Symbol.ShouldBe(symbol);
                    tokenSwapEvent.Amount.ShouldBe(decimal.ToInt64(amount / originShare));
                }
            }
        }
    }
}