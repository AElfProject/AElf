using System;
using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using Type = System.Type;

namespace AElf.Types.CSharp.Tests
{
    class TestUserType : UserType
    {
        public string Value { get; set; }
    }

    class MockContractForVoid
    {
        public int Value { get; private set; }

        public void SetInt(int value)
        {
            Value = value;
        }
    }

    class MockContract
    {
        public bool BoolReturnTypeMethod()
        {
            return MethodHandlerTests.GetValue<bool>();
        }

        public int Int32ReturnTypeMethod()
        {
            return MethodHandlerTests.GetValue<int>();
        }

        public uint UInt32ReturnTypeMethod()
        {
            return MethodHandlerTests.GetValue<uint>();
        }

        public long Int64ReturnTypeMethod()
        {
            return MethodHandlerTests.GetValue<long>();
        }

        public ulong UInt64ReturnTypeMethod()
        {
            return MethodHandlerTests.GetValue<ulong>();
        }

        public string StringReturnTypeMethod()
        {
            return MethodHandlerTests.GetValue<string>();
        }

        public byte[] BytesReturnTypeMethod()
        {
            return MethodHandlerTests.GetValue<byte[]>();
        }

        public StringValue PbMessageReturnTypeMethod()
        {
            return MethodHandlerTests.GetValue<StringValue>();
        }

        public TestUserType UserTypeReturnTypeMethod()
        {
            return MethodHandlerTests.GetValue<TestUserType>();
        }
    }

    public class MethodHandlerTests
    {
        public static object GetValue(Type type)
        {
            if (type == typeof(bool))
            {
                return true;
            }

            if (type == typeof(int))
            {
                return 32;
            }

            if (type == typeof(uint))
            {
                return 32u;
            }

            if (type == typeof(long))
            {
                return 64l;
            }

            if (type == typeof(ulong))
            {
                return 64ul;
            }

            if (type == typeof(string))
            {
                return "AElf";
            }

            if (type == typeof(byte[]))
            {
                return new byte[] {0x1, 0x2, 0x3};
            }

            if (type == typeof(StringValue))
            {
                return new StringValue()
                {
                    Value = "AElf"
                };
            }

            if (type == typeof(TestUserType))
            {
                return new TestUserType()
                {
                    Value = "AElf"
                };
            }

            return null;
        }

        public static T GetValue<T>()
        {
            var type = typeof(T);
            return (T) GetValue(type);
        }

        [Fact]
        public void Test()
        {
            {
                // Non-Void ReturnType
                var contract = new MockContract();
                foreach (var m in typeof(MockContract).GetMethods())
                {
                    if (m.IsConstructor || m.DeclaringType.Name != nameof(MockContract))
                    {
                        continue;
                    }

                    var handler = MethodHandlerFactory.CreateMethodHandler(m, contract);
                    var returnBytes = handler.Execute(ParamsPacker.Pack());
                    handler.BytesToReturnType(returnBytes).ShouldBe(GetValue(m.ReturnType));
                }
            }
            {
                // Void ReturnType
                var contract = new MockContractForVoid();
                int value = 99;
                var handler = MethodHandlerFactory.CreateMethodHandler(
                    typeof(MockContractForVoid).GetMethod(nameof(MockContractForVoid.SetInt)), contract);
                handler.Execute(ParamsPacker.Pack(value));
                contract.Value.ShouldBe(value);
            }
        }
    }
}