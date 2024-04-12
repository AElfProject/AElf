using System;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.TokenConverter;

public partial class TokenConverterContractTests : TokenConverterTestBase
{
    private const string NativeSymbol = "ELF";

    private const string WriteSymbol = "WRITE";

    //init connector
    private readonly Connector ELFConnector = new()
    {
        Symbol = NativeSymbol,
        VirtualBalance = 100_0000,
        Weight = "0.5",
        IsPurchaseEnabled = true,
        IsVirtualBalanceEnabled = true
    };

    private readonly Connector NtWriteConnector = new()
    {
        Symbol = "NT" + WriteSymbol,
        VirtualBalance = 100_0000,
        Weight = "0.5",
        IsPurchaseEnabled = true,
        IsVirtualBalanceEnabled = true,
        RelatedSymbol = WriteSymbol,
        IsDepositAccount = true
    };

    private readonly Connector WriteConnector = new()
    {
        Symbol = WriteSymbol,
        VirtualBalance = 0,
        Weight = "0.5",
        IsPurchaseEnabled = true,
        IsVirtualBalanceEnabled = false,
        RelatedSymbol = "NT" + WriteSymbol,
        IsDepositAccount = false
    };

    #region Views Test

    [Fact]
    public async Task GetBaseTokenSymbol_Test()
    {
        await InitializeTokenConverterContract();
        var tokenSymbol = await DefaultStub.GetBaseTokenSymbol.CallAsync(new Empty());
        tokenSymbol.ShouldNotBeNull();
        tokenSymbol.Symbol.ShouldBe("ELF");
    }

    [Fact]
    public async Task GetPairConnector_Test()
    {
        await InitializeTokenConverterContract();
        var ramConnectorInfo = (await DefaultStub.GetPairConnector.CallAsync(new TokenSymbol
        {
            Symbol = WriteConnector.Symbol
        })).ResourceConnector;

        ramConnectorInfo.Weight.ShouldBe(WriteConnector.Weight);
        ramConnectorInfo.VirtualBalance.ShouldBe(WriteConnector.VirtualBalance);
        ramConnectorInfo.IsPurchaseEnabled.ShouldBe(WriteConnector.IsPurchaseEnabled);
        ramConnectorInfo.IsVirtualBalanceEnabled.ShouldBe(WriteConnector.IsVirtualBalanceEnabled);
    }

    #endregion

    #region Action Test

    [Fact]
    public async Task Initialize_Failed_Test()
    {
        //Base token symbol is invalid.
        {
            var input = GetLegalInitializeInput();
            input.BaseTokenSymbol = "elf1";
            var result = (await DefaultStub.Initialize.SendWithExceptionAsync(input)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Base token symbol is invalid.").ShouldBeTrue();
        }

        //invalid fee rate
        {
            var input = GetLegalInitializeInput();
            input.FeeRate = "1ass";
            var result = (await DefaultStub.Initialize.SendWithExceptionAsync(input)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Invalid decimal \"1ass\"").ShouldBeTrue();
        }

        //invalid fee rate  fee rate should >0 && < 1
        {
            var input = GetLegalInitializeInput();
            input.FeeRate = "1";
            var result = (await DefaultStub.Initialize.SendWithExceptionAsync(input)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Fee rate has to be a decimal between 0 and 1.").ShouldBeTrue();
        }

        //Invalid connector symbol
        {
            var input = GetLegalInitializeInput();
            input.Connectors[0].Symbol = "write";
            var result = (await DefaultStub.Initialize.SendWithExceptionAsync(input)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Invalid symbol.").ShouldBeTrue();
        }

        //invalid weight rate
        {
            var input = GetLegalInitializeInput();
            input.Connectors[0].Weight = "1";
            var result = (await DefaultStub.Initialize.SendWithExceptionAsync(input)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Connector Shares has to be a decimal between 0 and 1.").ShouldBeTrue();
        }

        //invalid weight rate
        {
            var input = GetLegalInitializeInput();
            input.Connectors[0].Weight = "weight1ass";
            var result = (await DefaultStub.Initialize.SendWithExceptionAsync(input)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Invalid decimal \"weight1ass\"").ShouldBeTrue();
        }

        //Already initialized
        {
            var input = GetLegalInitializeInput();
            await InitializeTokenConverterContract();
            var result = (await DefaultStub.Initialize.SendWithExceptionAsync(input)).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Already initialized.").ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Initialize_With_Default_Base_Token_Test()
    {
        var input = GetLegalInitializeInput();
        input.BaseTokenSymbol = string.Empty;
        await DefaultStub.Initialize.SendAsync(input);
        var baseTokenSymbol = await DefaultStub.GetBaseTokenSymbol.CallAsync(new Empty());
        baseTokenSymbol.Symbol.Equals(NativeSymbol).ShouldBeTrue();
    }

    [Fact]
    public async Task Buy_Success_Test()
    {
        await CreateRamToken();
        await InitializeTreasuryContractAsync();
        await InitializeTokenConverterContract();
        await PrepareToBuyAndSell();

        //check the price and fee
        var fromConnectorBalance = ELFConnector.VirtualBalance;
        var fromConnectorWeight = decimal.Parse(ELFConnector.Weight);
        var toConnectorBalance = await GetBalanceAsync(WriteSymbol, TokenConverterContractAddress);
        var toConnectorWeight = decimal.Parse(WriteConnector.Weight);

        var amountToPay = BancorHelper.GetAmountToPayFromReturn(fromConnectorBalance, fromConnectorWeight,
            toConnectorBalance, toConnectorWeight, 1000L);
        var depositAmountBeforeBuy = await DefaultStub.GetDepositConnectorBalance.CallAsync(new StringValue
        {
            Value = WriteConnector.Symbol
        });
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
        var depositAmountAfterBuy = await DefaultStub.GetDepositConnectorBalance.CallAsync(new StringValue
        {
            Value = WriteConnector.Symbol
        });
        depositAmountAfterBuy.Value.Sub(depositAmountBeforeBuy.Value).ShouldBe(amountToPay);
        var balanceOfTesterWrite = await GetBalanceAsync(WriteSymbol, DefaultSender);
        balanceOfTesterWrite.ShouldBe(1000L);

        var ElfBalanceLoggedInTokenConvert = await DefaultStub.GetDepositConnectorBalance.CallAsync(new StringValue
        {
            Value = WriteConnector.Symbol
        });
        ElfBalanceLoggedInTokenConvert.Value.ShouldBe(ELFConnector.VirtualBalance + amountToPay);
        var balanceOfElfToken = await GetBalanceAsync(NativeSymbol, TokenConverterContractAddress);
        balanceOfElfToken.ShouldBe(amountToPay);

        var donatedFee = await TreasuryContractStub.GetUndistributedDividends.CallAsync(new Empty());
        donatedFee.Value[NativeSymbol].ShouldBe(fee.Div(2));

        var balanceOfRamToken = await GetBalanceAsync(WriteSymbol, TokenConverterContractAddress);
        balanceOfRamToken.ShouldBe(100_0000L - 1000L);

        var balanceOfTesterToken = await GetBalanceAsync(NativeSymbol, DefaultSender);
        balanceOfTesterToken.ShouldBe(100_0000L - amountToPay - fee);
    }

    [Fact]
    public async Task Buy_With_Invalid_Input_Test()
    {
        await CreateRamToken();
        await InitializeTokenConverterContract();
        await PrepareToBuyAndSell();

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
        await CreateRamToken();
        await InitializeTreasuryContractAsync();
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
        var treasuryBeforeSell =
            (await TreasuryContractStub.GetUndistributedDividends.CallAsync(new Empty())).Value[NativeSymbol];
        var balanceOfElfToken = await GetBalanceAsync(NativeSymbol, TokenConverterContractAddress);
        var balanceOfTesterToken = await GetBalanceAsync(NativeSymbol, DefaultSender);

        //check the price and fee
        var toConnectorBalance = ELFConnector.VirtualBalance + balanceOfElfToken;
        var toConnectorWeight = decimal.Parse(ELFConnector.Weight);
        var fromConnectorBalance = await GetBalanceAsync(WriteSymbol, TokenConverterContractAddress);
        var fromConnectorWeight = decimal.Parse(WriteConnector.Weight);

        var amountToReceive = BancorHelper.GetReturnFromPaid(fromConnectorBalance, fromConnectorWeight,
            toConnectorBalance, toConnectorWeight, 1000L);
        var depositAmountBeforeSell = await DefaultStub.GetDepositConnectorBalance.CallAsync(new StringValue
        {
            Value = WriteConnector.Symbol
        });
        var fee = Convert.ToInt64(amountToReceive * 5 / 1000);

        var sellResult = (await DefaultStub.Sell.SendAsync(new SellInput
        {
            Symbol = WriteConnector.Symbol,
            Amount = 1000L,
            ReceiveLimit = amountToReceive - fee - 10L
        })).TransactionResult;
        sellResult.Status.ShouldBe(TransactionResultStatus.Mined);

        //Verify the outcome of the transaction
        var depositAmountAfterSell = await DefaultStub.GetDepositConnectorBalance.CallAsync(new StringValue
        {
            Value = WriteConnector.Symbol
        });
        depositAmountBeforeSell.Value.Sub(depositAmountAfterSell.Value).ShouldBe(amountToReceive);
        var balanceOfTesterRam = await GetBalanceAsync(WriteSymbol, DefaultSender);
        balanceOfTesterRam.ShouldBe(0L);

        var treasuryAfterSell = await TreasuryContractStub.GetUndistributedDividends.CallAsync(new Empty());
        treasuryAfterSell.Value[NativeSymbol].ShouldBe(fee.Div(2) + treasuryBeforeSell);

        var balanceOfElfTokenAfterSell = await GetBalanceAsync(NativeSymbol, TokenConverterContractAddress);
        balanceOfElfTokenAfterSell.ShouldBe(balanceOfElfToken - amountToReceive);

        var balanceOfRamToken = await GetBalanceAsync(WriteSymbol, TokenConverterContractAddress);
        balanceOfRamToken.ShouldBe(100_0000L);

        var balanceOfTesterTokenAfterSell = await GetBalanceAsync(NativeSymbol, DefaultSender);
        balanceOfTesterTokenAfterSell.ShouldBe(balanceOfTesterToken + amountToReceive - fee);
    }

    [Fact]
    public async Task Sell_With_Invalid_Input_Test()
    {
        await CreateRamToken();
        await InitializeTreasuryContractAsync();
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

        var sellResultNotExistConnector = (await DefaultStub.Sell.SendWithExceptionAsync(
            new SellInput
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

    #endregion

    #region Private Task

    private InitializeInput GetLegalInitializeInput()
    {
        var writeConnector = Connector.Parser.ParseFrom(WriteConnector.ToByteString());
        return new InitializeInput
        {
            BaseTokenSymbol = NativeSymbol,
            FeeRate = "0.005",
            Connectors = { writeConnector }
        };
    }

    private async Task CreateRamToken()
    {
        await ExecuteProposalForParliamentTransaction(TokenContractAddress, nameof(TokenContractStub.Create),
            new CreateInput
            {
                Symbol = WriteConnector.Symbol,
                Decimals = 2,
                IsBurnable = true,
                Issuer = DefaultSender,
                Owner = DefaultSender,
                TokenName = "Write Resource",
                TotalSupply = 100_0000L,
                LockWhiteList = { TokenContractAddress, TokenConverterContractAddress }
            });

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
            Connectors = { ELFConnector, WriteConnector, NtWriteConnector }
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
            Amount = 2000L
        })).TransactionResult;
        approveTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var approveRamTokenResult = (await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Spender = TokenConverterContractAddress,
            Symbol = "WRITE",
            Amount = 2000L
        })).TransactionResult;
        approveRamTokenResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    #endregion
}