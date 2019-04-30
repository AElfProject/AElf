using System.Threading.Tasks;
using AElf.Cryptography;
using Xunit;

namespace AElf.Tester
{
    public class TesterTest
    {
        [Fact(Skip = "Not available")]
        public async Task Test()
        {
            var miner1 = new Tester(CryptoHelpers.GenerateKeyPair(), 7000, 7001, 7002);
            var miner2 = new Tester(CryptoHelpers.GenerateKeyPair(), 7001, 7000, 7002);
            var miner3 = new Tester(CryptoHelpers.GenerateKeyPair(), 7002, 7000, 7001);

            await miner1.StartAsync();

            var chain = miner1.Chain;

            Assert.NotNull(chain);
        }
    }
}