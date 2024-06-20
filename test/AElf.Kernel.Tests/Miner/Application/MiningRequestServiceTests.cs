using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Miner.Application;

public class MiningRequestServiceTests : KernelMiningTestBase
{
    private readonly IBlockchainService _blockchainService;
    private readonly IMiningRequestService _miningRequestService;

    public MiningRequestServiceTests()
    {
        _miningRequestService = GetRequiredService<IMiningRequestService>();
        _blockchainService = GetRequiredService<IBlockchainService>();
    }
}