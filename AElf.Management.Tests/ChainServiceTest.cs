using System.Collections.Generic;
using AElf.Management.Models;
using AElf.Management.Services;
using Xunit;

namespace AElf.Management.Tests
{
    public class ChainServiceTest
    {
        private string chainId = "0x8dec57e833dcf10b977b2076654007feadfa";

        //[Fact(Skip = "require aws account")]
        [Fact]
        public void DeployTestChain()
        {
            var service = new ChainService();

            service.DeployTestChain();
        }

        //[Fact(Skip = "require aws account")]
        [Fact]
        public void RemoveTestChain()
        {
            var service = new ChainService();
            service.RemoveTestChain(chainId);
        }
    }
}