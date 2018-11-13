using System;
using AElf.Common.FSM;
using AElf.Kernel.Types;
using Xunit;

namespace AElf.Kernel.Tests
{
    // ReSharper disable InconsistentNaming
    public class FSMTest
    {
        private FSM<NodeState> _fsm;

        [Fact]
        public void TimeoutTest()
        {
            var fsm = new FSM<int>();
            fsm.AddState(1)
                .SetTimeout(5000)
                .TransferTo(() => 2);
            fsm.AddState(2);

            fsm.CurrentState = 1;

            fsm.ProcessWithTime(0);
            Assert.Equal(1, fsm.CurrentState);
            fsm.ProcessWithTime(1000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.ProcessWithTime(2000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.ProcessWithTime(3000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.ProcessWithTime(4000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.ProcessWithTime(5001);
            Assert.Equal(2, fsm.CurrentState);
        }

        [Fact]
        public void CallbackTest()
        {
            var flag = new Container();

            var fsm = new FSM<Season>();
            fsm.AddState(Season.Spring)
                .SetTimeout(1000)
                .TransferTo(() => Season.Summer)
                .OnLeaving(() => flag.Value += 1);
            fsm.AddState(Season.Summer)
                .SetTimeout(1000)
                .TransferTo(() => Season.Autumn)
                .OnEntering(() => flag.Value += 10);
            fsm.AddState(Season.Autumn)
                .SetTimeout(1000)
                .TransferTo(() => Season.Winter)
                .OnEntering(() => flag.Value += 100);
            fsm.AddState(Season.Winter)
                .SetTimeout(1000)
                .TransferTo(() => Season.Spring)
                .OnEntering(() => flag.Value += 1000);

            fsm.CurrentState = Season.Spring;
            fsm.ProcessWithTime(0);

            fsm.ProcessWithTime(999);
            Assert.Equal(0, flag.Value);
            fsm.ProcessWithTime(1001);
            Assert.Equal(11, flag.Value);
            fsm.ProcessWithTime(2001);
            Assert.Equal(111, flag.Value);
            fsm.ProcessWithTime(3001);
            Assert.Equal(1111, flag.Value);
            fsm.ProcessWithTime(4001);
            fsm.ProcessWithTime(5001);
            Assert.Equal(1122, flag.Value);
        }

        [Fact]
        public void NextStateSelectorTest()
        {
            var flag = new Container();
            var fsm = new FSM<Season>();

            var amIInBeijing = true;

            Season StateTransferFunction()
            {
                if (amIInBeijing)
                {
                    return Season.Winter;
                }

                return Season.Autumn;
            }

            fsm.AddState(Season.Summer)
                .SetTimeout(1000)
                .TransferTo(StateTransferFunction);
            fsm.AddState(Season.Autumn)
                .SetTimeout(1000)
                .TransferTo(() => Season.Winter)
                .OnEntering(() => flag.Value += 100);
            fsm.AddState(Season.Winter)
                .SetTimeout(amIInBeijing ? 2000 : 1000)
                .TransferTo(() => Season.Spring)
                .OnEntering(() => flag.Value += 1000);

            fsm.CurrentState = Season.Summer;
            fsm.ProcessWithTime(0);

            fsm.ProcessWithTime(999);
            Assert.Equal(Season.Summer, fsm.CurrentState);
            fsm.ProcessWithTime(1001);
            Assert.Equal(Season.Winter, fsm.CurrentState);
        }

        [Fact]
        public void NodeStateTest_CatchingToCaught()
        {
            _fsm = new FSM<NodeState>();

            NodeState TransferFromCatching()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlockHeader)
                {
                    return NodeState.BlockValidating;
                }

                if (_fsm.StateEvent == StateEvent.BlockMined)
                {
                    return NodeState.Caught;
                }

                return NodeState.Catching;
            }
            
            _fsm.AddState(NodeState.Catching)
                .TransferTo(TransferFromCatching)
                .OnEntering(LogWhenEntering)
                .OnEntering(FindMoreBlockHeadersToValidate)
                .OnLeaving(LogWhenLeaving);
            _fsm.AddState(NodeState.Caught);
            _fsm.AddState(NodeState.BlockValidating);

            _fsm.CurrentState = NodeState.Catching;
            _fsm.ProcessWithStateEvent(StateEvent.BlockMined);

            Assert.Equal(NodeState.Caught, _fsm.CurrentState);
        }

        [Fact]
        public void NodeStateTest_BlockValidatingToBlockExecuting()
        {
            _fsm = new FSM<NodeState>();
            
            NodeState TransferFromBlockValidating()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlock)
                {
                    return NodeState.BlockExecuting;
                }

                if (_fsm.StateEvent == StateEvent.InvalidBlock)
                {
                    return NodeState.Catching;
                }

                return NodeState.BlockValidating;
            }

            _fsm.AddState(NodeState.BlockValidating)
                .TransferTo(TransferFromBlockValidating)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
            _fsm.AddState(NodeState.Catching);
            _fsm.AddState(NodeState.BlockExecuting);

            _fsm.CurrentState = NodeState.BlockValidating;
            _fsm.ProcessWithStateEvent(StateEvent.ValidBlock);
            
            Assert.Equal(NodeState.BlockExecuting, _fsm.CurrentState);
        }

        private void LogWhenEntering()
        {
            Console.WriteLine($"Entering {_fsm.CurrentState.ToString()}");
        }

        private void LogWhenLeaving()
        {
            Console.WriteLine($"Leaving {_fsm.CurrentState.ToString()}");
        }

        private void FindMoreBlockHeadersToValidate()
        {
        }

        enum Season
        {
            Spring,
            Summer,
            Autumn,
            Winter
        }

        private class Container
        {
            public int Value { get; set; }
        }
    }
}