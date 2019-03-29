using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Resource.FeeReceiver;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Token;
using Google.Protobuf.WellKnownTypes;
using Nito.AsyncEx;
using NSubstitute.Exceptions;
using Org.BouncyCastle.Security;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterContractTests : TokenConverterTestBase
    {
        private ECKeyPair FeeKeyPair;
        private ECKeyPair ManagerKeyPair;
        private ECKeyPair FoundationKeyPair;

        private Address BasicZeroContractAddress;
        private Address TokenContractAddress;
        private Address FeeReceiverContractAddress;
        private Address TokenConverterContractAddress;

        //init connector
        private Connector ELFConnector = new Connector
        {
            Symbol = "ELF",
            VirtualBalance = 100_0000,
            Weight = 100_0000,
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true
        };

        private Connector RamConnector = new Connector
        {
            Symbol = "RAM",
            VirtualBalance = 0,
            Weight = 100_0000,
            IsVirtualBalanceEnabled = false,
            IsPurchaseEnabled = true
        };

        public TokenConverterContractTests()
        {
            AsyncHelper.RunSync(() => Tester.InitialChainAndTokenAsync());
            BasicZeroContractAddress = Tester.GetZeroContractAddress();
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            FeeReceiverContractAddress =
                Tester.GetContractAddress(ResourceFeeReceiverSmartContractAddressNameProvider.Name);
            TokenConverterContractAddress =
                Tester.GetContractAddress(TokenConverterSmartContractAddressNameProvider.Name);

            FeeKeyPair = CryptoHelpers.GenerateKeyPair();
            FoundationKeyPair = CryptoHelpers.GenerateKeyPair();
            ManagerKeyPair = CryptoHelpers.GenerateKeyPair();
        }

        private async Task CreatRamToken()
        {
            var ceateResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.Create), new CreateInput()
                {
                    Symbol = "RAM",
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = Tester.GetCallOwnerAddress(),
                    TokenName = "Ram Resource",
                    TotalSupply = 100_0000
                });
            ceateResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task<TransactionResult> InitializeTokenConverterContract()
        {
            //init token converter
            var input = new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                FeeRateNumerator = 5,
                FeeRateDenominator = 5,
                Manager = Address.FromPublicKey(ManagerKeyPair.PublicKey),
                MaxWeight = 100_0000,
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverContractAddress,
                Connectors = {ELFConnector, RamConnector}
            };
            return await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Initialize), input);
        }

        #region Views Test

        [Fact]
        public async Task ViewTest()
        {
            await InitializeTokenConverterContract();
            //GetConnector
            var ramConnectorInfo = Connector.Parser.ParseFrom(await Tester.CallTokenConverterMethodAsync(
                nameof(TokenConverterContract.GetConnector),
                new TokenId
                {
                    Symbol = RamConnector.Symbol
                }));
            ramConnectorInfo.Weight.ShouldBe(RamConnector.Weight);
            ramConnectorInfo.VirtualBalance.ShouldBe(RamConnector.VirtualBalance);
            ramConnectorInfo.IsPurchaseEnabled.ShouldBe(RamConnector.IsPurchaseEnabled);
            ramConnectorInfo.IsVirtualBalanceEnabled.ShouldBe(RamConnector.IsVirtualBalanceEnabled);

            //GetFeeReceiverAddress
            var feeReceiverAddress = Address.Parser.ParseFrom(
                await Tester.CallTokenConverterMethodAsync(nameof(TokenConverterContract.GetFeeReceiverAddress),
                    new Empty()));
            feeReceiverAddress.ShouldBe(FeeReceiverContractAddress);

            //GetTokenContractAddress
//            var tokenContractAddress = await Tester.CallTokenConverterMethodAsync(nameof(TokenConverterContract.GetTokenContractAddress),
//                    new Empty());
//            tokenContractAddress.ShouldBe(TokenContractAddress.Value);
        }

        #endregion

        #region Action Test

        [Fact]
        public async Task Initialize_Failed()
        {
            //init token converter
            var input = new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                FeeRateNumerator = 5,
                FeeRateDenominator = 5,
                Manager = Address.FromPublicKey(ManagerKeyPair.PublicKey),
                MaxWeight = 1000_0000,
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverContractAddress,
                Connectors = {RamConnector}
            };

            //token address is null
            {
                input.TokenContractAddress = null;
                var result =
                    await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Initialize), input);
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Token contract address required.").ShouldBeTrue();
            }
            //fee address is null
            {
                input.TokenContractAddress = TokenContractAddress;
                input.FeeReceiverAddress = null;
                var result =
                    await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Initialize), input);
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Fee receiver address required.").ShouldBeTrue();
            }
            //Base token symbol is invalid.
            {
                input.FeeReceiverAddress = FeeReceiverContractAddress;
                input.BaseTokenSymbol = "elf1";
                var result =
                    await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Initialize), input);
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Base token symbol is invalid.").ShouldBeTrue();
            }
            //Invalid MaxWeight
            {
                input.BaseTokenSymbol = "ELF";
                input.MaxWeight = 0;
                var result =
                    await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Initialize), input);
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Invalid MaxWeight.").ShouldBeTrue();
            }
            //Invalid symbol
            {
                input.MaxWeight = 1000_0000;
                RamConnector.Symbol = "ram";
                var result =
                    await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Initialize), input);
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Invalid symbol.").ShouldBeTrue();
            }
            //Already initialized
            {
                RamConnector.Symbol = "RAM";
                await InitializeTokenConverterContract();
                var result =
                    await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Initialize), input);
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Already initialized.").ShouldBeTrue();
            }
        }

        [Fact(Skip = "Manager account can't set connector")]
        public async Task Set_Connector_Success()
        {
            await InitializeTokenConverterContract();

            var manager = Tester.CreateNewContractTester(ManagerKeyPair);
            var result = await manager.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.SetConnector),
                new Connector
                {
                    Symbol = "RAM",
                    VirtualBalance = 0,
                    IsPurchaseEnabled = false,
                    IsVirtualBalanceEnabled = false
                });
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var ramNewInfo = Connector.Parser.ParseFrom(await Tester.CallTokenConverterMethodAsync(
                nameof(TokenConverterContract.GetConnector),
                new TokenId
                {
                    Symbol = "RAM"
                }));
            ramNewInfo.IsPurchaseEnabled.ShouldBeFalse();
        }

        [Fact(Skip = "Manager account can't set connector")]
        public async Task NullConnectors_Set_Connector()
        {
            var input = new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                FeeRateNumerator = 5,
                FeeRateDenominator = 5,
                Manager = Address.FromPublicKey(ManagerKeyPair.PublicKey),
                MaxWeight = 1000_0000,
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverContractAddress,
                Connectors = { }
            };
            var connectorsInfo = Connector.Parser.ParseFrom(
                await Tester.CallTokenConverterMethodAsync(nameof(TokenConverterContract.GetConnector),
                    new TokenId {Symbol = "RAM"}));
            connectorsInfo.Symbol.ShouldBeEmpty();

            //add Connector
            var manager = Tester.CreateNewContractTester(ManagerKeyPair);
            await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Initialize), input);
            var result =
                await manager.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.SetConnector),
                    RamConnector);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var ramNewInfo = Connector.Parser.ParseFrom(
                await Tester.CallTokenConverterMethodAsync(nameof(TokenConverterContract.GetConnector),
                    new TokenId {Symbol = "RAM"}));
            ramNewInfo.Symbol.ShouldBe("RAM");
        }

        [Fact]
        public async Task Set_Connector_Failed()
        {
            await InitializeTokenConverterContract();
            var result = await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.SetConnector),
                new Connector
                {
                    Symbol = "RAM",
                    VirtualBalance = 0,
                    IsPurchaseEnabled = false,
                    IsVirtualBalanceEnabled = false
                });
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Only manager can perform this action.").ShouldBeTrue();
        }

        [Fact(Skip = "GetBalance got exception")]
        public async Task Buy_Success()
        {
            await CreatRamToken();
            await InitializeTokenConverterContract();
            //approve
            var approveTokenResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.Approve), new ApproveInput
                {
                    Spender = TokenConverterContractAddress,
                    Symbol = "ELF",
                    Amount = 2000L,
                });
            approveTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var approveFeeResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.Approve), new ApproveInput
                {
                    Spender = FeeReceiverContractAddress,
                    Symbol = "ELF",
                    Amount = 2000L,
                });
            approveFeeResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var buyResult = await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Buy),
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1L
                });

            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balanceOfRam = GetBalanceOutput.Parser.ParseFrom(await Tester.CallContractMethodAsync(
                TokenContractAddress, nameof(TokenContract.GetBalance), new GetBalanceInput
                {
                    Symbol = RamConnector.Symbol,
                    Owner = Tester.GetCallOwnerAddress()
                }));
            balanceOfRam.Balance.ShouldBe(1000L);
        }

        #endregion
    }
}