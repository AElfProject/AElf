using System.Collections.Generic;
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
        public void DeployTestChain()
        {
            var service = new ChainService();

            service.DeployTestChain();
        }

        [Fact(Skip = "require aws account")]
        //[Fact]
        public void RemoveTestChain()
        {
            var service = new ChainService();
            service.RemoveTestChain(chainId);
        }
    }
}