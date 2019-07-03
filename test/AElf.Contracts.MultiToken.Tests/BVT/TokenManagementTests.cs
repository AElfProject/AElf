using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

// ReSharper disable CheckNamespace
namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests : MultiTokenContractTestBase
    {
        /// <summary>
        /// Burnable & Transferable
        /// </summary>
        private TokenInfo AliceCoinTokenInfo { get; set; } = new TokenInfo
        {
            Symbol = "ALICE",
            TokenName = "For testing multi-token contract",
            TotalSupply = 1_000_000_000_00000000,
            Decimals = 8,
            IsBurnable = true,
            Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
            IsTransferDisabled = false,
            Supply = 0
        };

        /// <summary>
        /// Not Burnable & Transferable
        /// </summary>
        private TokenInfo BobCoinTokenInfo { get; set; } = new TokenInfo
        {
            Symbol = "BOB",
            TokenName = "For testing multi-token contract",
            TotalSupply = 1_000_000_000_0000,
            Decimals = 4,
            IsBurnable = false,
            Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
            IsTransferDisabled = false,
            Supply = 0
        };

        /// <summary>
        /// Not Burnable & Not Transferable
        /// </summary>
        private TokenInfo EanCoinTokenInfo { get; set; } = new TokenInfo
        {
            Symbol = "EAN",
            TokenName = "For testing multi-token contract",
            TotalSupply = 1_000_000_000,
            Decimals = 0,
            IsBurnable = true,
            Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
            IsTransferDisabled = true,
            Supply = 0
        };

        public MultiTokenContractTests()
        {
            var category = KernelConstants.CodeCoverageRunnerCategory;

            // TokenContract
            {
                var code = TokenContractCode;
                TokenContractAddress = AsyncHelper.RunSync(() => DeploySystemSmartContract(category, code,
                    TokenSmartContractAddressNameProvider.Name, DefaultKeyPair));
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultKeyPair);
            }
        }

        [Fact(DisplayName = "[MultiToken] Create token test.")]
        public async Task MultiTokenContract_Create()
        {
            // Check token information before creating.
            {
                var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {
                    Symbol = AliceCoinTokenInfo.Symbol
                });
                tokenInfo.ShouldBe(new TokenInfo());
            }

            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = AliceCoinTokenInfo.Symbol,
                TokenName = AliceCoinTokenInfo.TokenName,
                TotalSupply = AliceCoinTokenInfo.TotalSupply,
                Decimals = AliceCoinTokenInfo.Decimals,
                Issuer = AliceCoinTokenInfo.Issuer,
                IsBurnable = AliceCoinTokenInfo.IsBurnable,
                IsTransferDisabled = AliceCoinTokenInfo.IsTransferDisabled
            });

            // Check token information after creating.
            {
                var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {
                    Symbol = AliceCoinTokenInfo.Symbol
                });
                tokenInfo.ShouldBe(AliceCoinTokenInfo);
            }
        }

        [Fact(DisplayName = "[MultiToken] Create different tokens.")]
        public async Task MultiTokenContract_Create_NotSame()
        {
            await MultiTokenContract_Create();

            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = BobCoinTokenInfo.Symbol,
                TokenName = BobCoinTokenInfo.TokenName,
                TotalSupply = BobCoinTokenInfo.TotalSupply,
                Decimals = BobCoinTokenInfo.Decimals,
                Issuer = BobCoinTokenInfo.Issuer,
                IsBurnable = BobCoinTokenInfo.IsBurnable,
                IsTransferDisabled = BobCoinTokenInfo.IsTransferDisabled
            });

            // Check token information after creating.
            {
                var tokenInfo = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
                {
                    Symbol = BobCoinTokenInfo.Symbol
                });
                tokenInfo.ShouldNotBe(AliceCoinTokenInfo);
            }
        }
    }
}