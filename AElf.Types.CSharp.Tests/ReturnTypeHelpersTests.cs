using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Types.CSharp.Tests
{
    public class ReturnTypeHelperTests
    {
        class TestUserType : UserType
        {
            public string Value { get; set; }
        }

        public class ReturnTypeHelpersTests
        {
            private T GetValue<T>()
            {
                var type = typeof(T);
                if (type == typeof(bool))
                {
                    return (T) (object) true;
                }

                if (type == typeof(int))
                {
                    return (T) (object) 32;
                }

                if (type == typeof(uint))
                {
                    return (T) (object) 32u;
                }

                if (type == typeof(long))
                {
                    return (T) (object) 64l;
                }

                if (type == typeof(ulong))
                {
                    return (T) (object) 64ul;
                }

                if (type == typeof(string))
                {
                    return (T) (object) "AElf";
                }

                if (type == typeof(byte[]))
                {
                    return (T) (object) new byte[] {0x1, 0x2, 0x3};
                }

                if (type == typeof(StringValue))
                {
                    return (T) (object) new StringValue()
                    {
                        Value = "AElf"
                    };
                }

                if (type == typeof(TestUserType))
                {
                    return (T) (object) new TestUserType()
                    {
                        Value = "AElf"
                    };
                }

                return default(T);
            }

            [Fact]
            public void Test()
            {
                TestForType<bool>();
                TestForType<int>();
                TestForType<uint>();
                TestForType<long>();
                TestForType<ulong>();
                TestForType<string>();
                TestForType<byte[]>();
                TestForType<StringValue>();
                TestForType<TestUserType>();
            }

            private void TestForType<T>()
            {
                var encoded = ReturnTypeHelper.GetEncoder<T>()(GetValue<T>());
                var decoded = ReturnTypeHelper.GetDecoder<T>()(encoded);
                decoded.ShouldBe(GetValue<T>());
            }
        }
    }
}