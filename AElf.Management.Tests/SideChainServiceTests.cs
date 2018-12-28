using System.Threading.Tasks;
using AElf.Management.Models;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class SideChainServiceTests
    {
        private string _chainId = "kPBx"; //Guid.NewGuid().ToString("N");

        [Fact(Skip = "require aws account")]
        //[Fact]
        public async Task DeployTest()
        {
            var password = "123";

            var arg = new DeployArg();
            arg.MainChainId = _chainId;
            arg.AccountPassword = password;
            arg.DBArg = new DeployDBArg();
            arg.LighthouseArg = new DeployLighthouseArg();
            arg.LighthouseArg.IsCluster = false;
            arg.WorkArg = new DeployWorkArg();
            arg.LauncherArg = new DeployLauncherArg();
            arg.LauncherArg.IsConsensusInfoGenerator = true;

            var service = new SideChainService();
            await service.Deploy(arg);
        }

        [Fact(Skip = "require aws account")]
        //[Fact]
        public async Task RemoveTest()
        {
            var service = new SideChainService();
            await service.Remove(_chainId);
        }
    }
}