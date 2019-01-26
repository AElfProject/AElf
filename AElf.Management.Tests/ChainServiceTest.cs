using System.Threading.Tasks;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class ChainServiceTest : ManagementTestBase
    {
        private string chainId = "AElf";
        private readonly IChainService _chainService;

        public ChainServiceTest()
        {
            _chainService = GetRequiredService<IChainService>();
        }

        [Fact(Skip = "require aws account")]
        //[Fact]
        public async Task DeployTestChainTest()
        {
            var arg = new DeployArg();
            arg.AccountPassword = "123";
            arg.DBArg = new DeployDBArg();
            arg.LighthouseArg = new DeployLighthouseArg();
            arg.LighthouseArg.IsCluster = false;
            arg.WorkArg = new DeployWorkArg();
            arg.LauncherArg = new DeployLauncherArg();
            arg.LauncherArg.IsConsensusInfoGenerator = true;

            await _chainService.DeployMainChain(arg);
        }

        [Fact(Skip = "require aws account")]
        //[Fact]
        public async Task RemoveTestChainTest()
        {
            await _chainService.RemoveMainChain(chainId);
        }
    }
}