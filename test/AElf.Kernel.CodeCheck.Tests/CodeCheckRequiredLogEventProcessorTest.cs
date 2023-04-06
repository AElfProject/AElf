using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.CodeCheck.Tests;

public class LogEventProcessorTest : CodeCheckTestBase
{
    private readonly ILogEventProcessor _logEventProcessor;

    public LogEventProcessorTest()
    {
        _logEventProcessor = GetRequiredService<ILogEventProcessor>();
    }

    [Fact]
    public async Task GetInterestedEventAsync_Test()
    {
        var interestedEvent = await _logEventProcessor.GetInterestedEventAsync(new ChainContext());
        interestedEvent.ShouldNotBeNull();
        interestedEvent.LogEvent.Name.ShouldContain("CodeCheckRequired");
    }

    [Fact]
    public async Task ProcessAsync_Test()
    {
        var proposalCreated = new ProposalCreated
        {
            ProposalId = HashHelper.ComputeFrom("Test")
        };
        var transactionResult = new TransactionResult
        {
            Logs =
            {
                new LogEvent
                {
                    Name = "ProposalCreated",
                    NonIndexed = proposalCreated.ToByteString()
                }
            }
        };
        var logEventsMap = new Dictionary<TransactionResult, List<LogEvent>>();
        
        var block = new Block
        {
            Header = new BlockHeader
            {
                Height = 100,
                PreviousBlockHash = HashHelper.ComputeFrom("PreviousBlockHash"),
                MerkleTreeRootOfTransactions = HashHelper.ComputeFrom("MerkleTreeRootOfTransactions"),
                MerkleTreeRootOfWorldState = HashHelper.ComputeFrom("MerkleTreeRootOfWorldState"),
                MerkleTreeRootOfTransactionStatus = HashHelper.ComputeFrom("MerkleTreeRootOfTransactionStatus"),
                Time = TimestampHelper.GetUtcNow(),
                SignerPubkey = ByteString.CopyFromUtf8("SignerPubkey")
            }
        };

        // use default auditor
        logEventsMap[transactionResult] = new List<LogEvent>
        {
            new CodeCheckRequired
            {
                Category = 0,
                Code = ByteString.Empty,
                IsSystemContract = false,
                IsUserContract = true,
                ProposedContractInputHash = HashHelper.ComputeFrom(""),
            }.ToLogEvent(ZeroContractFakeAddress)
        };
        await _logEventProcessor.ProcessAsync(block, logEventsMap);
    }
}