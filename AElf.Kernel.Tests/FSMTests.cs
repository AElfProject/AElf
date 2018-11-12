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
            
            fsm.Process(0);
            Assert.Equal(1, fsm.CurrentState);
            fsm.Process(1000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.Process(2000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.Process(3000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.Process(4000);
            Assert.Equal(1, fsm.CurrentState);
            fsm.Process(5001);
            Assert.Equal(2, fsm.CurrentState);
        }
    }
}