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

        private const string WriteSymbol = "WRITE";

        //init connector
        private Connector ELFConnector = new Connector
        {
            Symbol = NativeSymbol,
            VirtualBalance = 100_0000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true
        };

        private Connector WriteConnector = new Connector
        {
            Symbol = WriteSymbol,
            VirtualBalance = 0,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = false,
            RelatedSymbol = "NT" + WriteSymbol,
            IsDepositAccount = false
        };
        
        private Connector NtWriteConnector = new Connector
        {
            Symbol = "NT" + WriteSymbol,
            VirtualBalance = 100_0000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true,
            RelatedSymbol = WriteSymbol,
            IsDepositAccount = true
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
                Symbol = WriteConnector.Symbol
            });

            ramConnectorInfo.Weight.ShouldBe(WriteConnector.Weight);
            ramConnectorInfo.VirtualBalance.ShouldBe(WriteConnector.VirtualBalance);
            ramConnectorInfo.IsPurchaseEnabled.ShouldBe(WriteConnector.IsPurchaseEnabled);
            ramConnectorInfo.IsVirtualBalanceEnabled.ShouldBe(WriteConnector.IsVirtualBalanceEnabled);

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
                Connectors = {WriteConnector}
            };

            //Base token symbol is invalid.
            {
                input.FeeReceiverAddress = FeeReceiverAddress;
                input.BaseTokenSymbol = "elf1";
                var result = (await DefaultStub.Initialize.SendWithExceptionAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Base token symbol is invalid.").ShouldBeTrue();
            }

            //Invalid symbol
            {
                input.BaseTokenSymbol = "ELF";
                WriteConnector.Symbol = "write";
                var result = (await DefaultStub.Initialize.SendWithExceptionAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Invalid symbol.").ShouldBeTrue();
            }

            //Already initialized
            {
                WriteConnector.Symbol = "WRITE";
                await InitializeTokenConverterContract();
                var result = (await DefaultStub.Initialize.SendWithExceptionAsync(input)).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Failed);
                result.Error.Contains("Already initialized.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Set_Connector_Test()
        {
            await DeployContractsAsync();
            await InitializeTokenConverterContract();
            //with authority user
            {
                var createTokenRet = (await AuthorizedTokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "TRAFFIC",
                    TokenName = "NET name",
                    TotalSupply = 100_0000_0000,
                    Issuer = ManagerAddress,
                    IsBurnable = true
                })).TransactionResult;
                createTokenRet.Status.ShouldBe(TransactionResultStatus.Mined);
                var setConnectResult = await AuthorizedTokenConvertStub.AddPairConnectors.SendAsync(new PairConnector
                {
                    ResourceConnectorSymbol = "TRAFFIC",
                    ResourceWeight = "0.05",
                    NativeWeight = "0.05",
                    NativeVirtualBalance = 1_000_000_00000000,
                });
                setConnectResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var ramNewInfo = await AuthorizedTokenConvertStub.GetConnector.CallAsync(new TokenSymbol()
                {
                    Symbol = "TRAFFIC"
                });
                ramNewInfo.IsPurchaseEnabled.ShouldBeFalse();

                var createTokenRet2 = (await AuthorizedTokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "READ",
                    TokenName = "READ name",
                    TotalSupply = 100_0000_0000,
                    Issuer = ManagerAddress,
                    IsBurnable = true
                })).TransactionResult;
                createTokenRet2.Status.ShouldBe(TransactionResultStatus.Mined);
                var connectorsInfo = await DefaultStub.GetConnector.CallAsync(new TokenSymbol {Symbol = "READ"});
                connectorsInfo.Symbol.ShouldBeEmpty();

                //add Connector
                var result = (await AuthorizedTokenConvertStub.AddPairConnectors.SendAsync(new PairConnector
                {
                    ResourceConnectorSymbol = "READ",
                    ResourceWeight = "0.05",
                    NativeWeight = "0.05",
                    NativeVirtualBalance = 1_000_000_00000000,
                })).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Mined);
                var readInfo = await DefaultStub.GetConnector.CallAsync(new TokenSymbol {Symbol = "READ"});
                readInfo.Symbol.ShouldBe("READ");
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
            var toConnectorBalance = await GetBalanceAsync(WriteSymbol, TokenConverterContractAddress);
            var toConnectorWeight = decimal.Parse(WriteConnector.Weight);

            var amountToPay = BancorHelper.GetAmountToPayFromReturn(fromConnectorBalance, fromConnectorWeight,
                toConnectorBalance, toConnectorWeight, 1000L);
            var fee = Convert.ToInt64(amountToPay * 5 / 1000);

            var buyResult = (await DefaultStub.Buy.SendAsync(
                new BuyInput
                {
                    Symbol = WriteConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = amountToPay + fee + 10L
                })).TransactionResult;
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Verify the outcome of the transaction
            var balanceOfTesterWrite = await GetBalanceAsync(WriteSymbol, DefaultSender);
            balanceOfTesterWrite.ShouldBe(1000L);

            var ElfBalanceLoggedInTokenConvert = await DefaultStub.GetDepositConnectorBalance.CallAsync(new StringValue
            {
                Value = WriteConnector.Symbol
            });
            ElfBalanceLoggedInTokenConvert.Value.ShouldBe(ELFConnector.VirtualBalance + amountToPay);
            var balanceOfElfToken = await GetBalanceAsync(NativeSymbol, TokenConverterContractAddress);
            balanceOfElfToken.ShouldBe(amountToPay);

            var balanceOfFeeReceiver = await GetBalanceAsync(NativeSymbol, FeeReceiverAddress);
            balanceOfFeeReceiver.ShouldBe(10000000000 + fee.Div(2));

            var balanceOfRamToken = await GetBalanceAsync(WriteSymbol, TokenConverterContractAddress);
            balanceOfRamToken.ShouldBe(100_0000L - 1000L);

            var balanceOfTesterToken = await GetBalanceAsync(NativeSymbol, DefaultSender);
            balanceOfTesterToken.ShouldBe(100_0000L - amountToPay - fee);
        }

        [Fact]
        public async Task Buy_Failed_Test()
        {
            await DeployContractsAsync();
            await CreateRamToken();
            await InitializeTokenConverterContract();
            await PrepareToBuyAndSell();

            var buyResultInvalidSymbol = (await DefaultStub.Buy.SendWithExceptionAsync(
                new BuyInput
                {
                    Symbol = "write",
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResultInvalidSymbol.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResultInvalidSymbol.Error.Contains("Invalid symbol.").ShouldBeTrue();

            var buyResultNotExistConnector = (await DefaultStub.Buy.SendWithExceptionAsync(
                new BuyInput
                {
                    Symbol = "READ",
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResultNotExistConnector.Status.ShouldBe(TransactionResultStatus.Failed);
            buyResultNotExistConnector.Error.Contains("Can't find to connector.").ShouldBeTrue();

            var buyResultPriceNotGood = (await DefaultStub.Buy.SendWithExceptionAsync(
                new BuyInput
                {
                    Symbol = WriteConnector.Symbol,
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
                    Symbol = WriteConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Balance  before Sell
            var balanceOfFeeReceiver = await GetBalanceAsync(NativeSymbol, FeeReceiverAddress);
            var balanceOfElfToken = await GetBalanceAsync(NativeSymbol, TokenConverterContractAddress);
            var balanceOfTesterToken = await GetBalanceAsync(NativeSymbol, DefaultSender);

            //check the price and fee
            var toConnectorBalance = ELFConnector.VirtualBalance + balanceOfElfToken;
            var toConnectorWeight = decimal.Parse(ELFConnector.Weight);
            var fromConnectorBalance = await GetBalanceAsync(WriteSymbol, TokenConverterContractAddress);
            var fromConnectorWeight = decimal.Parse(WriteConnector.Weight);

            var amountToReceive = BancorHelper.GetReturnFromPaid(fromConnectorBalance, fromConnectorWeight,
                toConnectorBalance, toConnectorWeight, 1000L);
            var fee = Convert.ToInt64(amountToReceive * 5 / 1000);

            var sellResult = (await DefaultStub.Sell.SendAsync(new SellInput
            {
                Symbol = WriteConnector.Symbol,
                Amount = 1000L,
                ReceiveLimit = amountToReceive - fee - 10L
            })).TransactionResult;
            sellResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //Verify the outcome of the transaction
            var balanceOfTesterRam = await GetBalanceAsync(WriteSymbol, DefaultSender);
            balanceOfTesterRam.ShouldBe(0L);

            var balanceOfFeeReceiverAfterSell = await GetBalanceAsync(NativeSymbol, FeeReceiverAddress);
            balanceOfFeeReceiverAfterSell.ShouldBe(fee.Div(2) + balanceOfFeeReceiver);

            var balanceOfElfTokenAfterSell = await GetBalanceAsync(NativeSymbol, TokenConverterContractAddress);
            balanceOfElfTokenAfterSell.ShouldBe(balanceOfElfToken - amountToReceive + fee);

            var balanceOfRamToken = await GetBalanceAsync(WriteSymbol, TokenConverterContractAddress);
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
                    Symbol = WriteConnector.Symbol,
                    Amount = 1000L,
                    PayLimit = 1010L
                })).TransactionResult;
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var sellResultInvalidSymbol = (await DefaultStub.Sell.SendWithExceptionAsync(
                new SellInput
                {
                    Symbol = "write",
                    Amount = 1000L,
                    ReceiveLimit = 900L
                })).TransactionResult;
            sellResultInvalidSymbol.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResultInvalidSymbol.Error.Contains("Invalid symbol.").ShouldBeTrue();

            var sellResultNotExistConnector = (await DefaultStub.Sell.SendWithExceptionAsync(
                new SellInput()
                {
                    Symbol = "READ",
                    Amount = 1000L,
                    ReceiveLimit = 900L
                })).TransactionResult;
            sellResultNotExistConnector.Status.ShouldBe(TransactionResultStatus.Failed);
            sellResultNotExistConnector.Error.Contains("Can't find from connector.").ShouldBeTrue();

            var sellResultPriceNotGood = (await DefaultStub.Sell.SendWithExceptionAsync(
                new SellInput
                {
                    Symbol = WriteConnector.Symbol,
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
                var transactionResult = (await DefaultStub.SetFeeRate.SendWithExceptionAsync(
                    new StringValue
                    {
                        Value = "test value"
                    })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Only manager can perform this action").ShouldBeTrue();
            }

            var testManager = GetTester<TokenConverterContractContainer.TokenConverterContractStub>(
                TokenConverterContractAddress,
                ManagerKeyPair);

            //invalid feeRate
            {
                var transactionResult = (await testManager.SetFeeRate.SendWithExceptionAsync(
                    new StringValue
                    {
                        Value = "test value"
                    })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid decimal").ShouldBeTrue();
            }

            //feeRate not correct
            {
                var transactionResult = (await testManager.SetFeeRate.SendWithExceptionAsync(
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
                var transactionResult = (await DefaultStub.SetManagerAddress.SendWithExceptionAsync(
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
                var transactionResult = (await testManager.SetManagerAddress.SendWithExceptionAsync(
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

        [Fact]
        public async Task Update_Connector_Success_Test()
        {
            string token = "NETT";
            await DeployContractsAsync();
            await InitializeTokenConverterContract();
            var createTokenRet = (await AuthorizedTokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = token,
                TokenName = "NETT name",
                TotalSupply = 100_0000_0000,
                Issuer = ManagerAddress,
                IsBurnable = true,
                LockWhiteList = { TokenConverterContractAddress}
            })).TransactionResult;
            createTokenRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var pairConnector = new PairConnector
            {
                ResourceConnectorSymbol = token,
                ResourceWeight = "0.05",
                NativeWeight = "0.05",
                NativeVirtualBalance = 1_0000_0000,
            };
            var ret = (await AuthorizedTokenConvertStub.AddPairConnectors.SendAsync(pairConnector)).TransactionResult;
            ret.Status.ShouldBe(TransactionResultStatus.Mined);
            var updateConnector = new Connector
            {
                Symbol = token,
                VirtualBalance = 1000_000,
                IsVirtualBalanceEnabled = false,
                IsPurchaseEnabled = true,
                Weight = "0.49",
                RelatedSymbol = "change"
            };
            var updateRet =  (await AuthorizedTokenConvertStub.UpdateConnector.SendAsync(updateConnector)).TransactionResult;
            updateRet.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task Add_Pair_Connector_And_Enable_Success_Test()
        {
            string token = "NETT";
            await DeployContractsAsync();
            await InitializeTokenConverterContract();
            var createTokenRet = (await AuthorizedTokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = token,
                TokenName = "NETT name",
                TotalSupply = 100_0000_0000,
                Issuer = ManagerAddress,
                IsBurnable = true,
                LockWhiteList = { TokenConverterContractAddress}
            })).TransactionResult;
            createTokenRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var pairConnector = new PairConnector
            {
                ResourceConnectorSymbol = token,
                ResourceWeight = "0.05",
                NativeWeight = "0.05",
                NativeVirtualBalance = 1_0000_0000,
            };
            var ret = (await AuthorizedTokenConvertStub.AddPairConnectors.SendAsync(pairConnector)).TransactionResult;
            ret.Status.ShouldBe(TransactionResultStatus.Mined);
            var resourceConnector = await AuthorizedTokenConvertStub.GetConnector.CallAsync(new TokenSymbol {Symbol = token});
            var nativeToResourceConnector =
                await AuthorizedTokenConvertStub.GetConnector.CallAsync(new TokenSymbol {Symbol = token});
            resourceConnector.ShouldNotBeNull();
            resourceConnector.IsPurchaseEnabled.ShouldBe(false);
            nativeToResourceConnector.ShouldNotBeNull();
            nativeToResourceConnector.IsPurchaseEnabled.ShouldBe(false);
            var issueRet = (await AuthorizedTokenContractStub.Issue.SendAsync(new IssueInput
            {
                Amount = 99_9999_0000,
                To = ManagerAddress,
                Symbol = token
            })).TransactionResult;
            issueRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var toBeBuildConnectorInfo = new ToBeConnectedTokenInfo
            {
                TokenSymbol = token,
                AmountToTokenConvert = 99_9999_0000
            }; 
            var deposit = await AuthorizedTokenConvertStub.GetNeededDeposit.CallAsync(toBeBuildConnectorInfo);
            deposit.NeedAmount.ShouldBe(100);
            var buildRet = (await AuthorizedTokenConvertStub.EnableConnector.SendAsync(toBeBuildConnectorInfo)).TransactionResult;
            buildRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenInTokenConvert = await GetBalanceAsync(token, TokenConverterContractAddress);
            tokenInTokenConvert.ShouldBe(99_9999_0000);
            resourceConnector = await AuthorizedTokenConvertStub.GetConnector.CallAsync(new TokenSymbol {Symbol = token});
            nativeToResourceConnector =
                await AuthorizedTokenConvertStub.GetConnector.CallAsync(new TokenSymbol {Symbol = token});
            resourceConnector.ShouldNotBeNull();
            resourceConnector.IsPurchaseEnabled.ShouldBe(true);
            nativeToResourceConnector.ShouldNotBeNull();
            nativeToResourceConnector.IsPurchaseEnabled.ShouldBe(true);
            var beforeTokenBalance = await GetBalanceAsync(token, ManagerAddress);
            var beforeBaseBalance = await GetBalanceAsync(NativeSymbol, ManagerAddress);
            var buyRet = (await AuthorizedTokenConvertStub.Buy.SendAsync(new BuyInput
            {
                Symbol = token,
                Amount = 10000
            })).TransactionResult;
            buyRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterTokenBalance = await GetBalanceAsync(token, ManagerAddress);
            var afterBaseBalance = await GetBalanceAsync(NativeSymbol, ManagerAddress);
            (afterTokenBalance - beforeTokenBalance).ShouldBe(10000);
            (beforeBaseBalance - afterBaseBalance).ShouldBe(100);
        }

        #endregion

        #region Private Task
        private async Task CreateRamToken()
        {
            var createResult = (await TokenContractStub.Create.SendAsync(
                new CreateInput()
                {
                    Symbol = WriteConnector.Symbol,
                    Decimals = 2,
                    IsBurnable = true,
                    Issuer = DefaultSender,
                    TokenName = "Write Resource",
                    TotalSupply = 100_0000L
                })).TransactionResult;
            createResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = (await TokenContractStub.Issue.SendAsync(
                new IssueInput
                {
                    Symbol = WriteConnector.Symbol,
                    Amount = 100_0000L,
                    Memo = "Issue WRITE token",
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
                Connectors = {ELFConnector, WriteConnector, NtWriteConnector}
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
                Symbol = "WRITE",
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