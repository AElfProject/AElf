using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Kernel;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Nethereum.ABI.Decoders;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class NowContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task<Address> NowTest()
    {
        IBlockTimeProvider blockTimeProvider = Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();
        var timestamp = TimestampHelper.GetUtcNow();
        blockTimeProvider.SetBlockTime(timestamp);

        const string solFilePath = "contracts/now.sol";
        var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
        var contractAddress = executionResult.Output;
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "now");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var decoder = new IntTypeDecoder();
        var result = decoder.DecodeLong(txResult.ReturnValue.Reverse().ToArray());
        result.ShouldBe(timestamp.Seconds);
        return contractAddress;
    }
    
}