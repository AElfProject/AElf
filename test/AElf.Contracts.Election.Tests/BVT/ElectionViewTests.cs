using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public partial class ElectionContractTests : ElectionContractTestBase
    {
        [Fact]
        public async Task GetMinersCount()
        {
            await ElectionContract_AnnounceElection();

            var minersCount = await ElectionContractStub.GetMinersCount.CallAsync(new Empty());
            minersCount.Value.ShouldBe(InitialMinersCount);
        }

        [Fact]
        public async Task GetElectionResult()
        {
            await ElectionContract_AnnounceElection();
            
        }
        
    }
}