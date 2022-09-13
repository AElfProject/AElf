using AElf.Kernel.Miner.Application;

namespace AElf.Kernel.Miner;

[Trait("Category", AElfBlockchainModule)]
public sealed class SystemTransactionValidationProviderTests : AElfKernelWithChainTestBase
{
    private readonly ISystemTransactionExtraDataProvider _systemTransactionExtraDataProvider;
    private readonly SystemTransactionValidationProvider _systemTransactionValidationProvider;

    public SystemTransactionValidationProviderTests()
    {
        _systemTransactionValidationProvider = GetRequiredService<SystemTransactionValidationProvider>();
        _systemTransactionExtraDataProvider = GetRequiredService<ISystemTransactionExtraDataProvider>();
    }

    [Fact]
    public async Task ValidateBeforeAttach_WithoutExtraData_Test()
    {
        var block = new Block
        {
            Header = new BlockHeader()
        };
        var validateResult = await _systemTransactionValidationProvider.ValidateBeforeAttachAsync(block);
        validateResult.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateBeforeAttach_WithExtraData_False_Test()
    {
        var block = new Block
        {
            Header = new BlockHeader()
        };

        _systemTransactionExtraDataProvider.SetSystemTransactionCount(0, block.Header);
        var validateResult = await _systemTransactionValidationProvider.ValidateBeforeAttachAsync(block);
        validateResult.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateBeforeAttach_WithExtraData_True_Test()
    {
        var block = new Block
        {
            Header = new BlockHeader()
        };

        _systemTransactionExtraDataProvider.SetSystemTransactionCount(1, block.Header);
        var validateResult = await _systemTransactionValidationProvider.ValidateBeforeAttachAsync(block);
        validateResult.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBlockBeforeExecute_Test()
    {
        var block = new Block
        {
            Header = new BlockHeader()
        };
        var validateResult = await _systemTransactionValidationProvider.ValidateBlockBeforeExecuteAsync(block);
        validateResult.ShouldBeTrue();
    }

    [Fact]
    public async Task ValidateBlockAfterExecuteTest()
    {
        var block = new Block
        {
            Header = new BlockHeader()
        };
        var validateResult = await _systemTransactionValidationProvider.ValidateBlockAfterExecuteAsync(block);
        validateResult.ShouldBeTrue();
    }
}