using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.FeeCalculation.Infrastructure;

public class CalculateFunctionTest
{
    private readonly CalculateFunction _calculateFunction;

    public CalculateFunctionTest()
    {
        var feeType = 1;
        _calculateFunction = new CalculateFunction(feeType);
    }


    [Theory]
    [InlineData(10, 100, 1000, 5, 5)] // 5
    [InlineData(10, 100, 1000, 10, 10)] // 10
    [InlineData(10, 100, 1000, 50, 4010)] // 10 + 40 * 100
    [InlineData(10, 100, 1000, 100, 9010)] // 10 + （100 - 10）* 100
    [InlineData(10, 100, 1000, 500, 409010)] // 10 + （100 - 10）* 100 +（500 -100）* 1000
    [InlineData(10, 100, 1000, 1000, 909010)] //10 + （100 - 10）* 100 +（1000 -100）* 1000
    [InlineData(10, 100, 1000, 1001, 909010)] //10 + （100 - 10）* 100 +（1000 -100）* 1000
    public void CalculateFunction_Piece_Wise_Test(int piece1, int piece2, int piece3, int input, long outCome)
    {
        _calculateFunction.AddFunction(new[] { piece1 }, Calculate1);
        _calculateFunction.AddFunction(new[] { piece2 }, Calculate2);
        _calculateFunction.AddFunction(new[] { piece3 }, Calculate3);

        var calculateOutcome = _calculateFunction.CalculateFee(input);
        calculateOutcome.ShouldBe(outCome);
    }

    [Fact]
    public void CalculateFunction_With_Miss_Match_Functions_Test()
    {
        _calculateFunction.AddFunction(new[] { 1 }, Calculate1);
        _calculateFunction.AddFunction(new[] { 10 }, Calculate2);
        _calculateFunction.AddFunction(new[] { 100 }, Calculate3);
        _calculateFunction.CalculateFeeCoefficients.PieceCoefficientsList.RemoveAt(0);
        string errorMsg = null;
        try
        {
            _calculateFunction.CalculateFee(1000);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            errorMsg = ex.Message;
        }

        errorMsg.ShouldContain("Coefficients count not match");
    }

    [Fact]
    public void GetChargedTransactionFees_Test()
    {
        var transactionResult = new TransactionResult();
        transactionResult.Logs.Add(new TransactionFeeCharged
        {
            ChargingAddress = SampleAddress.AddressList[0],
            Amount = 1,
            Symbol = "ELF"
        }.ToLogEvent());
        transactionResult.Logs.Add(new TransactionFeeCharged
        {
            ChargingAddress = SampleAddress.AddressList[0],
            Amount = 2,
            Symbol = "ELF"
        }.ToLogEvent());
        transactionResult.Logs.Add(new TransactionFeeCharged
        {
            ChargingAddress = SampleAddress.AddressList[0],
            Amount = 3,
            Symbol = "USDT"
        }.ToLogEvent());
        transactionResult.Logs.Add(new TransactionFeeCharged
        {
            ChargingAddress = SampleAddress.AddressList[0],
            Amount = 4,
            Symbol = "USDT"
        }.ToLogEvent());
        transactionResult.Logs.Add(new TransactionFeeCharged
        {
            ChargingAddress = SampleAddress.AddressList[1],
            Amount = 3,
            Symbol = "TEST"
        }.ToLogEvent());
        transactionResult.Logs.Add(new TransactionFeeCharged
        {
            ChargingAddress = SampleAddress.AddressList[1],
            Amount = 4,
            Symbol = "TEST"
        }.ToLogEvent());
        var feeDic = transactionResult.GetChargedTransactionFees();
        feeDic.Count.ShouldBe(2);
        feeDic.Keys.First().ShouldBe(SampleAddress.AddressList[0]);
        feeDic.Values.First()["ELF"].ShouldBe(3);   
        feeDic.Values.First()["USDT"].ShouldBe(7);
        feeDic.Keys.Last().ShouldBe(SampleAddress.AddressList[1]);
        feeDic.Values.Last()["TEST"].ShouldBe(7);
    }

    [Fact]
    public void GetConsumedResourceTokens_Test()
    {
        var transactionResult = new TransactionResult();
        var resourceTokenFeeDic = transactionResult.GetConsumedResourceTokens();
        resourceTokenFeeDic.Count.ShouldBe(0);
    }

    [Fact]
    public void GetOwningResourceTokens_Test()
    {
        {
            var transactionResult = new TransactionResult();
            var owningTokenFeeDic = transactionResult.GetOwningResourceTokens();
            owningTokenFeeDic.Count.ShouldBe(0);
        }

        {
            var symbol = "ELF";
            var amount = 100;
            var transactionResult = new TransactionResult();
            transactionResult.Logs.Add(new LogEvent
            {
                Name = "ResourceTokenOwned",
                NonIndexed = new ResourceTokenOwned
                {
                    Symbol = symbol,
                    Amount = amount
                }.ToByteString()
            });
            var owningTokenFeeDic = transactionResult.GetOwningResourceTokens();
            owningTokenFeeDic.Count.ShouldBe(1);
            owningTokenFeeDic[symbol].ShouldBe(amount);
        }
    }

    private long Calculate1(int count)
    {
        return count;
    }

    private long Calculate2(int count)
    {
        return (long)count * 100;
    }

    private long Calculate3(int count)
    {
        return (long)count * 1000;
    }
}