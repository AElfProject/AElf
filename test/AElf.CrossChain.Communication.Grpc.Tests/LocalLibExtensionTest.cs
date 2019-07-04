using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Modularity;
using AElf.TestBase;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Shouldly;
using Volo.Abp.Modularity;
using Xunit;

namespace AElf.CrossChain.Communication.Grpc
{
    public class CrossChainLibExtensionModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var dictionary = new Dictionary<long, Hash>
            {
                {1, Hash.FromString("1")},
                {2, Hash.FromString("2")},
                {3, Hash.FromString("3")}
            };
            context.Services.AddTransient(provider =>
            {
                var mockBlockChainService = new Mock<IBlockchainService>();
                mockBlockChainService.Setup(m => m.GetChainAsync()).Returns(Task.FromResult(new Chain
                {
                    LastIrreversibleBlockHeight = CrossChainConstants.LibHeightOffsetForCrossChainIndex + 1
                }));
                mockBlockChainService.Setup(m => m.GetBlockHashByHeightAsync(It.IsAny<Chain>(), It.IsAny<long>(), It.IsAny<Hash>()))
                    .Returns<Chain, long, Hash>((chain, height, hash) =>
                    {
                        if (height > 0 && height <= 3)
                            return Task.FromResult(dictionary[height]);
                        return Task.FromResult<Hash>(null);
                    });
                mockBlockChainService.Setup(m => m.GetBlockByHashAsync(It.IsAny<Hash>())).Returns<Hash>(hash =>
                {
                    foreach (var kv in dictionary)
                    {
                        if (kv.Value.Equals(hash))
                            return Task.FromResult(new Block {Header = new BlockHeader {Height = kv.Key}});
                    }
                    
                    return Task.FromResult<Block>(null);
                });
                return mockBlockChainService.Object;
            });
        }
    }
    
    public sealed class LocalLibExtensionTest : AElfIntegratedTest<CrossChainLibExtensionModule>
    {
        private readonly IBlockchainService _blockchainService;
        
        public LocalLibExtensionTest()
        {
            _blockchainService = GetService<IBlockchainService>();
        }
        
        [Fact]
        public async Task GetLibHeight_Test()
        {
            var lastIrreversibleBlockDto = await _blockchainService.GetLibHashAndHeightAsync();
            lastIrreversibleBlockDto.BlockHeight.ShouldBe(CrossChainConstants.LibHeightOffsetForCrossChainIndex + 1);
        }

        [Fact]
        public async Task GetIrreversibleBlockByHeight_Test()
        {
            var height = 2;
            var irreversibleBlock = await _blockchainService.GetIrreversibleBlockByHeightAsync(height);
            irreversibleBlock.ShouldBeNull();

            height = 1;
            irreversibleBlock = await _blockchainService.GetIrreversibleBlockByHeightAsync(height);
            var expectedBlock = new Block {Header = new BlockHeader {Height = height}};
            irreversibleBlock.Equals(expectedBlock).ShouldBeTrue();
        }
    }
}