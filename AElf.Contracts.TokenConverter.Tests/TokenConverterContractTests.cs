using System;
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
using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;
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
            IsVirtualBalanceEnabled = false
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
                FeeRateDenominator = 1000,
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
            var changeResult = await manager.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.SetConnector),
                new Connector
                {
                    Symbol = "RAM",
                    VirtualBalance = 0,
                    IsPurchaseEnabled = false,
                    IsVirtualBalanceEnabled = false
                });
            changeResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var ramNewInfo = Connector.Parser.ParseFrom(await Tester.CallTokenConverterMethodAsync(
                nameof(TokenConverterContract.GetConnector),
                new TokenId
                {
                    Symbol = "RAM"
                }));
            ramNewInfo.IsPurchaseEnabled.ShouldBeFalse();
            
            //add connector
            var connectorCpuInfo = Connector.Parser.ParseFrom(
                await Tester.CallTokenConverterMethodAsync(nameof(TokenConverterContract.GetConnector),
                    new TokenId {Symbol = "CPU"}));
            connectorCpuInfo.Symbol.ShouldBeEmpty();
            
            var addResult =
                await manager.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.SetConnector),
                    RamConnector);
            addResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var cpuNewInfo = Connector.Parser.ParseFrom(
                await Tester.CallTokenConverterMethodAsync(nameof(TokenConverterContract.GetConnector),
                    new TokenId {Symbol = "CPU"}));
            cpuNewInfo.Symbol.ShouldBe("CPU");
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

        [Fact]
        public async Task Buy_Success()
        {
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();
            
            var fromConnectorBalance = ELFConnector.VirtualBalance;
            var fromConnectorWeight = ELFConnector.Weight / 100_0000;
            var toConnectorBalance = await Tester.GetBalanceAsync(TokenContractAddress, RamConnector.Symbol);
            var toConnectorWeight = RamConnector.Weight / 100_0000;
            
            var amountToPay = BancorHelpers.GetAmountToPayFromReturn(fromConnectorBalance,fromConnectorWeight,toConnectorBalance,toConnectorWeight,1000L);
            var fee = Convert.ToInt64(amountToPay * 5 / 1000);

            var buyResult = await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Buy),
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = amountToPay+10L
                });

            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balanceOfRam = await Tester.GetBalanceAsync(Tester.GetCallOwnerAddress(), RamConnector.Symbol);
            balanceOfRam.ShouldBe(1000L);

            var balanceOfFeeReceiver = await Tester.GetBalanceAsync(FeeReceiverContractAddress,"ELF");
            balanceOfFeeReceiver.ShouldBe(fee);
        }

        [Fact]
        public async Task Buy_Failed()
        {
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();
            
            var buyResultInvalidSymbol = await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Buy),
                new BuyInput
                {
                    Symbol = "ram",
                    Amount = 1000L,
                    PayLimit = 1010L
                });
            buyResultInvalidSymbol.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResultInvalidSymbol.Error.Contains("Invalid symbol.").ShouldBeTrue();
            
            var buyResultNotExistConnector = await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Buy),
                new BuyInput
                {
                    Symbol = "CPU",
                    Amount = 1000L,
                    PayLimit = 1010L
                });
            buyResultNotExistConnector.Status.ShouldBe(TransactionResultStatus.Mined);
            buyResultNotExistConnector.Error.Contains("Can't find connector.").ShouldBeTrue();
            
            var buyResultPriceNotGood = await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Buy),
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1L
                });
            buyResultPriceNotGood.Status.ShouldBe(TransactionResultStatus.Mined);
            buyResultPriceNotGood.Error.Contains("Price not good.").ShouldBeTrue();
        }

        [Fact]
        public async Task Sell_Success()
        {
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();

            var buyResult = await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Buy),
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1010L
                });
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balanceOfFeeReceiver = await Tester.GetBalanceAsync(FeeReceiverContractAddress,"ELF");
            
            var fromConnectorBalance = ELFConnector.VirtualBalance;
            var fromConnectorWeight = ELFConnector.Weight / 100_0000;
            var toConnectorBalance = await Tester.GetBalanceAsync(TokenContractAddress, RamConnector.Symbol);
            var toConnectorWeight = RamConnector.Weight / 100_0000;
            
            var amountToPay = BancorHelpers.GetAmountToPayFromReturn(fromConnectorBalance,fromConnectorWeight,toConnectorBalance,toConnectorWeight,1000L);
            var fee = Convert.ToInt64(amountToPay * 5 / 1000);
            
            var sellResult =
                await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Sell), new SellInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    ReceiveLimit = 900L
                });
            sellResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balanceOfRam = await Tester.GetBalanceAsync(Tester.GetCallOwnerAddress(), RamConnector.Symbol);
            balanceOfRam.ShouldBe(0L);
            
            var balanceOfFeeReceiverAfterSell = await Tester.GetBalanceAsync(FeeReceiverContractAddress,"ELF");
            balanceOfFeeReceiverAfterSell.ShouldBe(fee+balanceOfFeeReceiver); 
        }

        [Fact]
        public async Task Sell_Failed()
        {
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();

            var buyResult = await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Buy),
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1010L
                });
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var sellResultInvalidSymbol =
                await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Sell), new SellInput
                {
                    Symbol = "ram",
                    Amount = 1000L,
                    ReceiveLimit = 900L
                });
            sellResultInvalidSymbol.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResultInvalidSymbol.Error.Contains("Invalid symbol.").ShouldBeTrue();
            
            var sellResultNotExistConnector = await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Sell),
                new SellInput()
                {
                    Symbol = "CPU",
                    Amount = 1000L,
                    ReceiveLimit = 900L
                });
            sellResultNotExistConnector.Status.ShouldBe(TransactionResultStatus.Mined);
            sellResultNotExistConnector.Error.Contains("Can't find connector.").ShouldBeTrue();
            
            var sellResultPriceNotGood = await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Sell),
                new SellInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    ReceiveLimit = 2000L
                });
            sellResultPriceNotGood.Status.ShouldBe(TransactionResultStatus.Mined);
            sellResultPriceNotGood.Error.Contains("Price not good.").ShouldBeTrue();    
        }

        #endregion
        
        #region Private task
        private async Task CreateRamToken()
        {
            var ceateResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.Create), new CreateInput()
                {
                    Symbol = RamConnector.Symbol,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = Tester.GetCallOwnerAddress(),
                    TokenName = "Ram Resource",
                    TotalSupply = 100_0000
                });
            ceateResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContract.Issue), new IssueInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 100_0000L,
                    Memo="Issue RAM token",
                    To = TokenContractAddress
                });
            issueResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task<TransactionResult> InitializeTokenConverterContract()
        {
            //init token converter
            var input = new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                FeeRateNumerator = 5,
                FeeRateDenominator = 1000,
                Manager = Address.FromPublicKey(ManagerKeyPair.PublicKey),
                MaxWeight = 100_0000,
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverContractAddress,
                Connectors = {ELFConnector, RamConnector}
            };
            return await Tester.ExecuteTokenConverterMethodAsync(nameof(TokenConverterContract.Initialize), input);
        }

        private async Task PrepareToBuyAndSell()
        {
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
        }
        #endregion
    }
}