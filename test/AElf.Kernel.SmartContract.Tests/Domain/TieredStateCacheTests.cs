using System.Collections.Generic;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Domain
{
    public class TieredStateCacheTests
    {
        private readonly ScopedStatePath FirstPath = new ScopedStatePath
        {
            Address = SampleAddress.AddressList[0],
            Path = new StatePath()
        };
        private readonly ScopedStatePath SecondPath = new ScopedStatePath
        {
            Address = SampleAddress.AddressList[1],
            Path = new StatePath()
        };
        private readonly ByteString FirstValue = ByteString.CopyFromUtf8(nameof(FirstValue));
        private readonly ByteString SecondValue = ByteString.CopyFromUtf8(nameof(SecondValue));
        private readonly ByteString ThirdValue = ByteString.CopyFromUtf8(nameof(ThirdValue));
        
        [Fact]
        public void TieredStateCache_Test()
        {
            var path = new ScopedStatePath
            {
                Address = SampleAddress.AddressList[2],
                Path = new StatePath()
            };
            var cache = new TieredStateCache();
            cache.TryGetValue(path, out var value).ShouldBeFalse();
            value.ShouldBeNull();
            cache[path].ShouldBeNull();
            
            cache.Update(GetTransactionExecutingStateSets());
            cache.TryGetValue(FirstPath, out var firstValue).ShouldBeTrue();
            firstValue.ShouldBeNull();
            cache.TryGetValue(SecondPath, out var secondValue).ShouldBeTrue();
            secondValue.ShouldBe(SecondValue.ToByteArray());

            var stateCacheFromPartialBlockStateSet = new StateCacheFromPartialBlockStateSet(new BlockStateSet
            {
                Changes = { {SecondPath.ToStateKey(),ThirdValue}}
            });
            cache = new TieredStateCache(stateCacheFromPartialBlockStateSet);
            cache.TryGetValue(SecondPath,out secondValue).ShouldBeTrue();
            secondValue.ShouldBe(ThirdValue.ToByteArray());
            cache[SecondPath] = FirstValue.ToByteArray();
            cache[SecondPath].ShouldBe(FirstValue.ToByteArray());
            cache.TryGetValue(SecondPath, out secondValue);
            secondValue.ShouldBe(FirstValue.ToByteArray());
            
            cache.Update(GetTransactionExecutingStateSets());
            cache.TryGetValue(SecondPath, out secondValue);
            secondValue.ShouldBe(SecondValue.ToByteArray());
            
            stateCacheFromPartialBlockStateSet = new StateCacheFromPartialBlockStateSet(new BlockStateSet
            {
                Changes = { {SecondPath.ToStateKey(),ThirdValue}}
            });
            cache.Update(GetTransactionExecutingStateSets());
            cache.TryGetValue(SecondPath, out secondValue);
            secondValue.ShouldBe(SecondValue.ToByteArray());
        }

        private IEnumerable<TransactionExecutingStateSet> GetTransactionExecutingStateSets()
        {
            var firstKey = FirstPath.ToStateKey();
            var secondKey = SecondPath.ToStateKey();
            var transactionExecutingStateSets = new List<TransactionExecutingStateSet>();
            var transactionExecutingStateSet = new TransactionExecutingStateSet
            {
                Writes = {{firstKey, FirstValue}},
                Deletes = {{secondKey, true}}
            };
            transactionExecutingStateSets.Add(transactionExecutingStateSet);
            transactionExecutingStateSet = new TransactionExecutingStateSet
            {
                Writes = {{secondKey, SecondValue}},
                Deletes = {{firstKey, true}}
            };
            transactionExecutingStateSets.Add(transactionExecutingStateSet);
            return transactionExecutingStateSets;
        }
    }
}