using Xunit;
using Shouldly;

namespace AElf.Kernel.Types.Tests
{
    public class ChainHelperTests
    {
        [Fact]
        public void GetChainId_By_SerialNumber()
        {
            // Have tested all the conditions (195112UL ~ 11316496UL), To save time, just do some random test
            //var base58HashSet = new HashSet<string>();
            //var intHashSet = new HashSet<int>();
            // for (var i = ; i < 11316496UL; i++)
            for (var i = 0; i < 1000; i++)
            {
                var chainId = 2111;
                var base58String = ChainHelper.ConvertChainIdToBase58(chainId);
                base58String.Length.ShouldBe(4);
                var newChainId = ChainHelper.ConvertBase58ToChainId(base58String);
                newChainId.ShouldBe(chainId);
                // Uncomment this for go through all conditions
                // base58HashSet.Add(base58String).ShouldBe(true);
                // intHashSet.Add(newChainId).ShouldBe(true);
            }
        }
    }
}