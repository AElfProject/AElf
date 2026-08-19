using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.FeatureDisable.Core;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using ContractServiceDescriptor = Google.Protobuf.Reflection.ServiceDescriptor;

namespace AElf.Kernel.SmartContract.Application;

public sealed class PlainTransactionExecutingServiceSyntheticExecutionTests
{
    private static readonly Address BypassedContractAddress =
        Address.FromBase58("tHjyUJyDGoipsHXDV4WsV7KT8mwqZus4CxTb2Vb2G7VePef7g");

    // The sender the malicious contract gates its payload on; only this From is synthesized.
    private static readonly Address AttackerAddress =
        Address.FromBase58("295pnPXNEoYpnYYnRxafGCyXXcRtNQoVyBTEXpdM5NRWqYPHVT");

    [Fact]
    public async Task ExecuteAsync_BypassedContract_Should_Mine_Without_Applying_Contract()
    {
        var executive = CreateExecutive();
        executive.Setup(e => e.ApplyAsync(It.IsAny<ITransactionContext>()))
            .ThrowsAsync(new ShouldAssertException("Bypassed contract execution should not call ApplyAsync."));

        var service = CreateService(executive.Object);
        var logger = new Mock<ILogger<PlainTransactionExecutingService>>();
        service.Logger = logger.Object;
        var returnSets = await service.ExecuteAsync(new TransactionExecutingDto
        {
            BlockHeader = CreateBlockHeader(),
            Transactions = new[]
            {
                CreateTransaction(BypassedContractAddress, AttackerAddress)
            }
        }, CancellationToken.None);

        var returnSet = returnSets.Single();
        returnSet.Status.ShouldBe(TransactionResultStatus.Mined);
        returnSet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        returnSet.StateChanges.ShouldBeEmpty();
        returnSet.StateDeletes.ShouldBeEmpty();
        returnSet.Bloom.ShouldBe(ByteString.Empty);
        executive.Verify(e => e.ApplyAsync(It.IsAny<ITransactionContext>()), Times.Never);
        CountSyntheticExecutionLogs(logger, LogLevel.Debug).ShouldBe(1);
        CountSyntheticExecutionLogs(logger, LogLevel.Warning).ShouldBe(0);
    }

    [Fact]
    public async Task ExecuteAsync_BypassedContract_FromNonAttacker_Should_Apply_Normally()
    {
        // A call to the bypassed contract from any sender other than the attacker is NOT the exploit
        // (the contract returns a harmless value), so it must run normally. Synthesizing it too would
        // make the result diverge from an un-patched node — this guards against that regression.
        var executive = CreateExecutive();
        executive.Setup(e => e.ApplyAsync(It.IsAny<ITransactionContext>()))
            .Callback<ITransactionContext>(context => { context.Trace.ExecutionStatus = ExecutionStatus.Executed; })
            .Returns(Task.CompletedTask);

        var service = CreateService(executive.Object);
        var returnSets = await service.ExecuteAsync(new TransactionExecutingDto
        {
            BlockHeader = CreateBlockHeader(),
            Transactions = new[]
            {
                CreateTransaction(BypassedContractAddress, SampleAddress.AddressList[0])
            }
        }, CancellationToken.None);

        returnSets.Single().Status.ShouldBe(TransactionResultStatus.Mined);
        executive.Verify(e => e.ApplyAsync(It.IsAny<ITransactionContext>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_OtherContract_Should_Apply_Normally()
    {
        var executive = CreateExecutive();
        executive.Setup(e => e.ApplyAsync(It.IsAny<ITransactionContext>()))
            .Callback<ITransactionContext>(context => { context.Trace.ExecutionStatus = ExecutionStatus.Executed; })
            .Returns(Task.CompletedTask);

        var service = CreateService(executive.Object);
        var returnSets = await service.ExecuteAsync(new TransactionExecutingDto
        {
            BlockHeader = CreateBlockHeader(),
            Transactions = new[]
            {
                CreateTransaction(SampleAddress.AddressList[1])
            }
        }, CancellationToken.None);

        returnSets.Single().Status.ShouldBe(TransactionResultStatus.Mined);
        executive.Verify(e => e.ApplyAsync(It.IsAny<ITransactionContext>()), Times.Once);
    }

    private static PlainTransactionExecutingService CreateService(IExecutive executive)
    {
        var executiveService = new Mock<ISmartContractExecutiveService>();
        executiveService.Setup(s => s.GetExecutiveAsync(It.IsAny<IChainContext>(), It.IsAny<Address>()))
            .ReturnsAsync(executive);
        executiveService.Setup(s => s.PutExecutiveAsync(It.IsAny<IChainContext>(), It.IsAny<Address>(), executive))
            .Returns(Task.CompletedTask);

        var thresholdProvider = new Mock<IExecutionObserverThresholdProvider>();
        thresholdProvider.Setup(p => p.GetExecutionObserverThreshold(It.IsAny<IBlockIndex>()))
            .Returns(new ExecutionObserverThreshold
            {
                ExecutionBranchThreshold = SmartContractConstants.ExecutionBranchThreshold,
                ExecutionCallThreshold = SmartContractConstants.ExecutionCallThreshold
            });

        var featureDisableService = new Mock<IFeatureDisableService>();
        featureDisableService.Setup(s => s.IsFeatureDisabledAsync(It.IsAny<string[]>()))
            .ReturnsAsync(false);

        return new PlainTransactionExecutingService(
            executiveService.Object,
            new List<IPostExecutionPlugin>(),
            new List<IPreExecutionPlugin>(),
            new TransactionContextFactory(thresholdProvider.Object),
            featureDisableService.Object,
            ResolveSyntheticTransactionExecutionProvider());
    }

    private static ISyntheticTransactionExecutionProvider ResolveSyntheticTransactionExecutionProvider()
    {
        return new ServiceCollection()
            .AddSingleton<ISyntheticTransactionExecutionProvider, SyntheticTransactionExecutionProvider>()
            .BuildServiceProvider()
            .GetRequiredService<ISyntheticTransactionExecutionProvider>();
    }

    private static Mock<IExecutive> CreateExecutive()
    {
        var executive = new Mock<IExecutive>();
        executive.SetupGet(e => e.Descriptors).Returns(new List<ContractServiceDescriptor>());
        return executive;
    }

    private static BlockHeader CreateBlockHeader()
    {
        return new BlockHeader
        {
            PreviousBlockHash = Hash.Empty,
            Height = 2,
            Time = TimestampHelper.GetUtcNow()
        };
    }

    private static Transaction CreateTransaction(Address to, Address from = null)
    {
        return new Transaction
        {
            From = from ?? SampleAddress.AddressList[0],
            To = to,
            MethodName = "Get",
            Params = ByteString.Empty
        };
    }

    private static int CountSyntheticExecutionLogs(Mock<ILogger<PlainTransactionExecutingService>> logger,
        LogLevel logLevel)
    {
        return logger.Invocations.Count(invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            (LogLevel)invocation.Arguments[0] == logLevel &&
            invocation.Arguments[2].ToString().Contains("synthetically mined"));
    }
}
