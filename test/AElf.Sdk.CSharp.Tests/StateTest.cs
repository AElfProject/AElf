using System;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Types;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.Sdk.CSharp.Tests
{
    public class StateTest
    {
        internal T GetValue<T>()
        {
            if (typeof(T) == typeof(bool))
            {
                return (T) (object) true;
            }

            if (typeof(T) == typeof(int))
            {
                return (T) (object) (int) -12345;
            }

            if (typeof(T) == typeof(uint))
            {
                return (T) (object) (uint) 12345U;
            }

            if (typeof(T) == typeof(long))
            {
                return (T) (object) (long) -678910L;
            }

            if (typeof(T) == typeof(ulong))
            {
                return (T) (object) (ulong) 678910UL;
            }

            if (typeof(T) == typeof(byte[]))
            {
                return (T) (object) ByteArrayHelper.HexStringToByteArray("302010");
            }

            if (typeof(T) == typeof(string))
            {
                return (T) (object) "aelf";
            }

            if (typeof(T) == typeof(Address))
            {
                return (T) (object) SampleAddress.AddressList[0];
            }

            throw new Exception("Not supported type.");
        }

        private void SetValues(MockContractState state)
        {
            state.BoolState.Value = GetValue<bool>();
            state.Int32State.Value = GetValue<int>();
            state.UInt32State.Value = GetValue<uint>();
            state.Int64State.Value = GetValue<long>();
            state.UInt64State.Value = GetValue<ulong>();
            state.StringState.Value = GetValue<string>();
            state.BytesState.Value = GetValue<byte[]>();
            state.StructuredState.StringState.Value = GetValue<string>();
            state.MappedState[GetValue<Address>()][GetValue<Address>()] = GetValue<string>();
        }

        private void AssertDefault(MockContractState state)
        {
            Assert.False(state.BoolState.Value);
            Assert.Equal(0, state.Int32State.Value);
            Assert.Equal(0U, state.UInt32State.Value);
            Assert.Equal(0, state.Int64State.Value);
            Assert.Equal(0U, state.UInt64State.Value);
            Assert.Equal("", state.StringState.Value);
            Assert.Null(state.BytesState.Value);
            Assert.Equal("", state.StructuredState.StringState.Value);
            Assert.Equal("", state.MappedState[GetValue<Address>()][GetValue<Address>()]);
        }

        private void AssertValues(MockContractState state)
        {
            Assert.Equal(GetValue<bool>(), state.BoolState.Value);
            Assert.Equal(GetValue<int>(), state.Int32State.Value);
            Assert.Equal(GetValue<uint>(), state.UInt32State.Value);
            Assert.Equal(GetValue<long>(), state.Int64State.Value);
            Assert.Equal(GetValue<ulong>(), state.UInt64State.Value);
            Assert.Equal(GetValue<string>(), state.StringState.Value);
            Assert.Equal(GetValue<byte[]>(), state.BytesState.Value);
            Assert.Equal(GetValue<string>(), state.StructuredState.StringState.Value);
            Assert.Equal(GetValue<string>(), state.MappedState[GetValue<Address>()][GetValue<Address>()]);
        }

        [Fact]
        public void State_Test()
        {
            var path = new StatePath();
            path.Parts.Add("dummy_address");
            var mockProvider = new Mock<IStateProvider>();
            var mockContext = new Mock<ISmartContractBridgeContext>();
            mockContext.SetupGet(o => o.StateProvider).Returns(mockProvider.Object);
            mockContext.SetupGet(o => o.Self).Returns(SampleAddress.AddressList[0]);

            var state = new MockContractState
            {
                Path = path,
                Context = new CSharpSmartContractContext(mockContext.Object)
            };

            // Initial default value
            AssertDefault(state);

            // Set values
            SetValues(state);
            AssertValues(state);

            // Get changes
            var changes = state.GetChanges();

            // Clear values
            state.Clear();
            AssertDefault(state);
        }

        [Fact]
        public void Func_And_Action_ExtensionTest()
        {
            var state = new MockContractState()
            {
                ElfToken = new ElfTokenContractReference
                {
                    Action0 = () => { },
                    Action1 = (x) => { },
                    Action2 = (x, y) => { },

                    Func1 = () => true,
                    Func2 = (x) => false,
                    Func3 = (x, y) => x + y
                }
            };

            //func test
            var func1 = state.ElfToken.Func1.GetType();
            func1.IsFunc().ShouldBeTrue();
            func1.IsAction().ShouldBeFalse();

            var func2 = state.ElfToken.Func2.GetType();
            func2.IsFunc().ShouldBeTrue();
            func2.IsAction().ShouldBeFalse();

            var func3 = state.ElfToken.Func3.GetType();
            func3.IsFunc().ShouldBeTrue();
            func3.IsAction().ShouldBeFalse();
            
            //action test
            var action0 = state.ElfToken.Action0.GetType();
            action0.IsAction().ShouldBeTrue();
            action0.IsFunc().ShouldBeFalse();
            
            var action1 = state.ElfToken.Action1.GetType();
            action1.IsAction().ShouldBeTrue();
            action1.IsFunc().ShouldBeFalse();
            
            var action2 = state.ElfToken.Action2.GetType();
            action2.IsAction().ShouldBeTrue();
            action2.IsFunc().ShouldBeFalse();
        } 
    }
}