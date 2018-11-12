using System;
using AElf.Common.FSM;
using Xunit;

namespace AElf.Kernel.Tests
{
    // ReSharper disable InconsistentNaming
    public class FSMTest
    {
        [Fact]
        public void TimeoutTest()
        {
            var fsm = new FSM<int>();
            fsm.AddState(1)
                .SetTimeout(5000)
                .GoesTo(() => 2);
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
                .GoesTo(() => Season.Summer)
                .OnLeaving(() => flag.Value += 1);
            fsm.AddState(Season.Summer)
                .SetTimeout(1000)
                .GoesTo(() => Season.Autumn)
                .OnEntering(() => flag.Value += 10);
            fsm.AddState(Season.Autumn)
                .SetTimeout(1000)
                .GoesTo(() => Season.Winter)
                .OnEntering(() => flag.Value += 100);
            fsm.AddState(Season.Winter)
                .SetTimeout(1000)
                .GoesTo(() => Season.Spring)
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

            Season Selector()
            {
                if (amIInBeijing)
                {
                    return Season.Winter;
                }

                return Season.Autumn;
            }

            fsm.AddState(Season.Summer)
                .SetTimeout(1000)
                .GoesTo(Selector);
            fsm.AddState(Season.Autumn)
                .SetTimeout(1000)
                .GoesTo(() => Season.Winter)
                .OnEntering(() => flag.Value += 100);
            fsm.AddState(Season.Winter)
                .SetTimeout(amIInBeijing ? 2000 : 1000)
                .GoesTo(() => Season.Spring)
                .OnEntering(() => flag.Value += 1000);
            
            fsm.CurrentState = Season.Summer;
            fsm.ProcessWithTime(0);
            
            fsm.ProcessWithTime(999);
            Assert.Equal(Season.Summer, fsm.CurrentState);
            fsm.ProcessWithTime(1001);
            Assert.Equal(Season.Winter, fsm.CurrentState);
        }

        enum Season
        {
            Spring,
            Summer,
            Autumn,
            Winter
        }

        class Container
        {
            public int Value { get; set; }
        }
    }
}