using System.Threading.Tasks;
using AElf.CrossChain.Indexing.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.CrossChain.Indexing;

public class IrreversibleBlockStateProviderTest : CrossChainTestBase
{
    private readonly CrossChainTestHelper _crossChainTestHelper;
    private readonly IIrreversibleBlockStateProvider _irreversibleBlockStateProvider;

    public IrreversibleBlockStateProviderTest()
    {
        _irreversibleBlockStateProvider = GetRequiredService<IIrreversibleBlockStateProvider>();
        _crossChainTestHelper = GetRequiredService<CrossChainTestHelper>();
    }

    [Fact]
    public async Task ValidateIrreversibleBlockExistingAsync_Test()
    {
        {
            var libExists = await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistingAsync();
            libExists.ShouldBeFalse();
        }

        _crossChainTestHelper.SetFakeLibHeight(1);
        {
            var libExists = await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistingAsync();
            libExists.ShouldBeFalse();
        }

        _crossChainTestHelper.SetFakeLibHeight(2);
        {
            var libExists = await _irreversibleBlockStateProvider.ValidateIrreversibleBlockExistingAsync();
            libExists.ShouldBeTrue();
        }
    }
}