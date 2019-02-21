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
            var base58String = ChainHelpers.ConvertChainIdToBase58(chainId);
            base58String.Length.ShouldBe(4);
        }

        [Fact]
        public void GetChainId_By_SerialNumber()
        {
            var base58HashSet = new HashSet<string>();
            var intHashSet = new HashSet<int>();
            for (var i = 195112UL; i < 11316496UL; i++)
            {
                var chainId = ChainHelpers.GetChainId(i);
                var base58String = ChainHelpers.ConvertChainIdToBase58(chainId);
                base58String.Length.ShouldBe(4);
                var newChainId = ChainHelpers.ConvertBase58ToChainId(base58String);
                newChainId.ShouldBe(chainId);
                base58HashSet.Add(base58String).ShouldBe(true);
                intHashSet.Add(newChainId).ShouldBe(true);
            }
        }
    }
}