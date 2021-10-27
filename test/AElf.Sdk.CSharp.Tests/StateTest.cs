using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Sdk.CSharp.State;
using AElf.Kernel.SmartContract;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Moq;
using Shouldly;
using Xunit;
using Type = System.Type;

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
            changes.Reads.Count.ShouldBeGreaterThan(0);
            changes.Writes.Count.ShouldBeGreaterThan(0);

            state.MappedState[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]] = "test";
            state.MappedState[SampleAddress.AddressList[1]][SampleAddress.AddressList[2]] = "test2";
            state.MappedState[SampleAddress.AddressList[3]][SampleAddress.AddressList[4]] = "test3";
            state.MappedState[SampleAddress.AddressList[0]].Remove(SampleAddress.AddressList[1]);
            state.MappedState[SampleAddress.AddressList[3]].Remove(SampleAddress.AddressList[4]);
            state.MappedState[SampleAddress.AddressList[0]].Remove(SampleAddress.AddressList[4]);
            state.MappedState[SampleAddress.AddressList[1]][SampleAddress.AddressList[2]].ShouldBe("test2");
            state.MappedState[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]].ShouldBeNull();
            state.MappedState[SampleAddress.AddressList[0]].Set(SampleAddress.AddressList[5],"test5");
            state.MappedState[SampleAddress.AddressList[0]][SampleAddress.AddressList[5]].ShouldBe("test5");
            var stateName = nameof(state.MappedState);
            changes = state.GetChanges();
            changes.Deletes.Count.ShouldBe(3);
            var key = string.Join("/",
                SampleAddress.AddressList[0].ToBase58(), "dummy_address", stateName,
                SampleAddress.AddressList[0].ToString(), SampleAddress.AddressList[1].ToString()
            );
            changes.Deletes.TryGetValue(key,out _).ShouldBeTrue();
            key = string.Join(",", SampleAddress.AddressList[0].ToBase58(), "dummy_address", stateName,
                SampleAddress.AddressList[1].ToString(), SampleAddress.AddressList[2].ToString());
            changes.Deletes.TryGetValue(key,out _).ShouldBeFalse();
            key = string.Join("/", SampleAddress.AddressList[0].ToBase58(), "dummy_address", stateName,
                SampleAddress.AddressList[3].ToString(), SampleAddress.AddressList[4].ToString());
            changes.Deletes.TryGetValue(key,out _).ShouldBeTrue();

            key = string.Join("/", SampleAddress.AddressList[0].ToBase58(), "dummy_address", stateName,
                SampleAddress.AddressList[4].ToString(), SampleAddress.AddressList[5].ToString());
            changes.Deletes.TryGetValue(key,out _).ShouldBeFalse();
            key = string.Join("/", SampleAddress.AddressList[0].ToBase58(), "dummy_address", stateName,
                SampleAddress.AddressList[0].ToString(), SampleAddress.AddressList[4].ToString());
            changes.Deletes.TryGetValue(key,out _).ShouldBeTrue();
            changes.Reads.Count.ShouldBeGreaterThanOrEqualTo(changes.Deletes.Count+ changes.Writes.Count);
            
            state.MappedState.OnContextSet();
            state.MappedState.Context.ShouldBe(state.MappedState[SampleAddress.AddressList[1]].Context);
            
            state.MappedState[SampleAddress.AddressList[1]].Clear();
            state.MappedState[SampleAddress.AddressList[1]][SampleAddress.AddressList[2]].ShouldBe(string.Empty);
            // Clear values
            state.Clear();
            AssertDefault(state);
        }

        [Fact]
        public void MappedState_Test()
        {
            var path = new StatePath();
            path.Parts.Add("dummy_address");
            var mockProvider = new Mock<IStateProvider>();
            var mockContext = new Mock<ISmartContractBridgeContext>();
            mockContext.SetupGet(o => o.StateProvider).Returns(mockProvider.Object);
            mockContext.SetupGet(o => o.Self).Returns(SampleAddress.AddressList[0]);
            
            var state = new MappedState<Address, Address, Address, string>
            {
                Path = path,
                Context = new CSharpSmartContractContext(mockContext.Object)
            };
            
            state[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[2]]
                .ShouldBe(string.Empty);
            state[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[2]] = "test";
            state[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[2]]
                .ShouldBe("test");
            state[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]].Remove(SampleAddress.AddressList[2]);
            state[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[2]].ShouldBeNull();

            state[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[3]] = "test2";
            state.OnContextSet();
            state.Context.ShouldBe(state[SampleAddress.AddressList[0]].Context);

            var changes = state.GetChanges();
            changes.Deletes.Count.ShouldBe(1);
            changes.Writes.Count.ShouldBe(1);
            changes.Reads.Count.ShouldBe(changes.Deletes.Count + changes.Writes.Count);
            
            state.Clear();
            state.GetChanges().ShouldBe(new TransactionExecutingStateSet());


            var mapState = new MappedState<Address, Address, Address, Address, string>
            {
                Path = path,
                Context = new CSharpSmartContractContext(mockContext.Object)
            };
            
            mapState[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[2]][SampleAddress.AddressList[3]]
                .ShouldBe(string.Empty);
            mapState[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[2]][SampleAddress.AddressList[3]] = "test";
            mapState[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[2]][SampleAddress.AddressList[3]]
                .ShouldBe("test");
            mapState[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[2]].Remove(SampleAddress.AddressList[3]);
            mapState[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[2]][SampleAddress.AddressList[3]].ShouldBeNull();

            mapState[SampleAddress.AddressList[0]][SampleAddress.AddressList[1]][SampleAddress.AddressList[2]][SampleAddress.AddressList[4]] = "test2";
            mapState.OnContextSet();
            mapState.Context.ShouldBe(mapState[SampleAddress.AddressList[0]].Context);

            changes = mapState.GetChanges();
            changes.Deletes.Count.ShouldBe(1);
            changes.Writes.Count.ShouldBe(1);
            changes.Reads.Count.ShouldBe(changes.Deletes.Count + changes.Writes.Count);
            
            mapState.Clear();
            mapState.GetChanges().ShouldBe(new TransactionExecutingStateSet());
        }
        
        [Fact]
        public void GetSubStatePath_Test()
        {
            var stateName = "Balances";
            var state = new MappedState();
            state.Path = new StatePath();
            state.Path.Parts.Add(stateName);
            var key = "ELF";
            var statePath = state.GetSubStatePath(key);
            statePath.Parts.Count.ShouldBe(2);
            statePath.Parts[0].ShouldBe(stateName);
            statePath.Parts[1].ShouldBe(key);
        }
        

        [Fact]
        public void MethodReferenceExtensions_Test()
        {
            typeof(MethodReference<Address,Empty>).IsMethodReference().ShouldBeTrue();
            typeof(Dictionary<string,string>).IsMethodReference().ShouldBeFalse();
            typeof(string).IsMethodReference().ShouldBeFalse();
        }

        [Fact]
        public void ReadOnlyState_Test()
        {
            var path = new StatePath();
            path.Parts.Add("dummy_address");
            var mockProvider = new Mock<IStateProvider>();
            var mockContext = new Mock<ISmartContractBridgeContext>();
            mockContext.SetupGet(o => o.StateProvider).Returns(mockProvider.Object);
            mockContext.SetupGet(o => o.Self).Returns(SampleAddress.AddressList[0]);
            var readOnlyState = new ReadonlyState<int>
            {
                Path = path,
                Context = new CSharpSmartContractContext(mockContext.Object)
            };
            readOnlyState.Value.ShouldBe(default);
            var intValue = 100;
            var otherIntValue = 200;
            readOnlyState.Value = intValue;
            readOnlyState.Value.ShouldBe(intValue);
            readOnlyState.Value = otherIntValue;
            readOnlyState.Value.ShouldBe(intValue);

            var changes = readOnlyState.GetChanges();
            changes.Writes.Count.ShouldBe(1);
            readOnlyState.Clear();
            readOnlyState.Loaded.ShouldBeFalse();
            readOnlyState.GetChanges().Writes.Count.ShouldBe(0);

            var otherReadOnlyState = new ReadonlyState<int>
            {
                Path = path, 
                Context = new CSharpSmartContractContext(mockContext.Object), 
                Value = intValue
            };
            otherReadOnlyState.Value.ShouldBe(intValue);

            var stringReadOnlyState = new ReadonlyState<string>
            {
                Path = path,
                Context = new CSharpSmartContractContext(mockContext.Object)
            };
            var stringValue = "test";
            stringReadOnlyState.Value = stringValue;
            stringReadOnlyState.Value.ShouldBe(string.Empty);
        }

        [Fact]
        public void SingletonState_Test()
        {
            var path = new StatePath();
            path.Parts.Add("dummy_address");
            var mockProvider = new Mock<IStateProvider>();
            var mockContext = new Mock<ISmartContractBridgeContext>();
            mockContext.SetupGet(o => o.StateProvider).Returns(mockProvider.Object);
            mockContext.SetupGet(o => o.Self).Returns(SampleAddress.AddressList[0]);
            var singletonState = new SingletonState<int>
            {
                Path = path,
                Context = new CSharpSmartContractContext(mockContext.Object)
            };
            singletonState.Value.ShouldBe(default);
            var intValue = 100;
            singletonState.Value = intValue;
            singletonState.Value.ShouldBe(intValue);

            singletonState.Modified.ShouldBeTrue();
            var changes = singletonState.GetChanges();
            changes.Writes.Count.ShouldBe(1);
            singletonState.Clear();
            singletonState.Loaded.ShouldBeFalse();
            singletonState.Modified.ShouldBeFalse();
            singletonState.Value.ShouldBe(default);
            singletonState.GetChanges().Writes.Count.ShouldBe(0);

            var otherReadOnlyState = new ReadonlyState<int>
            {
                Path = path, 
                Context = new CSharpSmartContractContext(mockContext.Object), 
                Value = intValue
            };
            otherReadOnlyState.Value.ShouldBe(intValue);
        }

        [Fact]
        public void StateBase_Test()
        {
            var part = "test";
            var mockProvider = new Mock<IStateProvider>();
            var mockContext = new Mock<ISmartContractBridgeContext>();
            mockContext.SetupGet(o => o.StateProvider).Returns(mockProvider.Object);
            mockContext.SetupGet(o => o.Self).Returns(SampleAddress.AddressList[0]);
            var stateBase = new StateBase();
            stateBase.Path = new StatePath
            {
                Parts = {part}
            };
            stateBase.Path.Parts.Single().ShouldBe(part);
            var context = new CSharpSmartContractContext(mockContext.Object);
            stateBase.Context = context;
            stateBase.Context.ShouldBe(context);
            stateBase.Provider.ShouldBe(mockProvider.Object);
            stateBase.Clear();
            var transactionExecutingStateSet = new TransactionExecutingStateSet();
            stateBase.GetChanges().ShouldBe(transactionExecutingStateSet);
        }
    }
}