using System;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class InValueCacheTests : AEDPoSTestBase
    {
        private IInValueCache _inValueCache;

        public InValueCacheTests()
        {
            _inValueCache = GetRequiredService<IInValueCache>();
        }

        [Fact]
        public void InValueCacheBasicFunctionTest()
        {
            const long startRoundId = 1000000L;
            Hash GenerateInValue(long i) => HashHelper.ComputeFrom($"InValue{i.ToString()}");
            for (var i = 0; i < 13; i++)
            {
                var roundId = startRoundId + i * 100;
                var inValue = GenerateInValue(roundId);
                _inValueCache.AddInValue(roundId, inValue);
            }

            _inValueCache.GetInValue(startRoundId + 500).ShouldBe(GenerateInValue(startRoundId + 500));
            // Already cleared.
            _inValueCache.GetInValue(startRoundId).ShouldBe(Hash.Empty);
        }
    }
}