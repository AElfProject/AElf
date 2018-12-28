using System.Threading.Tasks;
using AElf.Management.Models;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class ChainServiceTest
    {
        private string chainId = "0xcce1f3b8d6df42ba73050ba12244fa7fe415";

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

            var service = new ChainService();
            await service.DeployMainChain(arg);
        }

        [Fact(Skip = "require aws account")]
        //[Fact]
        public async Task RemoveTestChainTest()
        {
            var service = new ChainService();
            await service.RemoveMainChain(chainId);
        }
    }
}