using System.Threading.Tasks;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace AElf.Management.Tests
{
    public class SideChainServiceTests : ManagementTestBase
    {
        private string _chainId = "kPBx"; //Guid.NewGuid().ToString("N");
        private readonly ISideChainService _sideChainService;

        public SideChainServiceTests()
        {
            _sideChainService = GetRequiredService<ISideChainService>();
        }

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

            await _sideChainService.Deploy(arg);
        }

        [Fact(Skip = "require aws account")]
        //[Fact]
        public async Task RemoveTest()
        {
            await _sideChainService.Remove(_chainId);
        }
    }
}