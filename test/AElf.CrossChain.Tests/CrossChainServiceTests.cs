using System;
using System.Threading.Tasks;
using AElf.CrossChain.Application;
using AElf.CrossChain.Cache.Application;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.CrossChain
{
    public class CrossChainServiceTest : CrossChainTestBase
    {
        private readonly ICrossChainService _crossChainService;
        private readonly CrossChainTestHelper _crossChainTestHelper;
        private readonly ICrossChainCacheEntityService _crossChainCacheEntityService;
        private readonly CrossChainConfigOptions _crossChainConfigOptions;
    
        public CrossChainServiceTest()
        {
            _crossChainService = GetRequiredService<ICrossChainService>();
            _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
            _crossChainCacheEntityService = GetRequiredService<ICrossChainCacheEntityService>();
            _crossChainConfigOptions = GetRequiredService<IOptionsMonitor<CrossChainConfigOptions>>().CurrentValue;
        }
    
        [Fact]
        public async Task FinishInitialSync_Test()
        {
            int chainId = ChainHelper.ConvertBase58ToChainId("AELF");
            long libHeight = 10;
            _crossChainTestHelper.AddFakeChainIdHeight(chainId, libHeight);
            _crossChainConfigOptions.CrossChainDataValidationIgnored.ShouldBeTrue();
            await _crossChainService.FinishInitialSyncAsync();
            _crossChainConfigOptions.CrossChainDataValidationIgnored.ShouldBeFalse();
            
            var height = _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId);
            {
                Should.Throw<InvalidOperationException>(() =>
                    _crossChainCacheEntityService.GetTargetHeightForChainCacheEntity(chainId - 1));
            }
            Assert.Equal(libHeight + 1, height);
        }
    }
}