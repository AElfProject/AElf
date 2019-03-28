using System;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Types.CSharp.Tests
{
    public class ParamsPackerTests
    {
        [Fact]
        public void Test()
        {
            var objs = new object[]
            {
                true,
                -32,
                (uint) 32,
                (long) -64,
                (ulong) 64,
                "AElf",
                new byte[] {0x1, 0x2, 0x3}
            };
            var serialized = new TestParams
            {
                Field1 = (bool) objs[0],
                Field2 = (int) objs[1],
                Field3 = (uint) objs[2],
                Field4 = (long) objs[3],
                Field5 = (ulong) objs[4],
                Field6 = (string) objs[5],
                Field7 = ByteString.CopyFrom((byte[]) objs[6])
            }.ToByteArray();
            Assert.Equal(serialized, ParamsPacker.Pack(
                objs
            ));
        }

        [Fact]
        public void Type_GetDefaultTest()
        {
            //reference type
            var userData = new PersonalData();
            var result = userData.GetType().GetDefault();
            result.ShouldBeNull();

            //value type
            var valueData = new Random().Next();
            var result1 = valueData.GetType().GetDefault();
            result1.ShouldBe(0);
        }

        [Fact]
        public void UserType_PackTest()
        {
            var userTypeObj = new PersonalData
            {
                Name = "Eric",
                Sex = "Male"
            };
            var packData = ParamsPacker.Pack(userTypeObj);
            packData.ShouldNotBeNull();

            var unpackObj = ParamsPacker.Unpack(packData, new[] {typeof(PersonalData)});
            unpackObj.Length.ShouldBe(1);
            var userTypeObj1 = unpackObj[0] as PersonalData;
            userTypeObj.ShouldBe(userTypeObj1);
        }
    }
}