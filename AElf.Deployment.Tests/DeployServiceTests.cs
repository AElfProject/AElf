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
            var chainId = "ed7d50f2a4b94d9b9e7ec6ec6935e14e";//Guid.NewGuid().ToString("N");
            new SideChainService().Deploy(chainId,arg);
        }
    }
}