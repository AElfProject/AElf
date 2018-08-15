using System;
using AElf.Management.Models;
using Xunit;

namespace AElf.Management.Tests
{
    public class SideChainServiceTests
    {
        [Fact(Skip = "require aws account")]
        public void DeployTest()
        {
            var arg = new DeployArg();
            arg.MainChainAccount = "0x04b8b111fdbc2f5409a006339fa1758e1ed1";
            arg.AccountPassword = "123";
            arg.DBArg = new DeployDBArg();
            arg.WorkArg = new DeployWorkArg();

            var chainId = Guid.NewGuid().ToString("N");

            var service = new SideChainService();
            service.Deploy(chainId, arg);

            service.Remove(chainId);
        }
    }
}