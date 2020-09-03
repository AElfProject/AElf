using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Standards.ACS3;
using AElf.Kernel.Proposal.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.CodeCheck.Tests
{
    public class LogEventProcessorTest : CodeCheckTestBase
    {
        private readonly ILogEventProcessor _logEventProcessor;
        private readonly IProposalProvider _proposalProvider;

        public LogEventProcessorTest()
        {
            _logEventProcessor = GetRequiredService<ILogEventProcessor>();
            _proposalProvider = GetRequiredService<IProposalProvider>();
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
                Logs = { new LogEvent
                {
                    Name = "ProposalCreated",
                    NonIndexed = proposalCreated.ToByteString()
                }}
            };
            var logEventsMap = new Dictionary<TransactionResult, List<LogEvent>>();
            
            // use default auditor
            logEventsMap[transactionResult] = new List<LogEvent>{new LogEvent()};
            await _logEventProcessor.ProcessAsync(new Block(), logEventsMap);
        }
    }
}