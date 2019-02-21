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
        //TODO: the Create_Random_ChainId sometimes fails
        /*
        [Fact]
        public void Create_Random_ChainId()
        {
            var chainId = ChainHelpers.GetRandomChainId();
            chainId.ShouldBeGreaterThan(198535);
            chainId.ShouldBeLessThan(11316496);
        }*/

        [Fact]
        public void GetChainId_By_SerialNumber()
        {
            var chainId = ChainHelpers.GetChainId(ContractConsts.AuthorizationContract);
            chainId.ShouldBeGreaterThan(198535);
            chainId.ShouldBeLessThan(11316496);
        }

        [Fact]
        public void Convert_ChainId_To_Base58()
        {
            var chainId = ChainHelpers.GetRandomChainId();
            var dumpStr = ChainHelpers.ConvertChainIdToBase58(chainId);
            dumpStr.ShouldNotBe(string.Empty);

            var chainId1 = ChainHelpers.ConvertBase58ToChainId(dumpStr);
            chainId1.ShouldBe(chainId);
        }
    }
}