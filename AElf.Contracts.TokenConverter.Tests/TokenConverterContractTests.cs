using System;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterContractTests : ContractTestBase<TokenConverterTestModule>
    {
        private ISmartContractAddressService ContractAddressService =>
            Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();

        private Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();

        internal BasicContractZeroContainer.BasicContractZeroTester ContractZeroTester =>
            GetTester<BasicContractZeroContainer.BasicContractZeroTester>(ContractZeroAddress, DefaultSenderKeyPair);

        internal TokenContractContainer.TokenContractTester TokenContractTester;
        internal TokenConverterContractContainer.TokenConverterContractTester DefaultTester;
        private ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        private Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        private ECKeyPair FeeReceiverKeyPair { get; } = SampleECKeyPairs.KeyPairs[10];
        public Address FeeReceiverAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        private ECKeyPair ManagerKeyPair { get; } = SampleECKeyPairs.KeyPairs[11];
        private Address ManagerAddress => Address.FromPublicKey(ManagerKeyPair.PublicKey);
        private ECKeyPair FoundationKeyPair { get; } = SampleECKeyPairs.KeyPairs[12];

        private Address BasicZeroContractAddress;
        private Address TokenContractAddress;
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
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };

        private async Task DeployContractsAsync()
        {
            {
                // TokenContract
                var result = await ContractZeroTester.DeploySmartContract.SendAsync(new ContractDeploymentInput()
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))
                });
                TokenContractAddress = result.Output;
                TokenContractTester =
                    GetTester<TokenContractContainer.TokenContractTester>(TokenContractAddress, DefaultSenderKeyPair);

                await TokenContractTester.Create.SendAsync(new CreateInput()
                {
                    Symbol = "ELF",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = 1000_0000L,
                    Issuer = DefaultSender
                });
                await TokenContractTester.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "ELF",
                    Amount = 1000_000L,
                    To = DefaultSender,
                    Memo = "Set for token converter."
                });
            }
            {
                // TokenConverterContract
                var result = await ContractZeroTester.DeploySmartContract.SendAsync(new ContractDeploymentInput()
                {
                    Category = KernelConstants.CodeCoverageRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenConverterContract).Assembly.Location))
                });
                TokenConverterContractAddress = result.Output;
                DefaultTester = GetTester<TokenConverterContractContainer.TokenConverterContractTester>(
                    TokenConverterContractAddress, DefaultSenderKeyPair);
            }
        }

        #region Views Test

        [Fact]
        public async Task ViewTest()
        {
            await DeployContractsAsync();
            await InitializeTokenConverterContract();
            //GetConnector
            var ramConnectorInfo = await DefaultTester.GetConnector.CallAsync(new TokenId
            {
                Symbol = RamConnector.Symbol
            });

            ramConnectorInfo.Weight.ShouldBe(RamConnector.Weight);
            ramConnectorInfo.VirtualBalance.ShouldBe(RamConnector.VirtualBalance);
            ramConnectorInfo.IsPurchaseEnabled.ShouldBe(RamConnector.IsPurchaseEnabled);
            ramConnectorInfo.IsVirtualBalanceEnabled.ShouldBe(RamConnector.IsVirtualBalanceEnabled);

            //GetFeeReceiverAddress
            var feeReceiverAddress = await DefaultTester.GetFeeReceiverAddress.CallAsync(new Empty());
            feeReceiverAddress.ShouldBe(feeReceiverAddress);

            //GetTokenContractAddress
            var tokenContractAddress = await DefaultTester.GetTokenContractAddress.CallAsync(new Empty());
            tokenContractAddress.ShouldBe(TokenContractAddress);
        }

        #endregion

        #region Action Test

        [Fact]
        public async Task Initialize_Failed()
        {
            await DeployContractsAsync();
            //init token converter
            var input = new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                FeeRateNumerator = 5,
                FeeRateDenominator = 1000,
                Manager = ManagerAddress,
                MaxWeight = 1000_0000,
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverAddress,
                Connectors = {RamConnector}
            };

            //token address is null
            {
                input.TokenContractAddress = null;
                var result = (await DefaultTester.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Token contract address required.").ShouldBeTrue();
            }
            //fee address is null
            {
                input.TokenContractAddress = TokenContractAddress;
                input.FeeReceiverAddress = null;
                var result = (await DefaultTester.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Fee receiver address required.").ShouldBeTrue();
            }
            //Base token symbol is invalid.
            {
                input.FeeReceiverAddress = FeeReceiverAddress;
                input.BaseTokenSymbol = "elf1";
                var result = (await DefaultTester.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Base token symbol is invalid.").ShouldBeTrue();
            }
            //Invalid MaxWeight
            {
                input.BaseTokenSymbol = "ELF";
                input.MaxWeight = 0;
                var result = (await DefaultTester.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Invalid MaxWeight.").ShouldBeTrue();
            }
            //Invalid symbol
            {
                input.MaxWeight = 1000_0000;
                RamConnector.Symbol = "ram";
                var result = (await DefaultTester.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Invalid symbol.").ShouldBeTrue();
            }
            //Already initialized
            {
                RamConnector.Symbol = "RAM";
                await InitializeTokenConverterContract();
                var result = (await DefaultTester.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Already initialized.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Set_Connector_Success()
        {
            await DeployContractsAsync();
            await InitializeTokenConverterContract();
            var testerForManager =
                GetTester<TokenConverterContractContainer.TokenConverterContractTester>(TokenConverterContractAddress,
                    ManagerKeyPair);
            var setConnectResult = await testerForManager.SetConnector.SendAsync(new Connector
            {
                Symbol = "RAM",
                VirtualBalance = 0,
                IsPurchaseEnabled = false,
                IsVirtualBalanceEnabled = false
            });
            setConnectResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var ramNewInfo = await testerForManager.GetConnector.CallAsync(new TokenId
            {
                Symbol = "RAM"
            });
            ramNewInfo.IsPurchaseEnabled.ShouldBeFalse();
            
            var connectorsInfo = await DefaultTester.GetConnector.CallAsync(new TokenId {Symbol = "CPU"});
            connectorsInfo.Symbol.ShouldBeEmpty();

            //add Connector
            var result = (await testerForManager.SetConnector.SendAsync(new Connector
            {
                Symbol = "CPU",
                VirtualBalance = 0,
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = false
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            var cpuInfo = await DefaultTester.GetConnector.CallAsync(new TokenId {Symbol = "CPU"});
            cpuInfo.Symbol.ShouldBe("CPU");
        }

        [Fact]
        public async Task Set_Connector_Failed()
        {
            await DeployContractsAsync();
            await InitializeTokenConverterContract();
            var result = (await DefaultTester.SetConnector.SendAsync(
                new Connector
                {
                    Symbol = "RAM",
                    VirtualBalance = 0,
                    IsPurchaseEnabled = false,
                    IsVirtualBalanceEnabled = false
                })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Only manager can perform this action.").ShouldBeTrue();
        }

        [Fact]
        public async Task Buy_Success()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();
            
            //check the price and fee
            var fromConnectorBalance = ELFConnector.VirtualBalance;
            var fromConnectorWeight = ELFConnector.Weight / 100_0000;
            var toConnectorBalance = (await TokenContractTester.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = TokenConverterContractAddress,
                Symbol = RamConnector.Symbol
            })).Balance;

            var toConnectorWeight = RamConnector.Weight / 100_0000;
            
            var amountToPay = BancorHelpers.GetAmountToPayFromReturn(fromConnectorBalance,fromConnectorWeight,toConnectorBalance,toConnectorWeight,1000L);
            var fee = Convert.ToInt64(amountToPay * 5 / 1000);

            var buyResult = (await DefaultTester.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = amountToPay + fee + 10L
                })).TransactionResult;
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //Verify the outcome of the transaction
            var balanceOfTesterRam = (await TokenContractTester.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = DefaultSender,
                    Symbol = RamConnector.Symbol
                })).Balance;
            balanceOfTesterRam.ShouldBe(1000L);
            
            var balanceOfElfToken = (await TokenContractTester.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = TokenConverterContractAddress,
                    Symbol = "ELF"
                })).Balance;
            balanceOfElfToken.ShouldBe(amountToPay);

            var balanceOfFeeReceiver = (await TokenContractTester.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = FeeReceiverAddress,
                    Symbol = "ELF"
                })).Balance;
            balanceOfFeeReceiver.ShouldBe(fee);

            var balanceOfRamToken = (await TokenContractTester.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = TokenConverterContractAddress,
                    Symbol = RamConnector.Symbol
                })).Balance;
            balanceOfRamToken.ShouldBe(100_0000L - 1000L);
            
            var balanceOfTesterToken = (await TokenContractTester.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = DefaultSender,
                    Symbol = "ELF"
                })).Balance;
            balanceOfTesterToken.ShouldBe(100_0000L - amountToPay - fee);
            
        }

        [Fact]
        public async Task Buy_Failed()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();

            var buyResultInvalidSymbol = (await DefaultTester.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = "ram",
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResultInvalidSymbol.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResultInvalidSymbol.Error.Contains("Invalid symbol.").ShouldBeTrue();

            var buyResultNotExistConnector = (await DefaultTester.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = "CPU",
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResultNotExistConnector.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResultNotExistConnector.Error.Contains("Can't find connector.").ShouldBeTrue();

            var buyResultPriceNotGood = (await DefaultTester.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1L
                })).TransactionResult;
            buyResultPriceNotGood.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResultPriceNotGood.Error.Contains("Price not good.").ShouldBeTrue();
        }

        [Fact]
        public async Task Sell_Success()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();

            var buyResult = (await DefaultTester.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var balanceOfFeeReceiver = (await TokenContractTester.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = FeeReceiverAddress,
                Symbol = "ELF"
            })).Balance;
            
            var balanceOfElfToken = (await TokenContractTester.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = TokenConverterContractAddress,
                    Symbol = "ELF"
                })).Balance;
            
            var balanceOfTesterToken = (await TokenContractTester.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = DefaultSender,
                    Symbol = "ELF"
                })).Balance;
           
            //check the price and fee
            var toConnectorBalance = ELFConnector.VirtualBalance + balanceOfElfToken;
            var toConnectorWeight = ELFConnector.Weight / 100_0000;
            var fromConnectorBalance= (await TokenContractTester.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = TokenConverterContractAddress,
                Symbol = RamConnector.Symbol
            })).Balance;
            var fromConnectorWeight = RamConnector.Weight / 100_0000;
            
            var amountToReceive = BancorHelpers.GetReturnFromPaid(fromConnectorBalance,fromConnectorWeight,toConnectorBalance,toConnectorWeight,1000L);
            var fee = Convert.ToInt64(amountToReceive * 5 / 1000);
            
            var sellResult =(await DefaultTester.Sell.SendAsync(new SellInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    ReceiveLimit = amountToReceive - fee - 10L 
                })).TransactionResult;
            sellResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //Verify the outcome of the transaction
            var balanceOfTesterRam = (await TokenContractTester.GetBalance.CallAsync(new GetBalanceInput()
                {
                    Owner = DefaultSender,
                    Symbol = RamConnector.Symbol
                })).Balance;
            balanceOfTesterRam.ShouldBe(0L);
            
            var balanceOfFeeReceiverAfterSell = (await TokenContractTester.GetBalance.CallAsync(new GetBalanceInput()
            {
                Owner = FeeReceiverAddress,
                Symbol = "ELF"
            })).Balance;
            balanceOfFeeReceiverAfterSell.ShouldBe(fee+balanceOfFeeReceiver); 
            
            var balanceOfElfTokenAfterSell = (await TokenContractTester.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = TokenConverterContractAddress,
                    Symbol = "ELF"
                })).Balance;
            balanceOfElfTokenAfterSell.ShouldBe(balanceOfElfToken-amountToReceive);

            var balanceOfRamToken = (await TokenContractTester.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = TokenConverterContractAddress,
                    Symbol = RamConnector.Symbol
                })).Balance;
            balanceOfRamToken.ShouldBe(100_0000L);
            
            var balanceOfTesterTokenAfterSell = (await TokenContractTester.GetBalance.CallAsync(
                new GetBalanceInput()
                {
                    Owner = DefaultSender,
                    Symbol = "ELF"
                })).Balance;
            balanceOfTesterTokenAfterSell.ShouldBe(balanceOfTesterToken + (amountToReceive - fee));
        }

        [Fact]
        public async Task Sell_Failed()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();

            var buyResult = (await DefaultTester.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var sellResultInvalidSymbol = (await DefaultTester.Sell.SendAsync(
                new SellInput
                {
                    Symbol = "ram",
                    Amount = 1000L,
                    ReceiveLimit = 900L
                })).TransactionResult;
            sellResultInvalidSymbol.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResultInvalidSymbol.Error.Contains("Invalid symbol.").ShouldBeTrue();

            var sellResultNotExistConnector = (await DefaultTester.Sell.SendAsync(
                new SellInput()
                {
                    Symbol = "CPU",
                    Amount = 1000L,
                    ReceiveLimit = 900L
                })).TransactionResult;
            sellResultNotExistConnector.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResultNotExistConnector.Error.Contains("Can't find connector.").ShouldBeTrue();

            var sellResultPriceNotGood = (await DefaultTester.Sell.SendAsync(
                new SellInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    ReceiveLimit = 2000L
                })).TransactionResult;
            sellResultPriceNotGood.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResultPriceNotGood.Error.Contains("Price not good.").ShouldBeTrue();
        }

        #endregion

        #region Private Task

        private async Task CreateRamToken()
        {
            var ceateResult = (await TokenContractTester.Create.SendAsync(
                new CreateInput()
                {
                    Symbol = RamConnector.Symbol,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = DefaultSender,
                    TokenName = "Ram Resource",
                    TotalSupply = 100_0000L
                })).TransactionResult;
            ceateResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = (await TokenContractTester.Issue.SendAsync(
                new IssueInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 100_0000L,
                    Memo = "Issue RAM token",
                    To = TokenConverterContractAddress
                })).TransactionResult;
            issueResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task<TransactionResult> InitializeTokenConverterContract()
        {
            //init token converter
            var input = new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                FeeRateNumerator = 5L,
                FeeRateDenominator = 1000L,
                Manager = Address.FromPublicKey(ManagerKeyPair.PublicKey),
                MaxWeight = 100_0000,
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverAddress,
                Connectors = {ELFConnector, RamConnector}
            };
            return (await DefaultTester.Initialize.SendAsync(input)).TransactionResult;
        }

        private async Task PrepareToBuyAndSell()
        {
            //approve
            var approveTokenResult = (await TokenContractTester.Approve.SendAsync(new ApproveInput
                {
                    Spender = TokenConverterContractAddress,
                    Symbol = "ELF",
                    Amount = 2000L,
                })).TransactionResult;
            approveTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);
             
            var approveRamTokenResult = (await TokenContractTester.Approve.SendAsync(new ApproveInput
            {
                Spender = TokenConverterContractAddress,
                Symbol = "RAM",
                Amount = 2000L,
            })).TransactionResult;
            approveRamTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var approveFeeResult = (await TokenContractTester.Approve.SendAsync(
                new ApproveInput
                {
                    Spender = FeeReceiverAddress,
                    Symbol = "ELF",
                    Amount = 2000L,
                })).TransactionResult;
            approveFeeResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        #endregion
    }
}