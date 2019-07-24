using System;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.TokenConverter
{
    public class TokenConverterContractTests : TokenConverterTestBase
    {
        private string _nativeSymbol = "ELF";

        private string _ramSymbol = "RAM";
        
        //init connector
        private Connector ELFConnector = new Connector
        {
            Symbol = "ELF",
            VirtualBalance = 100_0000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true
        };

        private Connector RamConnector = new Connector
        {
            Symbol = "RAM",
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false
        };

        #region Views Test

        [Fact]
        public async Task ViewTest()
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
        public async Task Initialize_Failed()
        {
            await DeployContractsAsync();
            //init token converter
            var input = new InitializeInput
            {
                BaseTokenSymbol = _nativeSymbol,
                FeeRate = "0.005",
                ManagerAddress = ManagerAddress,
                TokenContractAddress = TokenContractAddress,
                FeeReceiverAddress = FeeReceiverAddress,
                Connectors = {RamConnector}
            };

            //token address is null
            {
                input.TokenContractAddress = null;
                var result = (await DefaultStub.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Token contract address required.").ShouldBeTrue();
            }
            //fee address is null
            {
                input.TokenContractAddress = TokenContractAddress;
                input.FeeReceiverAddress = null;
                var result = (await DefaultStub.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Fee receiver address required.").ShouldBeTrue();
            }
            //Base token symbol is invalid.
            {
                input.FeeReceiverAddress = FeeReceiverAddress;
                input.BaseTokenSymbol = "elf1";
                var result = (await DefaultStub.Initialize.SendAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Base token symbol is invalid.").ShouldBeTrue();
            }
            //Invalid MaxWeight
//            {
//                input.BaseTokenSymbol = "ELF";
//                input.MaxWeight = 0;
//                var result = (await DefaultStub.Initialize.SendAsync(input)).TransactionResult;
//                result.Status.ShouldBe(TransactionResultStatus.Failed);
//                result.Error.Contains("Invalid MaxWeight.").ShouldBeTrue();
//            }
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
        public async Task Set_Connector_Success()
        {
            await DeployContractsAsync();
            await InitializeTokenConverterContract();
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

        [Fact]
        public async Task Set_Connector_Failed()
        {
            await DeployContractsAsync();
            await InitializeTokenConverterContract();
            var result = (await DefaultStub.SetConnector.SendAsync(
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
            var fromConnectorWeight = decimal.Parse(ELFConnector.Weight);
            var toConnectorBalance = await GetBalanceAsync(_ramSymbol, TokenConverterContractAddress);
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
            var balanceOfTesterRam = await GetBalanceAsync(_ramSymbol,DefaultSender);
            balanceOfTesterRam.ShouldBe(1000L);

            var balanceOfElfToken = await GetBalanceAsync(_nativeSymbol,TokenConverterContractAddress);
            balanceOfElfToken.ShouldBe(amountToPay);

            var balanceOfFeeReceiver = await GetBalanceAsync(_nativeSymbol,FeeReceiverAddress);
            balanceOfFeeReceiver.ShouldBe(fee);

            var balanceOfRamToken = await GetBalanceAsync(_ramSymbol,TokenConverterContractAddress);
            balanceOfRamToken.ShouldBe(100_0000L - 1000L);

            var balanceOfTesterToken = await GetBalanceAsync(_nativeSymbol,DefaultSender);
            balanceOfTesterToken.ShouldBe(100_0000L - amountToPay - fee);
            
        }

        [Fact]
        public async Task Buy_Failed()
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
        public async Task Sell_Success()
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
            var balanceOfFeeReceiver = await GetBalanceAsync(_nativeSymbol, FeeReceiverAddress);
            var balanceOfElfToken = await GetBalanceAsync(_nativeSymbol,TokenConverterContractAddress);
            var balanceOfTesterToken = await GetBalanceAsync(_nativeSymbol,DefaultSender);
           
            //check the price and fee
            var toConnectorBalance = ELFConnector.VirtualBalance + balanceOfElfToken;
            var toConnectorWeight = decimal.Parse(ELFConnector.Weight);
            var fromConnectorBalance = await GetBalanceAsync(_ramSymbol,TokenConverterContractAddress);
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
            var balanceOfTesterRam = await GetBalanceAsync(_ramSymbol, DefaultSender);
            balanceOfTesterRam.ShouldBe(0L);

            var balanceOfFeeReceiverAfterSell = await GetBalanceAsync(_nativeSymbol,FeeReceiverAddress);
            balanceOfFeeReceiverAfterSell.ShouldBe(fee+balanceOfFeeReceiver);

            var balanceOfElfTokenAfterSell = await GetBalanceAsync(_nativeSymbol, TokenConverterContractAddress);
            balanceOfElfTokenAfterSell.ShouldBe(balanceOfElfToken-amountToReceive);

            var balanceOfRamToken = await GetBalanceAsync(_ramSymbol,TokenConverterContractAddress);
            balanceOfRamToken.ShouldBe(100_0000L);

            var balanceOfTesterTokenAfterSell = await GetBalanceAsync(_nativeSymbol, DefaultSender);
            balanceOfTesterTokenAfterSell.ShouldBe(balanceOfTesterToken + (amountToReceive - fee));
        }

        [Fact]
        public async Task Sell_Failed()
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
        public async Task SetFeeRate_Success()
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
        public async Task SetManagerAddress_Success()
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