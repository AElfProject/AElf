using System;
using AElf.Common;
using AElf.Common.FSM;
using AElf.Kernel.Types;
using Xunit;

namespace AElf.Kernel.Tests
{
    // ReSharper disable InconsistentNaming
    public class FSMTest
    {
        private FSM _fsm;

        [Fact]
        public void TimeoutTest()
        {
            var fsm = new FSM();
            fsm.AddState(1)
                .SetTimeout(5000)
                .SetTransferFunction(() => 2);
            fsm.AddState(2);

            fsm.CurrentState = 1;

            fsm.ProcessWithNumber(0);
            Assert.Equal(1, fsm.CurrentState);
            fsm.ProcessWithNumber(1000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.ProcessWithNumber(2000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.ProcessWithNumber(3000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.ProcessWithNumber(4000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.ProcessWithNumber(5001);
            Assert.Equal(2, fsm.CurrentState);
        }

        [Fact]
        public void CallbackTest()
        {
            var flag = new Container();

            var fsm = new FSM();
            fsm.AddState((int) Season.Spring)
                .SetTimeout(1000)
                .SetTransferFunction(() => (int) Season.Summer)
                .OnLeaving(() => flag.Value += 1);
            fsm.AddState((int) Season.Summer)
                .SetTimeout(1000)
                .SetTransferFunction(() => (int) Season.Autumn)
                .OnEntering(() => flag.Value += 10);
            fsm.AddState((int) Season.Autumn)
                .SetTimeout(1000)
                .SetTransferFunction(() => (int) Season.Winter)
                .OnEntering(() => flag.Value += 100);
            fsm.AddState((int) Season.Winter)
                .SetTimeout(1000)
                .SetTransferFunction(() => (int) Season.Spring)
                .OnEntering(() => flag.Value += 1000);

            fsm.CurrentState = (int) Season.Spring;
            fsm.ProcessWithNumber(0);

            fsm.ProcessWithNumber(999);
            Assert.Equal(0, flag.Value);
            fsm.ProcessWithNumber(1001);
            Assert.Equal(11, flag.Value);
            fsm.ProcessWithNumber(2001);
            Assert.Equal(111, flag.Value);
            fsm.ProcessWithNumber(3001);
            Assert.Equal(1111, flag.Value);
            fsm.ProcessWithNumber(4001);
            fsm.ProcessWithNumber(5001);
            Assert.Equal(1122, flag.Value);
        }

        [Fact]
        public void NextStateSelectorTest()
        {
            var flag = new Container();
            var fsm = new FSM();

            var amIInBeijing = true;

            int StateTransferFunction()
            {
                if (amIInBeijing)
                {
                    return (int) Season.Winter;
                }

                return (int) Season.Autumn;
            }

            fsm.AddState((int) Season.Summer)
                .SetTimeout(1000)
                .SetTransferFunction(StateTransferFunction);
            fsm.AddState((int) Season.Autumn)
                .SetTimeout(1000)
                .SetTransferFunction(() => (int) Season.Winter)
                .OnEntering(() => flag.Value += 100);
            fsm.AddState((int) Season.Winter)
                .SetTimeout(amIInBeijing ? 2000 : 1000)
                .SetTransferFunction(() => (int) Season.Spring)
                .OnEntering(() => flag.Value += 1000);

            fsm.CurrentState = (int) Season.Summer;
            fsm.ProcessWithNumber(0);

            fsm.ProcessWithNumber(999);
            Assert.Equal((int) Season.Summer, fsm.CurrentState);
            fsm.ProcessWithNumber(1001);
            Assert.Equal((int) Season.Winter, fsm.CurrentState);
        }

        [Fact]
        public void NodeStateTest_CatchingToMining()
        {
            _fsm = new FSM();

            int TransferFromCatching()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlockHeader)
                {
                    return (int) NodeState.BlockValidating;
                }

                if (_fsm.StateEvent == StateEvent.MiningStart)
                {
                    return (int) NodeState.GeneratingConsensusTx;
                }

                return (int) NodeState.Catching;
            }
            
            _fsm.AddState((int) NodeState.Catching)
                .SetTransferFunction(TransferFromCatching)
                .OnEntering(LogWhenEntering)
                .OnEntering(FindMoreBlockHeadersToValidate)
                .OnLeaving(LogWhenLeaving);
            _fsm.AddState((int) NodeState.GeneratingConsensusTx);
            _fsm.AddState((int) NodeState.BlockValidating);

            _fsm.CurrentState = (int) NodeState.Catching;
            _fsm.ProcessWithStateEvent(StateEvent.MiningStart);

            Assert.Equal((int) NodeState.GeneratingConsensusTx, _fsm.CurrentState);
        }

        [Fact]
        public void NodeStateTest_BlockValidatingToBlockExecuting()
        {
            _fsm = new FSM();
            
            int TransferFromBlockValidating()
            {
                if (_fsm.StateEvent == StateEvent.ValidBlock)
                {
                    return (int) NodeState.BlockExecuting;
                }

                if (_fsm.StateEvent == StateEvent.InvalidBlock)
                {
                    return (int)  NodeState.Catching;
                }

                return (int) NodeState.BlockValidating;
            }

            _fsm.AddState((int) NodeState.BlockValidating)
                .SetTransferFunction(TransferFromBlockValidating)
                .OnEntering(LogWhenEntering)
                .OnLeaving(LogWhenLeaving);
            _fsm.AddState((int) NodeState.Catching);
            _fsm.AddState((int) NodeState.BlockExecuting);

            _fsm.CurrentState = (int) NodeState.BlockValidating;
            _fsm.ProcessWithStateEvent(StateEvent.ValidBlock);
            
            Assert.Equal((int) NodeState.BlockExecuting, _fsm.CurrentState);
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