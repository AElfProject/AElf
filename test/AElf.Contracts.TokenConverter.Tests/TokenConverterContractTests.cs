using System;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterContractTests : TokenConverterTestBase
    {
        private const string NativeSymbol = "ELF";

        private const string RamSymbol = "RAM";
        
        //init connector
        private Connector ELFConnector = new Connector
        {
            Symbol = NativeSymbol,
            VirtualBalance = 100_0000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true
        };

        private Connector RamConnector = new Connector
        {
            Symbol = RamSymbol,
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };

        #region Views Test

        [Fact]
        public async Task View_Test()
        {
            await DeployContractsAsync();
            await InitializeTokenConverterContract();
            //GetConnector
            var ramConnectorInfo = await DefaultStub.GetConnector.CallAsync(new TokenSymbol()
            {
                Symbol = RamConnector.Symbol
            });

            ramConnectorInfo.Weight.ShouldBe(RamConnector.Weight);
            ramConnectorInfo.VirtualBalance.ShouldBe(RamConnector.VirtualBalance);
            ramConnectorInfo.IsPurchaseEnabled.ShouldBe(RamConnector.IsPurchaseEnabled);
            ramConnectorInfo.IsVirtualBalanceEnabled.ShouldBe(RamConnector.IsVirtualBalanceEnabled);

            //GetFeeReceiverAddress
            var feeReceiverAddress = await DefaultStub.GetFeeReceiverAddress.CallAsync(new Empty());
            feeReceiverAddress.ShouldBe(feeReceiverAddress);

            //GetTokenContractAddress
            var tokenContractAddress = await DefaultStub.GetTokenContractAddress.CallAsync(new Empty());
            tokenContractAddress.ShouldBe(TokenContractAddress);
            
            //GetBaseTokenSymbol
            var tokenSymbol = await DefaultStub.GetBaseTokenSymbol.CallAsync(new Empty());
            tokenSymbol.ShouldNotBeNull();
            tokenSymbol.Symbol.ShouldBe("ELF");
        }

        #endregion

        #region Action Test

        [Fact]
        public async Task Initialize_Failed_Test()
        {
            await DeployContractsAsync();
            //init token converter
            var input = new InitializeInput
            {
                BaseTokenSymbol = NativeSymbol,
                FeeRate = "0.005",
                ManagerAddress = ManagerAddress,
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverAddress,
                Connectors = {RamConnector}
            };

            //Base token symbol is invalid.
            {
                input.FeeReceiverAddress = FeeReceiverAddress;
                input.BaseTokenSymbol = "elf1";
                var result = (await DefaultStub.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Base token symbol is invalid.").ShouldBeTrue();
            }

            //Invalid symbol
            {
                input.BaseTokenSymbol = "ELF";
                RamConnector.Symbol = "ram";
                var result = (await DefaultStub.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Invalid symbol.").ShouldBeTrue();
            }
            
            //Already initialized
            {
                RamConnector.Symbol = "RAM";
                await InitializeTokenConverterContract();
                var result = (await DefaultStub.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Already initialized.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Set_Connector_Test()
        {
            await DeployContractsAsync();
            await InitializeTokenConverterContract();
            
            //not authority user
            {
                var connectorResult = (await DefaultStub.SetConnector.SendAsync(
                    new Connector
                    {
                        Symbol = "RAM",
                        VirtualBalance = 0,
                        IsPurchaseEnabled = false,
                        IsVirtualBalanceEnabled = false
                    })).TransactionResult;
                connectorResult.Status.ShouldBe(TransactionResultStatus.Failed);
                connectorResult.Error.Contains("Only manager can perform this action.").ShouldBeTrue();
            }
            
            //with authority user
            {
                var testerForManager =
                    GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                        ManagerKeyPair);
                var setConnectResult = await testerForManager.SetConnector.SendAsync(new Connector
                {
                    Symbol = "RAM",
                    Weight = "0.5",
                    VirtualBalance = 0,
                    IsPurchaseEnabled = false,
                    IsVirtualBalanceEnabled = false
                });
                setConnectResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var ramNewInfo = await testerForManager.GetConnector.CallAsync(new TokenSymbol()
                {
                    Symbol = "RAM"
                });
                ramNewInfo.IsPurchaseEnabled.ShouldBeFalse();

                var connectorsInfo = await DefaultStub.GetConnector.CallAsync(new TokenSymbol() {Symbol = "CPU"});
                connectorsInfo.Symbol.ShouldBeEmpty();

                //add Connector
                var result = (await testerForManager.SetConnector.SendAsync(new Connector
                {
                    Symbol = "CPU",
                    Weight = "0.5",
                    VirtualBalance = 0,
                    IsPurchaseEnabled = true,
                    IsVirtualBalanceEnabled = false
                })).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Mined);
                var cpuInfo = await DefaultStub.GetConnector.CallAsync(new TokenSymbol() {Symbol = "CPU"});
                cpuInfo.Symbol.ShouldBe("CPU");
            }
        }

        [Fact]
        public async Task Buy_Success_Test()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();
            
            //check the price and fee
            var fromConnectorBalance = ELFConnector.VirtualBalance;
            var fromConnectorWeight = decimal.Parse(ELFConnector.Weight);
            var toConnectorBalance = await GetBalanceAsync(RamSymbol, TokenConverterContractAddress);
            var toConnectorWeight = decimal.Parse(RamConnector.Weight);
            
            var amountToPay = BancorHelper.GetAmountToPayFromReturn(fromConnectorBalance,fromConnectorWeight,toConnectorBalance,toConnectorWeight,1000L);
            var fee = Convert.ToInt64(amountToPay * 5 / 1000);

            var buyResult = (await DefaultStub.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = amountToPay + fee + 10L
                })).TransactionResult;
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //Verify the outcome of the transaction
            var balanceOfTesterRam = await GetBalanceAsync(RamSymbol,DefaultSender);
            balanceOfTesterRam.ShouldBe(1000L);

            var balanceOfElfToken = await GetBalanceAsync(NativeSymbol,TokenConverterContractAddress);
            balanceOfElfToken.ShouldBe(amountToPay);

            var balanceOfFeeReceiver = await GetBalanceAsync(NativeSymbol,FeeReceiverAddress);
            balanceOfFeeReceiver.ShouldBe(fee.Div(2));

            var balanceOfRamToken = await GetBalanceAsync(RamSymbol,TokenConverterContractAddress);
            balanceOfRamToken.ShouldBe(100_0000L - 1000L);

            var balanceOfTesterToken = await GetBalanceAsync(NativeSymbol,DefaultSender);
            balanceOfTesterToken.ShouldBe(100_0000L - amountToPay - fee);
            
        }

        [Fact]
        public async Task Buy_Failed_Test()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();

            var buyResultInvalidSymbol = (await DefaultStub.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = "ram",
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResultInvalidSymbol.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResultInvalidSymbol.Error.Contains("Invalid symbol.").ShouldBeTrue();

            var buyResultNotExistConnector = (await DefaultStub.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = "CPU",
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResultNotExistConnector.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResultNotExistConnector.Error.Contains("Can't find connector.").ShouldBeTrue();

            var buyResultPriceNotGood = (await DefaultStub.Buy.SendAsync(
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
        public async Task Sell_Success_Test()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();

            var buyResult = (await DefaultStub.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //Balance  before Sell
            var balanceOfFeeReceiver = await GetBalanceAsync(NativeSymbol, FeeReceiverAddress);
            var balanceOfElfToken = await GetBalanceAsync(NativeSymbol,TokenConverterContractAddress);
            var balanceOfTesterToken = await GetBalanceAsync(NativeSymbol,DefaultSender);
           
            //check the price and fee
            var toConnectorBalance = ELFConnector.VirtualBalance + balanceOfElfToken;
            var toConnectorWeight = decimal.Parse(ELFConnector.Weight);
            var fromConnectorBalance = await GetBalanceAsync(RamSymbol,TokenConverterContractAddress);
            var fromConnectorWeight = decimal.Parse(RamConnector.Weight);
            
            var amountToReceive = BancorHelper.GetReturnFromPaid(fromConnectorBalance,fromConnectorWeight,toConnectorBalance,toConnectorWeight,1000L);
            var fee = Convert.ToInt64(amountToReceive * 5 / 1000);
            
            var sellResult =(await DefaultStub.Sell.SendAsync(new SellInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    ReceiveLimit = amountToReceive - fee - 10L 
                })).TransactionResult;
            sellResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //Verify the outcome of the transaction
            var balanceOfTesterRam = await GetBalanceAsync(RamSymbol, DefaultSender);
            balanceOfTesterRam.ShouldBe(0L);

            var balanceOfFeeReceiverAfterSell = await GetBalanceAsync(NativeSymbol,FeeReceiverAddress);
            balanceOfFeeReceiverAfterSell.ShouldBe(fee.Div(2) + balanceOfFeeReceiver);

            var balanceOfElfTokenAfterSell = await GetBalanceAsync(NativeSymbol, TokenConverterContractAddress);
            balanceOfElfTokenAfterSell.ShouldBe(balanceOfElfToken-amountToReceive + fee);

            var balanceOfRamToken = await GetBalanceAsync(RamSymbol,TokenConverterContractAddress);
            balanceOfRamToken.ShouldBe(100_0000L);

            var balanceOfTesterTokenAfterSell = await GetBalanceAsync(NativeSymbol, DefaultSender);
            balanceOfTesterTokenAfterSell.ShouldBe(balanceOfTesterToken + (amountToReceive - fee) - fee);
        }

        [Fact]
        public async Task Sell_Failed_Test()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();

            var buyResult = (await DefaultStub.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var sellResultInvalidSymbol = (await DefaultStub.Sell.SendAsync(
                new SellInput
                {
                    Symbol = "ram",
                    Amount = 1000L,
                    ReceiveLimit = 900L
                })).TransactionResult;
            sellResultInvalidSymbol.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResultInvalidSymbol.Error.Contains("Invalid symbol.").ShouldBeTrue();

            var sellResultNotExistConnector = (await DefaultStub.Sell.SendAsync(
                new SellInput()
                {
                    Symbol = "CPU",
                    Amount = 1000L,
                    ReceiveLimit = 900L
                })).TransactionResult;
            sellResultNotExistConnector.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResultNotExistConnector.Error.Contains("Can't find connector.").ShouldBeTrue();

            var sellResultPriceNotGood = (await DefaultStub.Sell.SendAsync(
                new SellInput
                {
                    Symbol = RamConnector.Symbol,
                    Amount = 1000L,
                    ReceiveLimit = 2000L
                })).TransactionResult;
            sellResultPriceNotGood.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResultPriceNotGood.Error.Contains("Price not good.").ShouldBeTrue();
        }

        [Fact]
        public async Task SetFeeRate_Success_Test()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();

            //perform by non manager
            {
                var transactionResult = (await DefaultStub.SetFeeRate.SendAsync(
                    new StringValue
                    {
                        Value = "test value"
                    })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Only manager can perform this action").ShouldBeTrue();
            }
            
            var testManager = GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                ManagerKeyPair);
            
            //invalid feeRate
            {
                var transactionResult = (await testManager.SetFeeRate.SendAsync(
                    new StringValue
                    {
                        Value = "test value"
                    })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid decimal").ShouldBeTrue();            
            }
            
            //feeRate not correct
            {
                var transactionResult = (await testManager.SetFeeRate.SendAsync(
                    new StringValue
                    {
                        Value = "1.05"
                    })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Fee rate has to be a decimal between 0 and 1").ShouldBeTrue(); 
            }
            
            //correct 
            {
                var feeRate = new StringValue
                {
                    Value = "0.15"
                };
                var transactionResult = (await testManager.SetFeeRate.SendAsync(feeRate))
                    .TransactionResult;
                
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                var feeRate1 = await testManager.GetFeeRate.CallAsync(new Empty());
                feeRate1.ShouldBe(feeRate);
            }
        }

        [Fact]
        public async Task SetManagerAddress_Success_Test()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();

            //perform by non manager
            {
                var transactionResult = (await DefaultStub.SetManagerAddress.SendAsync(
                    new Address()
                    )).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Only manager can perform this action").ShouldBeTrue();
            }

            var testManager = GetTester<TokenConverterContractContainer.TokenConverterContractStub>(
                TokenConverterContractAddress,
                ManagerKeyPair);
            
            //invalid address
            {
                var transactionResult = (await testManager.SetManagerAddress.SendAsync(
                    new Address()
                )).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Input is not a valid address").ShouldBeTrue();
            }
            
            //valid address
            {
                var address = SampleAddress.AddressList[0];
                
                var transactionResult = (await testManager.SetManagerAddress.SendAsync(address)).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var managerAddress = await testManager.GetManagerAddress.CallAsync(new Empty());
                managerAddress.ShouldBe(address);
            }
        }
        
        #endregion

        #region Private Task
        
        private async Task CreateRamToken()
        {
            var createResult = (await TokenContractStub.Create.SendAsync(
                new CreateInput()
                {
                    Symbol = RamConnector.Symbol,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = DefaultSender,
                    TokenName = "Ram Resource",
                    TotalSupply = 100_0000L
                })).TransactionResult;
            createResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = (await TokenContractStub.Issue.SendAsync(
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
                FeeRate = "0.005",
                ManagerAddress = Address.FromPublicKey(ManagerKeyPair.PublicKey),
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverAddress,
                Connectors = {ELFConnector, RamConnector}
            };
            return (await DefaultStub.Initialize.SendAsync(input)).TransactionResult;
        }

        private async Task PrepareToBuyAndSell()
        {
            //approve
            var approveTokenResult = (await TokenContractStub.Approve.SendAsync(new ApproveInput
                {
                    Spender = TokenConverterContractAddress,
                    Symbol = "ELF",
                    Amount = 2000L,
                })).TransactionResult;
            approveTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);
             
            var approveRamTokenResult = (await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = TokenConverterContractAddress,
                Symbol = "RAM",
                Amount = 2000L,
            })).TransactionResult;
            approveRamTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var approveFeeResult = (await TokenContractStub.Approve.SendAsync(
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