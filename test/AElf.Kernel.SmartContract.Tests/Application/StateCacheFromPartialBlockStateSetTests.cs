using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class StateCacheFromPartialBlockStateSetTests
    {
        [Fact]
        public void StateCacheFromPartialBlockStateSet_Test()
        {
            var path = new StatePath();
            var deleteStatePath = new ScopedStatePath
            {
                Address = SampleAddress.AddressList[0],
                Path = path
            };
            var changeStatePath = new ScopedStatePath
            {
                Address = SampleAddress.AddressList[1],
                Path = path
            };
            var blockStateSet = new BlockStateSet
            {
                Deletes = {deleteStatePath.ToStateKey()},
                Changes = {{changeStatePath.ToStateKey(), ByteString.Empty}},
            };
            var stateCacheFromPartialBlockStateSet = new StateCacheFromPartialBlockStateSet(blockStateSet);
            var notExistStatePath = new ScopedStatePath
            {
                Address = SampleAddress.AddressList[2],
                Path = path
            };
            
            stateCacheFromPartialBlockStateSet.TryGetValue(deleteStatePath,out var deletedValue).ShouldBeTrue();
            deletedValue.ShouldBeNull();
            stateCacheFromPartialBlockStateSet[deleteStatePath].ShouldBeNull();
            
            stateCacheFromPartialBlockStateSet.TryGetValue(changeStatePath,out var changeValue).ShouldBeTrue();
            changeValue.ShouldBe(ByteString.Empty);
            stateCacheFromPartialBlockStateSet[changeStatePath].ShouldBe(ByteString.Empty.ToByteArray());
            
            stateCacheFromPartialBlockStateSet.TryGetValue(notExistStatePath, out var value).ShouldBeFalse();
            value.ShouldBeNull();
            stateCacheFromPartialBlockStateSet[notExistStatePath].ShouldBeNull();

            stateCacheFromPartialBlockStateSet[notExistStatePath] = ByteString.Empty.ToByteArray();
            stateCacheFromPartialBlockStateSet[notExistStatePath].ShouldBeNull();
        }
    }
}