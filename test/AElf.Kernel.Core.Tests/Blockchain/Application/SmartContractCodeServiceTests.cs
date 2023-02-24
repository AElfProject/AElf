using AElf.Kernel.Blockchain.Domain;

namespace AElf.Kernel.Blockchain.Application;

public class SmartContractCodeServiceTests : AElfKernelWithChainTestBase
{
    private readonly ISmartContractCodeService _smartContractCodeService;

    public SmartContractCodeServiceTests()
    {
        _smartContractCodeService = GetRequiredService<ISmartContractCodeService>();
    }

    [Fact]
    public async Task SmartContractCode_Test()
    {
        var code = new byte[10];
        var codeHash = HashHelper.ComputeFrom(code);
        var contractCode = await _smartContractCodeService.GetSmartContractCodeAsync(codeHash);
        contractCode.ShouldBeNull();

        await _smartContractCodeService.AddSmartContractCodeAsync(codeHash, ByteString.CopyFrom(code));
        contractCode = await _smartContractCodeService.GetSmartContractCodeAsync(codeHash);
        contractCode.ShouldBe(ByteString.CopyFrom(code));
    }
}