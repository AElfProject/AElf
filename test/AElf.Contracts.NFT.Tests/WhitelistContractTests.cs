using System.Threading.Tasks;
using AElf.Contracts.Whitelist;
using Xunit;

namespace AElf.Contracts.NFT
{
    public class WhitelistContractTests : NFTContractTestBase
    {
        [Fact]
        public async Task InitializeTest()
        {
            await WhitelistContractStub.Initialize.SendAsync(new InitializeInput());
        }
    }
}