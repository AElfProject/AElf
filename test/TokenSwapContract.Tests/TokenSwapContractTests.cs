using System.Linq;
using System.Threading.Tasks;
using AElf;
using AElf.Contracts.TestKit;
using AElf.Kernel;
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
        public async Task TestTokenSwap()
        {
            var pairId = await AddSwapPairAsync();
            var merkleTreeRoot =
                HashHelper.HexStringToHash("0x3d4fb6567b200fa417a7fd3e38a2c0b43648cbdf42470c045d29fd82e3d50850");
            await AddSwapRound(pairId, merkleTreeRoot);
            var swapTokenInput = new SwapTokenInput
            {
                OriginAmount = "75900000000000000000",
                
            };
        }
    }
}