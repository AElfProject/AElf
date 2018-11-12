using AElf.Common.FSM;
using Xunit;

namespace AElf.Kernel.Tests
{
    // ReSharper disable InconsistentNaming
    public class FSMTest
    {
        [Fact]
        public void BasicTest()
        {
            var fsm = new FSM<int>();
            fsm.AddState(1)
                .SetTimeout(5000)
                .GoesTo(() => 2);
            fsm.AddState(2);

            fsm.CurrentState = 1;
            
            fsm.Process(0);
            
        }
    }
}