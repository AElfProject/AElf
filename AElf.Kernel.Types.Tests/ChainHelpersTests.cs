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
            // Have tested all the conditions (195112UL ~ 11316496UL), To save time, just do some random test
            // for (var i = ; i < 11316496UL; i++)
            for (var i = 0; i < 1000; i++)
            {
                var chainId = ChainHelpers.GetRandomChainId();
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