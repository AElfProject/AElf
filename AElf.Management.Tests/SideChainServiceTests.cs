using System;
using System.Runtime.InteropServices.ComTypes;
using AElf.Management.Models;
using Xunit;

namespace AElf.Management.Tests
{
    public class SideChainServiceTests
    {
        private string _chainId = "ed7d50f2a4b94d9b9e7ec6ec6935e14e";//Guid.NewGuid().ToString("N");
        
        //[Fact(Skip = "require aws account")]
        [Fact]
        public void DeployTest()
        {
            var arg = new DeployArg();
            arg.MainChainAccount = "0x04b8b111fdbc2f5409a006339fa1758e1ed1";
            arg.AccountPassword = "123";
            arg.DBArg = new DeployDBArg();
            arg.WorkArg = new DeployWorkArg();
            arg.WorkArg.ActorCount = 1;

            var service = new SideChainService();
            service.Deploy(_chainId, arg);
        }
        
        //[Fact(Skip = "require aws account")]
        [Fact]
        public void RemoveTest()
        {
            var service = new SideChainService();

            service.Remove(_chainId);
        }
    }
}