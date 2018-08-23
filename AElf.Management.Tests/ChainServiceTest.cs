using System.Collections.Generic;
using AElf.Management.Models;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class ChainServiceTest
    {
        private string chainId = "0x7a9f33c7cfaf7c8bd08f9decb0c286890639";

        //[Fact(Skip = "require aws account")]
        [Fact]
        public void DeployTestChain()
        {
            var service = new ChainService();

            service.DeployTestChain();
        }

        [Fact]
        public void RemoveTestChain()
        {
            var service = new ChainService();
            var removeChainIds = new List<string>
            {
                chainId+"-1",
                chainId+"-2",
                chainId+"-3"
            };

            foreach (var chainId in removeChainIds)
            {
                service.RemoveMainChain(chainId);
            }
        }
    }
}