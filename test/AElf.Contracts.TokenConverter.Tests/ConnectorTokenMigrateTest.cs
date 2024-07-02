using System;
using System.Threading.Tasks;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.TokenConverter;

public partial class TokenConverterContractTests
{
    [Fact]
    public async Task CanBuyResourceTokenAfterMigration()
    {
        await CreateWriteToken();
        await InitializeTreasuryContractAsync();
        await InitializeTokenConverterContract();
        await PrepareToBuyAndSell();

        await DefaultStub.MigrateConnectorTokens.SendAsync(new Empty());

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

        var elfBalanceLoggedInTokenConvert = await DefaultStub.GetDepositConnectorBalance.CallAsync(new StringValue
        {
            Value = WriteConnector.Symbol
        });
        elfBalanceLoggedInTokenConvert.Value.ShouldBe(ELFConnector.VirtualBalance + amountToPay);
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
    public async Task CanSellResourceTokenAfterMigration()
    {
        await CreateWriteToken();
        await InitializeTreasuryContractAsync();
        await InitializeTokenConverterContract();
        await PrepareToBuyAndSell();

        await DefaultStub.MigrateConnectorTokens.SendAsync(new Empty());

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
    public async Task MigrateTwiceTest()
    {
        await CreateWriteToken();
        await InitializeTreasuryContractAsync();
        await InitializeTokenConverterContract();
        await PrepareToBuyAndSell();

        await DefaultStub.MigrateConnectorTokens.SendAsync(new Empty());
        var result = await DefaultStub.MigrateConnectorTokens.SendWithExceptionAsync(new Empty());
        result.TransactionResult.Error.ShouldContain("Already migrated.");
    }
}