using Shouldly;
using Xunit;

namespace AElf.Types.Tests.Helper
{
    public class ChainHelperTests
    {
        [Fact]
        public void TestChainIdGeneration()
        {
            {
                var chainId = ChainHelper.GetChainId(0);
                var chainIdBased58 = ChainHelper.ConvertChainIdToBase58(chainId);
                chainIdBased58.ShouldBe("2111");

                var convertedChainId = ChainHelper.ConvertBase58ToChainId(chainIdBased58);
                convertedChainId.ShouldBe(chainId);
            }
            
            {
                var chainId = ChainHelper.GetChainId(1);
                var chainIdBased58 = ChainHelper.ConvertChainIdToBase58(chainId);
                chainIdBased58.ShouldBe("2112");
                
                var convertedChainId = ChainHelper.ConvertBase58ToChainId(chainIdBased58);
                convertedChainId.ShouldBe(chainId);
            }
            
            {
                var chainIdMaxValue = ChainHelper.GetChainId(long.MaxValue);
                var chainIdBased58MaxValue = ChainHelper.ConvertChainIdToBase58(chainIdMaxValue);
                chainIdBased58MaxValue.ShouldBe("mR59");

                var convertedChainIdMaxValue = ChainHelper.ConvertBase58ToChainId(chainIdBased58MaxValue);
                convertedChainIdMaxValue.ShouldBe(chainIdMaxValue);
                
                var chainIdMinValue = ChainHelper.GetChainId(long.MinValue);
                chainIdMinValue.ShouldBe(chainIdMaxValue);
                var chainIdBased58MinValue = ChainHelper.ConvertChainIdToBase58(chainIdMaxValue);
                chainIdBased58MinValue.ShouldBe(chainIdBased58MaxValue);
                var convertedChainIdMinValue = ChainHelper.ConvertBase58ToChainId(chainIdBased58MinValue);
                convertedChainIdMinValue.ShouldBe(convertedChainIdMaxValue);
            }

            {
                var chainIdAElf = ChainHelper.ConvertBase58ToChainId("AELF");
                var chainId = ChainHelper.GetChainId(chainIdAElf + 1);
                var chainIdBased58 = ChainHelper.ConvertChainIdToBase58(chainId);
                chainIdBased58.ShouldBe("tDVV");
            }
        }
    }
}