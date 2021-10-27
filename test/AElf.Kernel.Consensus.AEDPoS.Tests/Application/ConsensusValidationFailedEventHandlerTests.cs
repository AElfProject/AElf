using System.Threading.Tasks;
using AElf.Kernel.Consensus.AEDPoS.Application;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class ConsensusValidationFailedEventHandlerTests : AEDPoSTestBase
    {
        private readonly ConsensusValidationFailedEventHandler _consensusValidationFailedEventHandler;

        public ConsensusValidationFailedEventHandlerTests()
        {
            _consensusValidationFailedEventHandler = GetRequiredService<ConsensusValidationFailedEventHandler>();
        }

        [Fact]
        public async Task HandleEventAsync_Test()
        {
            var eventData = new ConsensusValidationFailedEventData()
            {
                ValidationResultMessage = "Time slot already passed before execution."
            };
            await _consensusValidationFailedEventHandler.HandleEventAsync(eventData);

            eventData.ValidationResultMessage = "other message";
            await _consensusValidationFailedEventHandler.HandleEventAsync(eventData);
        }
    }
}