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
            var chainId = Guid.NewGuid().ToString("N");
            new DeployService().DeploySideChain(chainId,arg);
        }
    }
}