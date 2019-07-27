using System.Threading.Tasks;
using Xunit;

namespace AElf.CrossChain.Communication
{
    public class CrossChainCommunicationEventHandlerTest : CrossChainCommunicationTestBase
    {
        private CrossChainCommunicationEventHandler _crossChainCommunicationEventHandler;

        public CrossChainCommunicationEventHandlerTest()
        {
            _crossChainCommunicationEventHandler = GetRequiredService<CrossChainCommunicationEventHandler>();
        }

        [Fact]
        public async Task HandleEventAsync()
        {
            var eventData = new CrossChainDataValidatedEvent();
            await _crossChainCommunicationEventHandler.HandleEventAsync(eventData);
        }
    }
}