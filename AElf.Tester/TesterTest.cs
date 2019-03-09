using System.Threading.Tasks;
using AElf.Cryptography;
using Xunit;

namespace AElf.Tester
{
    public class TesterTest
    {
        [Fact]
        public async Task InitialChainTest()
        {
            var tester = new Tester();
            await tester.StartAsync();
            var chain = tester.Chain;

            Assert.NotNull(chain);
        }
    }
}