using System.Collections.Generic;
using System.Threading.Tasks;
using Acs3;
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
    }
}
