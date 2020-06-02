using System.Threading.Tasks;
using Xunit;

namespace AElf.Contracts.AEDPoS
{
    public class MiningTest : AEDPoSContractTestBase
    {
        [Fact]
        public async Task Test()
        {
            await PackageConsensusTransactionAsync();
        }
    }
}