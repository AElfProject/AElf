using System;
using AElf.Deployment.Models;
using Xunit;

namespace AElf.Deployment.Tests
{
    public class DeployServiceTests
    {
        [Fact]
        public void DeploySideChainTest()
        {
            var arg = new DeployArg();
            arg.MainChainAccount = "0x04b8b111fdbc2f5409a006339fa1758e1ed1";
            arg.AccountPassword = "123";
            arg.DBArg=new DeployDBArg();
            arg.WorkArg=new DeployWorkArg();
            
            var chainId = "ed7d50f2a4b94d9b9e7ec6ec6935e14e";//Guid.NewGuid().ToString("N");
            
            new SideChainService().Deploy(chainId,arg);
        }
        
        [Fact]
        public void RemoveSideChainTest()
        {
            var chainId = "ed7d50f2a4b94d9b9e7ec6ec6935e14e";//Guid.NewGuid().ToString("N");
            new SideChainService().Remove(chainId);
        }
    }
}