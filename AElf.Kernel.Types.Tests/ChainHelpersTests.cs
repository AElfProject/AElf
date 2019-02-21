using System;
using System.Collections.Generic;
using System.Threading;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;
using AElf.Common;
using Shouldly;

namespace AElf.Kernel.Types.Tests
{
    public class ChainHelpersTests
    {
        [Fact]
        public void Create_Random_ChainId()
        {
            var chainId = ChainHelpers.GetRandomChainId();
            chainId.ShouldBeGreaterThan(198535);
            chainId.ShouldBeLessThan(11316496);
        }

        [Fact]
        public void GetChainId_By_SerialNumber()
        {
            var chainId = ChainHelpers.GetChainId(ContractConsts.AuthorizationContract);
            chainId.ShouldBeGreaterThan(198535);
            chainId.ShouldBeLessThan(11316496);
        }

        [Fact]
        public void Base58_Dump_And_Convert()
        {
            var chainId = ChainHelpers.GetRandomChainId();
            var dumpStr = chainId.DumpBase58();
            dumpStr.ShouldNotBe(string.Empty);

            var chainId1 = dumpStr.ConvertBase58ToChainId();
            chainId1.ShouldBe(chainId);
        }
    }
}