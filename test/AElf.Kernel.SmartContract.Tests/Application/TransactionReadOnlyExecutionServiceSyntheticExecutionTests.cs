using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;
using Xunit;
using ContractServiceDescriptor = Google.Protobuf.Reflection.ServiceDescriptor;

namespace AElf.Kernel.SmartContract.Application;

public sealed class TransactionReadOnlyExecutionServiceSyntheticExecutionTests
{
    private static readonly Address BypassedContractAddress =
        Address.FromBase58("tHjyUJyDGoipsHXDV4WsV7KT8mwqZus4CxTb2Vb2G7VePef7g");

    // The sender the malicious contract gates its payload on; only this From is synthesized.
    private static readonly Address AttackerAddress =
        Address.FromBase58("295pnPXNEoYpnYYnRxafGCyXXcRtNQoVyBTEXpdM5NRWqYPHVT");

    [Fact]
    public async Task ExecuteAsync_BypassedContract_Should_Not_Apply_Contract()
    {
        var executive = new Mock<IExecutive>();
        executive.SetupGet(e => e.Descriptors).Returns(new List<ContractServiceDescriptor>());
        executive.Setup(e => e.ApplyAsync(It.IsAny<ITransactionContext>()))
            .ThrowsAsync(new ShouldAssertException("Bypassed read-only execution should not call ApplyAsync."));

        var executiveService = new Mock<ISmartContractExecutiveService>();
        executiveService.Setup(s => s.GetExecutiveAsync(It.IsAny<IChainContext>(), BypassedContractAddress))
            .ReturnsAsync(executive.Object);
        executiveService.Setup(s =>
                s.PutExecutiveAsync(It.IsAny<IChainContext>(), BypassedContractAddress, executive.Object))
            .Returns(Task.CompletedTask);

        var thresholdProvider = new Mock<IExecutionObserverThresholdProvider>();
        thresholdProvider.Setup(p => p.GetExecutionObserverThreshold(It.IsAny<IBlockIndex>()))
            .Returns(new ExecutionObserverThreshold
            {
                ExecutionBranchThreshold = SmartContractConstants.ExecutionBranchThreshold,
                ExecutionCallThreshold = SmartContractConstants.ExecutionCallThreshold
            });

        var service = new TransactionReadOnlyExecutionService(
            executiveService.Object,
            new TransactionContextFactory(thresholdProvider.Object),
            ResolveSyntheticTransactionExecutionProvider());
        var logger = new Mock<ILogger<TransactionReadOnlyExecutionService>>();
        service.Logger = logger.Object;

        var trace = await service.ExecuteAsync(new ChainContext
        {
            BlockHash = Hash.Empty,
            BlockHeight = 1
        }, new Transaction
        {
            From = AttackerAddress,
            To = BypassedContractAddress,
            MethodName = "Get",
            Params = ByteString.Empty
        }, TimestampHelper.GetUtcNow());

        trace.ExecutionStatus.ShouldBe(ExecutionStatus.Executed);
        executive.Verify(e => e.ApplyAsync(It.IsAny<ITransactionContext>()), Times.Never);
        CountSyntheticExecutionLogs(logger).ShouldBe(1);
    }

    private static ISyntheticTransactionExecutionProvider ResolveSyntheticTransactionExecutionProvider()
    {
        return new ServiceCollection()
            .AddSingleton<ISyntheticTransactionExecutionProvider, SyntheticTransactionExecutionProvider>()
            .BuildServiceProvider()
            .GetRequiredService<ISyntheticTransactionExecutionProvider>();
    }

    private static int CountSyntheticExecutionLogs(Mock<ILogger<TransactionReadOnlyExecutionService>> logger)
    {
        return logger.Invocations.Count(invocation =>
            invocation.Method.Name == nameof(ILogger.Log) &&
            (LogLevel)invocation.Arguments[0] == LogLevel.Debug &&
            invocation.Arguments[2].ToString().Contains("synthetically executed"));
    }
}
